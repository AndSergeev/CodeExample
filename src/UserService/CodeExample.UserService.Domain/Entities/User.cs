using CodeExample.Shared.Abstractions;

namespace CodeExample.UserService.Domain.Entities;

/// <summary>
/// Зарегистрированный пользователь.
/// </summary>
public sealed class User
{
    public const int NameMaxLength = 64;

    /// <inheritdoc cref="NameMaxLength"/>
    public const int NameMinLength = 3;

    /// <summary>Идентификатор пользователя.</summary>
    public Guid Id { get; init; }

    /// <summary>Логин.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>BCrypt-хеш пароля.</summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>Когда аккаунт создан.</summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>Создаёт пользователя. Пароль хеширует вызывающий.</summary>
    public static Result<User> Create(string? name, string password, DateTime now)
    {
        var normalized = NormalizeName(name);

        if (normalized is null)
        {
            return Result.Failure<User>(Error.Validation(
                "user.name_invalid",
                $"The user name must be between {NameMinLength} and {NameMaxLength} characters."));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return Result.Failure<User>(Error.Validation(
                "user.password_required",
                "A password hash is required to create a user."));
        }

        return Result.Success(new User
        {
            Id = Guid.NewGuid(),
            Name = normalized,
            Password = password,
            CreatedAtUtc = now
        });
    }

    /// <summary>Восстанавливает пользователя из строки БД.</summary>
    public static User FromRow(Guid id, string name, string password, DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Сохранённый пользователь обязан иметь идентификатор.", nameof(id));
        }

        if (NormalizeName(name) is not { } normalized || normalized != name)
        {
            throw new ArgumentException(
                "Имя сохранённого пользователя должно быть длиной от 3 до 64 символов и уже обрезанным.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Сохранённый пользователь обязан иметь пароль.", nameof(password));
        }

        return new User
        {
            Id = id,
            Name = name,
            Password = password,
            CreatedAtUtc = createdAtUtc
        };
    }

    /// <summary>Обрезает и проверяет логин.</summary>
    public static string? NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim();

        return trimmed.Length is < NameMinLength or > NameMaxLength ? null : trimmed;
    }
}
