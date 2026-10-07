using CodeExample.UserService.Api.Endpoints;

namespace CodeExample.UserService.Api.Host;

/// <summary>
/// Регистрирует все маршруты этого сервиса.
/// </summary>
internal static class EndpointsMapper
{
    public static void Map(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapAuthEndpoints();
    }
}
