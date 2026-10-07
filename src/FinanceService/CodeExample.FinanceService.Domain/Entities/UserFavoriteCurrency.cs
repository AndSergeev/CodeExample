namespace CodeExample.FinanceService.Domain.Entities;

/// <summary>
/// Отслеживаемая пользователем валюта.
/// </summary>
public sealed class UserFavoriteCurrency
{
    /// <summary>Предельная длина идентификатора валюты, совпадает с колонкой в схеме.</summary>
    public const int CurrencyIdMaxLength = 16;

    /// <summary>Владелец избранного.</summary>
    public Guid UserId { get; init; }

    /// <summary>Идентификатор валюты в сервисе курсов.</summary>
    public string CurrencyId { get; init; } = string.Empty;

    /// <summary>Начинает отслеживать валюту.</summary>
    public static UserFavoriteCurrency Create(Guid userId, string currencyId)
    {
        var (checkedUserId, checkedCurrencyId) = Validate(userId, currencyId, stored: false);

        return new UserFavoriteCurrency
        {
            UserId = checkedUserId,
            CurrencyId = checkedCurrencyId
        };
    }

    /// <summary>Маппит строку БД.</summary>
    public static UserFavoriteCurrency FromRow(Guid userId, string currencyId)
    {
        var (checkedUserId, checkedCurrencyId) = Validate(userId, currencyId, stored: true);

        return new UserFavoriteCurrency
        {
            UserId = checkedUserId,
            CurrencyId = checkedCurrencyId
        };
    }

    private static (Guid UserId, string CurrencyId) Validate(Guid userId, string currencyId, bool stored)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                stored ? "Сохранённая отметка обязана иметь владельца." : "Нужен идентификатор пользователя.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(currencyId) || currencyId.Length > CurrencyIdMaxLength)
        {
            throw new ArgumentException(
                stored
                    ? $"Идентификатор сохранённой валюты должен быть длиной от 1 до {CurrencyIdMaxLength} символов."
                    : $"Нужен идентификатор валюты длиной от 1 до {CurrencyIdMaxLength} символов.",
                nameof(currencyId));
        }

        return (userId, currencyId);
    }
}
