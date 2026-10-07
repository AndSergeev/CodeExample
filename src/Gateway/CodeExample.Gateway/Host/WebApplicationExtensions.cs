using Microsoft.AspNetCore.Diagnostics;
using Serilog;
using CodeExample.Shared.Web;

namespace CodeExample.Gateway.Host;

internal static class WebApplicationExtensions
{
    public static WebApplication Configure(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseSerilogRequestLogging();

        app.UseApiExceptionHandling();
        app.UseUpstreamFailureHandling();

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "CodeExample API Gateway v1");
            options.SwaggerEndpoint("/swagger/user/v1/swagger.json", "User Service");
            options.SwaggerEndpoint("/swagger/finance/v1/swagger.json", "Finance Service");
            options.DocumentTitle = "CodeExample API Gateway";
        });

        app.UseAuthentication();

        app.UseRateLimiter();

        app.UseAuthorization();

        return app;
    }

    private static IApplicationBuilder UseApiExceptionHandling(this WebApplication app)
    {
        var logger = app.Logger;

        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();

            await ProblemDetailsWriter.WriteUnhandledAsync(
                context,
                feature?.Error,
                "gateway.unhandled_exception");

            if (feature?.Error is not null)
            {
                logger.LogError(feature.Error, "Необработанное исключение при обработке {Path}", context.Request.Path);
            }
        }));

        return app;
    }

    private static IApplicationBuilder UseUpstreamFailureHandling(this WebApplication app)
    {
        app.UseStatusCodePages(async statusCodeContext =>
        {
            var response = statusCodeContext.HttpContext.Response;

            if (response.StatusCode is StatusCodes.Status502BadGateway or StatusCodes.Status503ServiceUnavailable
                or StatusCodes.Status504GatewayTimeout)
            {
                var (title, detail) = response.StatusCode switch
                {
                    StatusCodes.Status504GatewayTimeout =>
                        ("Gateway Timeout", "The upstream service did not answer in time. Retry the request later."),
                    StatusCodes.Status503ServiceUnavailable =>
                        ("Service Unavailable", "The upstream service is not available. Retry the request later."),
                    _ => ("Bad Gateway", "The upstream service could not be reached. Retry the request later.")
                };

                await ProblemDetailsWriter.WriteAsync(
                    statusCodeContext.HttpContext,
                    response.StatusCode,
                    title,
                    detail,
                    "gateway.upstream_unavailable");
            }
        });

        return app;
    }
}
