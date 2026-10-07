using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CodeExample.Shared.Web;

/// <summary>Регистрация health-пробы: эндпоинт объявляется и отвечает одинаково во всех сервисах.</summary>
public static class HealthCheckExtensions
{
    public const string HealthPath = "/health";

    /// <summary>
    /// Настраивает пробу и возвращает builder проверок для стандартного AddCheck.
    /// </summary>
    public static IHealthChecksBuilder AddHealthProbe(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        _ = configuration.ServiceName();

        return services.AddHealthChecks();
    }

    internal static string ServiceName(this IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var name = configuration.ReadOptions<ServiceOptions>().Name;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException(
                $"Имя сервиса не настроено. " +
                $"Задайте {nameof(ServiceOptions)}:{nameof(ServiceOptions.Name)} в appsettings или через переменные среды.");
        }

        return name;
    }

    /// <summary>
    /// Маппит пробу на <see cref="HealthPath"/> с именем сервиса из секции <see cref="ServiceOptions"/>.
    /// </summary>
    public static IEndpointConventionBuilder MapHealthEndpoints(
        this IEndpointRouteBuilder endpoints,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(configuration);

        var serviceName = configuration.ServiceName();

        return endpoints
            .MapHealthChecks(HealthPath, new HealthCheckOptions
            {
                ResponseWriter = (context, report) => WriteResponseAsync(context, report, serviceName)
            })
            .AllowAnonymous()
            .WithTags("Diagnostics");
    }

    private static Task WriteResponseAsync(HttpContext context, HealthReport report, string serviceName)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        return context.Response.WriteAsync(JsonSerializer.Serialize(
            new
            {
                status = report.Status.ToString(),
                service = serviceName,
                durationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
                checks = report.Entries.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.Status.ToString())
            },
            JsonSerializerOptions));
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
}
