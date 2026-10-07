using CodeExample.FinanceService.Domain.Entities;

namespace CodeExample.FinanceService.Infrastructure.Persistence;

/// <summary>
/// Одна строка finance.user_favorite_currency.
/// </summary>
public sealed class UserFavoriteCurrencyRow
{
    public Guid UserId { get; init; }

    public string CurrencyId { get; init; } = string.Empty;

    /// <summary>Маппит сохранённую строку на домен.</summary>
    public static UserFavoriteCurrency ToEntity(UserFavoriteCurrencyRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return UserFavoriteCurrency.FromRow(row.UserId, row.CurrencyId);
    }
}
