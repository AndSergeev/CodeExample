using CodeExample.CurrencyService.Api.HostedServices;
using CodeExample.CurrencyService.Application;
using CodeExample.CurrencyService.Infrastructure;
using CodeExample.Shared.Web;
using CodeExample.Shared.HealthChecks;

namespace CodeExample.CurrencyService.Api.Host;

/// <summary>
/// Внедрение зависимостей.
/// </summary>
internal static class DependencyInjection
{
    /// <summary>Внедрение зависимостей.</summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configuration = builder.Configuration;

        builder.Services.AddCurrencyRateSynchronization(configuration);
        builder.Services.AddCurrencyInfrastructure(configuration);
        builder.Services.AddCurrencyApplication(configuration);

        builder.Services.AddInternalOnlyAuthentication(configuration);
        builder.Services.AddSwaggerDocumentation(configuration);

        builder.Services
            .AddHealthProbe(configuration)
            .AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);

        builder.Services.AddHostedService<CurrencySyncStartupRunner>();

        return builder;
    }
}
