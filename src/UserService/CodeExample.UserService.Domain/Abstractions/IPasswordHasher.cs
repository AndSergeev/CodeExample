namespace CodeExample.UserService.Domain.Abstractions;

/// <summary>Хеширование и проверка паролей; за интерфейсом, чтобы домен не знал о BCrypt.</summary>
public interface IPasswordHasher
{
    /// <summary>Считает хеш с солью, пригодный для хранения.</summary>
    string Hash(string password);

    /// <summary>Сверяет пароль с сохранённым хешем.</summary>
    bool Verify(string password, string passwordHash);
}
