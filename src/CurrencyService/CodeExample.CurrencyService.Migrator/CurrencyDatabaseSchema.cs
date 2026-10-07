namespace CodeExample.CurrencyService.Migrator;

/// <summary>
/// Схема, в которой живут таблицы сервиса валют.
/// </summary>
internal static class CurrencyDatabaseSchema
{
    /// <summary>Схема, принадлежащая сервису.</summary>
    public const string Name = "currencies";

    /// <summary>Таблица, в которой DbUp отмечает применённые скрипты.</summary>
    public const string JournalTable = "schemaversions";
}
