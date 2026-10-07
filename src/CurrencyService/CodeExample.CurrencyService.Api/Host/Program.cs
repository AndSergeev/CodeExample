using CodeExample.Shared.Web;

namespace CodeExample.CurrencyService.Api.Host;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var app = WebApplication
            .CreateBuilder(args)
            .ConfigureSettings()
            .ConfigureLogging()
            .ConfigureServices()
            .Build();

        app.Configure(EndpointsMapper.Map);

        await app.RunAsync();
    }
}
