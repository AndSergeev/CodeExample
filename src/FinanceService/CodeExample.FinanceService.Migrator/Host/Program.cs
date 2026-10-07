using System.Reflection;
using CodeExample.FinanceService.Infrastructure;
using CodeExample.Shared.Migrations;

namespace CodeExample.FinanceService.Migrator.Host;

internal static class Program
{
    private static Task<int> Main(string[] args) =>
        MigratorHost.RunAsync(
            args,
            DependencyInjection.ConnectionStringName,
            "Finance service",
            FinanceDatabaseSchema.Name,
            FinanceDatabaseSchema.JournalTable,
            Assembly.GetExecutingAssembly());
}
