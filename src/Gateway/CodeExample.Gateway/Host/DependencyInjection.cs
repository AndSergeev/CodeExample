using CodeExample.Gateway.Extensions;
using CodeExample.Shared.Web;

namespace CodeExample.Gateway.Host;

/// <summary>Внедрение зависимостей.</summary>
internal static class DependencyInjection
{
    /// <summary>Внедрение зависимостей.</summary>
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configuration = builder.Configuration;

        builder.Services.AddGatewayProxy(configuration);

        builder.Services.AddSharedJwtAuthentication(configuration);
        builder.Services.AddGatewayRateLimiting(configuration);
        builder.Services.AddGatewaySwagger(configuration);

        builder.Services.AddHealthProbe(configuration);

        return builder;
    }
}
