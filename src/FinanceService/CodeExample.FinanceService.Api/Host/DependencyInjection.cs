using CodeExample.FinanceService.Application;
using CodeExample.FinanceService.Infrastructure;
using CodeExample.Shared.Web;
using CodeExample.Shared.HealthChecks;

namespace CodeExample.FinanceService.Api.Host;

/// <summary>Внедрение зависимостей.</summary>
internal static class DependencyInjection
{
    /// <summary>
    /// Внедрение зависимостей.
    /// </summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configuration = builder.Configuration;

        builder.Services.AddFinanceInfrastructure(configuration);
        builder.Services.AddFinanceApplication(configuration);

        builder.Services.AddSharedJwtAuthentication(configuration);
        builder.Services.AddSwaggerDocumentation(configuration);

        builder.Services
            .AddHealthProbe(configuration)
            .AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);

        return builder;
    }
}
