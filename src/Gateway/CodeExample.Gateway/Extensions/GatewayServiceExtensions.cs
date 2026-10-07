using CodeExample.Shared.Web;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CodeExample.Gateway.Extensions;

internal static class GatewayServiceExtensions
{
    /// <summary>
    /// Регистрирует YARP из секции ReverseProxy.
    /// </summary>
    public static IServiceCollection AddGatewayProxy(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<ITransformProvider, IdentityForwardingTransformProvider>();

        services
            .AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }

    /// <summary>
    /// Регистрирует Swagger для самого шлюза: только его собственные эндпоинты, схему безопасности
    /// публикует каждый бэкенд.
    /// </summary>
    public static IServiceCollection AddGatewaySwagger(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddSwaggerDocumentation(configuration);
    }
}
