using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using CodeExample.Shared.Abstractions;

namespace CodeExample.Shared.Web;

/// <summary>Помощники регистрации публичного API.</summary>
public static class ApiEndpointExtensions
{
    /// <summary>
    /// Создаёт группу маршрутов, доступных авторизованному пользователю.
    /// </summary>
    public static RouteGroupBuilder MapUserApi(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        string tag)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        ArgumentException.ThrowIfNullOrWhiteSpace(tag);

        return endpoints
            .MapGroup(prefix)
            .RequireAuthorization()
            .WithTags(tag);
    }

    /// <summary>
    /// Создаёт внутреннюю группу маршрутов.
    /// </summary>
    public static RouteGroupBuilder MapInternalApi(
        this IEndpointRouteBuilder endpoints,
        string prefix,
        string tag = "Internal")
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);

        return endpoints
            .MapGroup(prefix)
            .RequireAuthorization(InternalApiKeyAuthenticationHandler.PolicyName)
            .WithTags(tag);
    }

    /// <summary>Читает идентификатор пользователя из проверенного access-токена.</summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? principal.FindFirst("sub")?.Value;

        return Guid.TryParse(value, out var id) ? id : null;
    }

    /// <summary>Problem-ответ для пользовательского маршрута.</summary>
    public static IResult UnauthorizedProblem(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return ProblemDetailsWriter.ToProblem(
            Error.Unauthorized("auth.unauthorized", "A valid token is required."),
            context);
    }
}
