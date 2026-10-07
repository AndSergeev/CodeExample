namespace CodeExample.UserService.Domain.Entities;

/// <summary>
/// Токен обновления сессии.
/// </summary>
public sealed record RefreshToken
{
    private const int HashLength = 64;

    /// <summary>Идентификатор сессии.</summary>
    public Guid Id { get; init; }

    /// <summary>Идентификатор пользователя.</summary>
    public Guid UserId { get; init; }

    /// <summary>Хеш токена.</summary>
    public string TokenHash { get; init; } = string.Empty;

    /// <summary>Когда сессия создана.</summary>
    public DateTime CreatedAtUtc { get; init; }

    /// <summary>Когда сессия истекает.</summary>
    public DateTime ExpiresAtUtc { get; init; }

    /// <summary>Проставляется при выходе пользователя.</summary>
    public DateTime? RevokedAtUtc { get; init; }

    /// <summary>Проставляется, когда для сессии выдали замену. Отличает обмен от выхода.</summary>
    public DateTime? ReplacedAtUtc { get; init; }

    public static RefreshToken Issue(Guid userId, string tokenHash, DateTime now, TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Для выпуска refresh-токена нужен идентификатор пользователя.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Length != HashLength)
        {
            throw new ArgumentException("Для выпуска refresh-токена нужен хеш ожидаемой длины.", nameof(tokenHash));
        }

        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(lifetime),
            RevokedAtUtc = null,
            ReplacedAtUtc = null
        };
    }
}
