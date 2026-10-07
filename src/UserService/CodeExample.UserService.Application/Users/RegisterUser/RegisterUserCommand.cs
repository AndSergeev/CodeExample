using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.Shared.Abstractions;
using CodeExample.UserService.Domain.Abstractions;
using CodeExample.UserService.Application.Abstractions;
using CodeExample.UserService.Domain.Entities;
using CodeExample.UserService.Contracts;

namespace CodeExample.UserService.Application.Users.RegisterUser;

/// <summary>Регистрирует нового пользователя и сразу выполняет вход.</summary>
public sealed record RegisterUserCommand(string Name, string Password) : IRequest<Result<RegisterUserResponse>>;

internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private const int PasswordMinLength = 8;

    // bcrypt отбрасывает всё после 72 байт, а не символов: сорок кириллических букв
    // весят восемьдесят байт и превратились бы в другой пароль.
    internal const int PasswordMaxBytes = 72;

    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("The user name is required.")
            .Must(name => User.NormalizeName(name) is not null)
            .WithMessage($"The user name must be between {User.NameMinLength} and {User.NameMaxLength} characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("The password is required.")
            .MinimumLength(PasswordMinLength)
            .WithMessage($"The password must be at least {PasswordMinLength} characters long.")
            .Must(password => Encoding.UTF8.GetByteCount(password ?? string.Empty) <= PasswordMaxBytes)
            .WithMessage($"The password must not exceed {PasswordMaxBytes} bytes.");
    }
}

/// <inheritdoc />
internal sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Result<RegisterUserResponse>>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenIssuer _tokenIssuer;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ITokenIssuer tokenIssuer,
        IDateTimeProvider dateTimeProvider,
        ILogger<RegisterUserCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(tokenIssuer);
        ArgumentNullException.ThrowIfNull(dateTimeProvider);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<RegisterUserResponse>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = User.NormalizeName(request.Name);

        if (name is null)
        {
            return Result.Failure<RegisterUserResponse>(Error.Validation(
                "user.name_invalid",
                $"The user name must be between {User.NameMinLength} and {User.NameMaxLength} characters."));
        }

        if (await _repository.NameExistsAsync(name, cancellationToken))
        {
            return Result.Failure<RegisterUserResponse>(Error.Conflict(
                "user.name_taken",
                $"The user name '{name}' is already taken."));
        }

        var now = _dateTimeProvider.UtcNow;
        var password = _passwordHasher.Hash(request.Password);

        var userResult = User.Create(name, password, now);

        if (userResult.IsFailure)
        {
            return Result.Failure<RegisterUserResponse>(userResult.Error);
        }

        var user = userResult.Value;
        var (tokens, session) = _tokenIssuer.Issue(user, now);

        await using var unitOfWork = _unitOfWork;

        var transaction = await unitOfWork.BeginAsync(cancellationToken);

        // Проверка выше отсеивает занятый логин без записи, но между ней и вставкой
        // помещается параллельный запрос. Тогда строк не запишется, и это тот же отказ.
        var written = await _repository.AddAsync(user, transaction, cancellationToken);

        if (written == 0)
        {
            return Result.Failure<RegisterUserResponse>(Error.Conflict(
                "user.name_taken",
                $"The user name '{name}' is already taken."));
        }

        await _repository.AddRefreshTokenAsync(session, transaction, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);

        _logger.LogInformation("Зарегистрирован пользователь {UserId}", user.Id);

        return Result.Success(new RegisterUserResponse(user.ToResponse(), tokens));
    }
}
