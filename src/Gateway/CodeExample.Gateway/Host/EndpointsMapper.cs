using CodeExample.Gateway.Extensions;
using CodeExample.Shared.Web;

namespace CodeExample.Gateway.Host;

/// <summary>
/// Регистрирует все маршруты, которые публикует шлюз.
/// </summary>
internal static class EndpointsMapper
{
    public static void Map(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapReverseProxy()
            .RequireRateLimiting(RateLimitingExtensions.PolicyName)
            .AllowAnonymous();

        // Общий конвейер мапит health только в перегрузке с делегатом, а шлюз зовёт свою,
        // поэтому здесь это единственная регистрация, а не повторная.
        app.MapHealthEndpoints(app.Configuration);
    }
}
