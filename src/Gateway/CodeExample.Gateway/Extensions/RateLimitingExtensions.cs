using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using CodeExample.Shared.Web;

namespace CodeExample.Gateway.Extensions;

internal static class RateLimitingExtensions
{
    public const string PolicyName = "gateway-fixed-window";

    public static IServiceCollection AddGatewayRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<Configuration.RateLimitingOptions>(configuration);

        services.AddRateLimiter(limiter =>
        {
            limiter.AddPolicy(PolicyName, httpContext =>
            {
                var options = httpContext.RequestServices
                    .GetRequiredService<IOptions<Configuration.RateLimitingOptions>>()
                    .Value;

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ResolvePartitionKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = TimeSpan.FromSeconds(options.WindowSeconds),
                        QueueLimit = options.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });

            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.OnRejected = (context, cancellationToken) => new ValueTask(
                ProblemDetailsWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status429TooManyRequests,
                    "Too Many Requests",
                    "The request limit for this client has been reached. Retry later.",
                    "gateway.rate_limit_exceeded"));
        });

        return services;
    }

    private static string ResolvePartitionKey(HttpContext context)
    {
        var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? context.User.FindFirst("sub")?.Value;

        return string.IsNullOrWhiteSpace(userId)
            ? $"ip:{context.Connection.RemoteIpAddress?.ToString() ?? "unknown"}"
            : $"user:{userId}";
    }
}
