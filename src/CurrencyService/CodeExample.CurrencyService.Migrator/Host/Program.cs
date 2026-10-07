using System.Reflection;
using CodeExample.CurrencyService.Infrastructure;
using CodeExample.Shared.Migrations;

namespace CodeExample.CurrencyService.Migrator.Host;

internal static class Program
{
    private static Task<int> Main(string[] args) =>
        MigratorHost.RunAsync(
            args,
            DependencyInjection.ConnectionStringName,
            "Currency service",
            CurrencyDatabaseSchema.Name,
            CurrencyDatabaseSchema.JournalTable,
            Assembly.GetExecutingAssembly());
}
