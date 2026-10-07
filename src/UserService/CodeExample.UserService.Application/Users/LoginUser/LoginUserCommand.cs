using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.Shared.Abstractions;
using CodeExample.UserService.Application.Abstractions;
using CodeExample.UserService.Domain.Abstractions;
using CodeExample.UserService.Contracts;

namespace CodeExample.UserService.Application.Users.LoginUser;

/// <summary>Аутентифицирует пользователя и открывает новую сессию.</summary>
public sealed record LoginUserCommand(string Name, string Password) : IRequest<Result<LoginUserResponse>>;

internal sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("The user name is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("The password is required.");
    }
}

/// <inheritdoc />
internal sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, Result<LoginUserResponse>>
{
    private readonly IUserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<LoginUserCommandHandler> _logger;

    public LoginUserCommandHandler(
        IUserRepository repository,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IDateTimeProvider dateTimeProvider,
        ILogger<LoginUserCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(tokenIssuer);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<LoginUserResponse>> Handle(
        LoginUserCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = Domain.Entities.User.NormalizeName(request.Name);

        if (name is null)
        {
            return Result.Failure<LoginUserResponse>(InvalidCredentials());
        }

        var user = await _repository.FindByNameAsync(name, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Попытка входа с неизвестным именем пользователя");
            return Result.Failure<LoginUserResponse>(InvalidCredentials());
        }

        if (!_passwordHasher.Verify(request.Password, user.Password))
        {
            _logger.LogWarning("Неудачная попытка входа для пользователя {UserId}", user.Id);
            return Result.Failure<LoginUserResponse>(InvalidCredentials());
        }

        var now = _dateTimeProvider.UtcNow;
        var (tokens, session) = _tokenIssuer.Issue(user, now);

        await _repository.AddRefreshTokenAsync(session, cancellationToken: cancellationToken);

        _logger.LogInformation("Пользователь {UserId} вошёл в систему", user.Id);

        return Result.Success(new LoginUserResponse(user.ToResponse(), tokens));
    }

    private static Error InvalidCredentials() => Error.Unauthorized(
        "auth.invalid_credentials",
        "The user name or password is incorrect.");
}
