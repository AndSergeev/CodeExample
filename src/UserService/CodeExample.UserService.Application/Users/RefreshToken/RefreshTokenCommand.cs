using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.Shared.Abstractions;
using CodeExample.Shared.Security;
using CodeExample.UserService.Domain.Abstractions;
using CodeExample.UserService.Application.Abstractions;
using CodeExample.UserService.Contracts;

namespace CodeExample.UserService.Application.Users.RefreshToken;

/// <summary>Обменивает refresh-токен на новую пару токенов.</summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<AuthTokensResponse>>;

/// <summary>Коды отказа обмена, по которым вызывающий различает случаи.</summary>
public static class RefreshTokenErrors
{
    /// <summary>Токен неизвестен, просрочен или уже отозван: предъявлять его больше нечем.</summary>
    public const string Invalid = "auth.refresh_token_invalid";

    /// <summary>Сессию обменяли другим запросом: у выигравшего уже есть новая пара.</summary>
    public const string SessionReplaced = "auth.session_replaced";
}

internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    private const int MaximumTokenLength = 256;

    public RefreshTokenCommandValidator() =>
        RuleFor(x => x.RefreshToken)
            .MaximumLength(MaximumTokenLength)
            .WithMessage("The refresh token is not a value this service issues.");
}

/// <inheritdoc />
internal sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<AuthTokensResponse>>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IRefreshTokenGenerator refreshTokenGenerator,
        ITokenIssuer tokenIssuer,
        IDateTimeProvider dateTimeProvider,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(refreshTokenGenerator);
        ArgumentNullException.ThrowIfNull(tokenIssuer);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _unitOfWork = unitOfWork;
        _refreshTokenGenerator = refreshTokenGenerator;
        _tokenIssuer = tokenIssuer;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<AuthTokensResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _dateTimeProvider.UtcNow;
        var tokenHash = _refreshTokenGenerator.HashToken(request.RefreshToken);

        await using var unitOfWork = _unitOfWork;

        var transaction = await unitOfWork.BeginAsync(cancellationToken);

        // Гашение предъявленного токена и запись замены идут в одной транзакции. Порознь сбой
        // между ними уничтожил бы сессию, не выдав новую, и повторить было бы нечем: токен уже сожжён.
        var consumption = await _repository
            .ConsumeAsync(tokenHash, now, now, transaction, cancellationToken);

        if (consumption is null || consumption.Outcome == RefreshTokenConsumption.NotUsable)
        {
            return Result.Failure<AuthTokensResponse>(Error.Unauthorized(
                RefreshTokenErrors.Invalid,
                "The refresh token is invalid or has expired."));
        }

        if (consumption.Outcome == RefreshTokenConsumption.AlreadyReplaced)
        {
            // Отдельный код: сессию обменял другой запрос, и у вызывающего уже может быть её
            // новая пара. Стирать её cookie в этом случае нельзя.
            return Result.Failure<AuthTokensResponse>(Error.Unauthorized(
                RefreshTokenErrors.SessionReplaced,
                "This refresh token was already exchanged. Use the pair issued by the request that won."));
        }

        // Транзакция передаётся и в чтение: строка сессии уже заперта этой же транзакцией,
        // а соединение из пула не увидело бы её незакоммиченного состояния.
        var user = await _repository
            .FindByIdAsync(consumption.UserId, transaction, cancellationToken);

        if (user is null)
        {
            return Result.Failure<AuthTokensResponse>(Error.Unauthorized(
                RefreshTokenErrors.Invalid,
                "The refresh token is invalid or has expired."));
        }

        var (tokens, replacement) = _tokenIssuer.Issue(user, now);

        await _repository.AddRefreshTokenAsync(replacement, transaction, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Обновлена сессия обновления пользователя {UserId}", consumption.UserId);
        return Result.Success(tokens);
    }
}
