using System.Reflection;
using CodeExample.Shared.Migrations;
using CodeExample.UserService.Infrastructure;

namespace CodeExample.UserService.Migrator.Host;

internal static class Program
{
    private static Task<int> Main(string[] args) =>
        MigratorHost.RunAsync(
            args,
            DependencyInjection.ConnectionStringName,
            "User service",
            UserDatabaseSchema.Name,
            UserDatabaseSchema.JournalTable,
            Assembly.GetExecutingAssembly());
}
