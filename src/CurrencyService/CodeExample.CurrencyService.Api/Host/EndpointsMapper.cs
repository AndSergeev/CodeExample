using CodeExample.CurrencyService.Api.Endpoints;

namespace CodeExample.CurrencyService.Api.Host;

/// <summary>
/// Регистрирует все ручки, которые публикует сервис.
/// </summary>
internal static class EndpointsMapper
{
    public static void Map(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapCurrenciesEndpoints();
    }
}
