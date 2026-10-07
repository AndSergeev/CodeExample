using CodeExample.FinanceService.Api.Endpoints;

namespace CodeExample.FinanceService.Api.Host;

/// <summary>
/// Регистрация эндпоинтов
/// </summary>
internal static class EndpointsMapper
{
    public static void Map(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapFavoriteCurrenciesEndpoints();
    }
}
