namespace CodeExample.CurrencyService.Domain.Entities;

/// <summary>
/// Валюта, публикуемая ЦБ РФ, и её курс.
/// </summary>
public sealed class Currency
{
    private const int NameMaxLength = 128;

    private const int IdMaxLength = 16;

    /// <summary>
    /// Идентификатор ЦБ РФ, например R01235: первичный ключ строки.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Название валюты, как его публикует ЦБ РФ.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Рублей ровно за одну единицу валюты.
    /// </summary>
    public decimal Rate { get; init; }

    /// <summary>
    /// Публикует ли банк эту валюту до сих пор.
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Создаёт валюту из свежей записи ЦБ РФ.
    /// </summary>
    /// <remarks>
    /// Банк публикует стоимость не одной единицы, а номинала: у иены это сто, у некоторых
    /// валют миллион. Деление приводит курс к рублям ровно за одну единицу.
    /// </remarks>
    public static Currency CreateFromSource(
        string id,
        string name,
        decimal value,
        int nominal)
    {
        var (checkedId, checkedName, checkedValue, checkedNominal) = Validate(id, name, value, nominal);

        return new Currency
        {
            Id = checkedId,
            Name = checkedName,
            Rate = checkedValue / checkedNominal,
            IsActive = true
        };
    }

    /// <summary>
    /// Восстанавливает валюту, прочитанную из хранилища.
    /// </summary>
    public static Currency FromRow(
        string id,
        string name,
        decimal rate,
        bool isActive)
    {
        // Прочитанный курс уже приведён к единице, поэтому делитель не нужен: единица
        // пропускает проверку номинала, не меняя значение.
        var (checkedId, checkedName, checkedRate, _) = Validate(id, name, rate, nominal: 1);

        return new Currency
        {
            Id = checkedId,
            Name = checkedName,
            Rate = checkedRate,
            IsActive = isActive
        };
    }

    private static (string Id, string Name, decimal Value, int Nominal) Validate(
        string id,
        string name,
        decimal value,
        int nominal)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > IdMaxLength)
        {
            throw new ArgumentException($"Идентификатор должен быть длиной от 1 до {IdMaxLength} символов.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
        {
            throw new ArgumentException($"Название должно быть длиной от 1 до {NameMaxLength} символов.", nameof(name));
        }

        if (nominal <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(nominal), nominal, "Номинал должен быть положительным.");
        }

        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Значение должно быть положительным.");
        }

        return (id, name, value, nominal);
    }
}
