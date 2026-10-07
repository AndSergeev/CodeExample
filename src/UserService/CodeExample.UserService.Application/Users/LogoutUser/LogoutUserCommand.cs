using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.Shared.Abstractions;
using CodeExample.Shared.Security;
using CodeExample.UserService.Domain.Abstractions;

namespace CodeExample.UserService.Application.Users.LogoutUser;

/// <summary>
/// Завершает сессию.
/// </summary>
/// <param name="UserId">Берётся из проверенного access-токена, никогда из тела запроса.</param>
/// <param name="RefreshToken">Если не передан, отзываются все активные сессии.</param>
public sealed record LogoutUserCommand(Guid UserId, string? RefreshToken = null) : IRequest<Result<bool>>;

internal sealed class LogoutUserCommandValidator : AbstractValidator<LogoutUserCommand>
{
    public LogoutUserCommandValidator() =>
        RuleFor(x => x.UserId).NotEmpty().WithMessage("A user id is required.");
}

/// <inheritdoc />
internal sealed class LogoutUserCommandHandler : IRequestHandler<LogoutUserCommand, Result<bool>>
{
    private readonly IUserRepository _repository;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LogoutUserCommandHandler> _logger;

    public LogoutUserCommandHandler(
        IUserRepository repository,
        IRefreshTokenGenerator refreshTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        ILogger<LogoutUserCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(refreshTokenGenerator);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _refreshTokenGenerator = refreshTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(LogoutUserCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _repository.FindByIdAsync(request.UserId, cancellationToken: cancellationToken);

        if (user is null)
        {
            return Result.Failure<bool>(Error.NotFound(
                "user.not_found",
                "The authenticated user no longer exists."));
        }

        var now = _dateTimeProvider.UtcNow;
        var revoked = request.RefreshToken is null
            ? await _repository.RevokeAllRefreshTokensAsync(user.Id, now, cancellationToken)
            : await _repository.RevokeRefreshTokenAsync(
                user.Id,
                _refreshTokenGenerator.HashToken(request.RefreshToken),
                now,
                cancellationToken);

        _logger.LogInformation("Отозвано сессий пользователя {UserId}: {RevokedCount}", user.Id, revoked);

        return Result.Success(true);
    }
}
