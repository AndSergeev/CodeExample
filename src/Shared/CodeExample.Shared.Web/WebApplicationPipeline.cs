using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CodeExample.Shared.Web;

/// <summary>
/// Конвейер запросов.
/// </summary>
public static class WebApplicationPipeline
{
    /// <summary>
    /// Собирает конвейер и маппит маршруты сервиса.
    /// </summary>
    public static WebApplication Configure(this WebApplication app, Action<WebApplication> mapEndpoints)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(mapEndpoints);

        var configuration = app.Configuration;

        var serviceName = configuration.ServiceName();

        app.UseSerilogRequestLogging();

        app.UseApiExceptionHandling(serviceName);

        app.UseSwagger();
        app.UseSwaggerUI(ui =>
        {
            ui.SwaggerEndpoint(SwaggerExtensions.SwaggerJsonPath(configuration), serviceName);
            ui.DocumentTitle = serviceName;
        });

        app.UseAuthentication();
        app.UseAuthorization();

        mapEndpoints(app);

        app.MapHealthEndpoints(configuration);

        return app;
    }

    private static IApplicationBuilder UseApiExceptionHandling(this WebApplication app, string serviceName)
    {
        var logger = app.Logger;

        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();

            await ProblemDetailsWriter.WriteUnhandledAsync(
                context,
                feature?.Error,
                "server.unhandled_exception");

            if (feature?.Error is not null)
            {
                logger.LogError(
                    feature.Error,
                    "Необработанное исключение в {ServiceName} при обработке {Path}",
                    serviceName,
                    context.Request.Path);
            }
        }));

        return app;
    }
}
