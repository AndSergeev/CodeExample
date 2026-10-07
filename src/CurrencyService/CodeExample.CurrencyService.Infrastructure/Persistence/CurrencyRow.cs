namespace CodeExample.CurrencyService.Infrastructure.Persistence;

/// <summary>
/// Валюта в БД.
/// </summary>
public sealed class CurrencyRow
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public decimal Rate { get; init; }

    public bool IsActive { get; init; }

    /// <summary>Маппит сущность на хранимую форму.</summary>
    public static CurrencyRow From(Domain.Entities.Currency currency)
    {
        ArgumentNullException.ThrowIfNull(currency);

        return new CurrencyRow
        {
            Id = currency.Id,
            Name = currency.Name,
            Rate = currency.Rate,
            IsActive = currency.IsActive
        };
    }

    /// <summary>
    /// Маппит хранимую строку на домен.
    /// </summary>
    public static Domain.Entities.Currency ToEntity(CurrencyRow row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return Domain.Entities.Currency.FromRow(
            row.Id,
            row.Name,
            row.Rate,
            row.IsActive);
    }
}
