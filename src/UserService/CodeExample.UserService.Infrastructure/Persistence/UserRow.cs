using CodeExample.UserService.Domain.Entities;

namespace CodeExample.UserService.Infrastructure.Persistence;

/// <summary>
/// Одна строка users.user.
/// </summary>
public sealed class UserRow
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    /// <summary>Маппит сохранённую строку на домен.</summary>
    public static User ToEntity(UserRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return User.FromRow(row.Id, row.Name, row.Password, row.CreatedAtUtc);
    }
}
