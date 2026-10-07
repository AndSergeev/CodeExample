namespace CodeExample.UserService.Migrator;

/// <summary>Схема, в которой живут объекты User-сервиса.</summary>
internal static class UserDatabaseSchema
{
    public const string Name = "users";

    public const string JournalTable = "schemaversions";
}
