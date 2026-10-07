namespace CodeExample.Shared.Security;

/// <summary>Выпускает подписанные access-токены.</summary>
public interface IJwtTokenGenerator
{
    /// <summary>Создаёт подписанный access-токен для указанного пользователя.</summary>
    /// <param name="userId">Значение claim'а sub.</param>
    /// <param name="userName">Значение claim'а name.</param>
    string CreateAccessToken(Guid userId, string userName, DateTime now);
}

/// <summary>Создаёт refresh-токен; хранится только его хеш.</summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Возвращает URL-safe случайный токен.</summary>
    string CreateToken();

    /// <summary>Возвращает SHA-256 хеш токена.</summary>
    string HashToken(string token);
}
