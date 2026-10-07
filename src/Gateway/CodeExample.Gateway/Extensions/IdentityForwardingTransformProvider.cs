using System.Security.Claims;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace CodeExample.Gateway.Extensions;

/// <summary>
/// Переписывает заголовки идентичности, которые шлюз пересылает бэкенду.
/// </summary>
public sealed class IdentityForwardingTransformProvider : ITransformProvider
{
    private const string UserIdHeader = "X-User-Id";

    private const string UserNameHeader = "X-User-Name";

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.AddRequestTransform(transformContext =>
        {
            var request = transformContext.ProxyRequest;
            var user = transformContext.HttpContext.User;

            request.Headers.Remove(UserIdHeader);
            request.Headers.Remove(UserNameHeader);

            if (user.Identity?.IsAuthenticated != true)
            {
                return ValueTask.CompletedTask;
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? user.FindFirst("sub")?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                request.Headers.TryAddWithoutValidation(UserIdHeader, userId);
            }

            var userName = user.FindFirst(ClaimTypes.Name)?.Value
                           ?? user.FindFirst("name")?.Value
                           ?? user.Identity.Name;

            if (!string.IsNullOrWhiteSpace(userName))
            {
                request.Headers.TryAddWithoutValidation(UserNameHeader, userName);
            }

            return ValueTask.CompletedTask;
        });
    }
}
