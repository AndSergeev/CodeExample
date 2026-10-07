using CodeExample.Shared.Web;
using CodeExample.Shared.HealthChecks;
using CodeExample.UserService.Application;
using CodeExample.UserService.Infrastructure;

namespace CodeExample.UserService.Api.Host;

/// <summary>Внедрение зависимостей.</summary>
internal static class DependencyInjection
{
    /// <summary>Внедрение зависимостей.</summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configuration = builder.Configuration;

        builder.Services.AddUserInfrastructure(configuration);
        builder.Services.AddUserApplication(configuration);

        builder.Services.AddSharedJwtAuthentication(configuration);

        builder.Services.AddSwaggerDocumentation(configuration);

        builder.Services
            .AddHealthProbe(configuration)
            .AddCheck<DatabaseHealthCheck>(DatabaseHealthCheck.Name);

        return builder;
    }
}
