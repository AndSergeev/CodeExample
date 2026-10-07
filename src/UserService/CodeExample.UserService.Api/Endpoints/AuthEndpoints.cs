using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using CodeExample.Shared.Abstractions;
using CodeExample.Shared.Security;
using CodeExample.Shared.Web;
using CodeExample.UserService.Application.Users;
using CodeExample.UserService.Contracts;
using CodeExample.UserService.Application.Users.GetCurrentUser;
using CodeExample.UserService.Application.Users.LoginUser;
using CodeExample.UserService.Application.Users.LogoutUser;
using CodeExample.UserService.Application.Users.RefreshToken;
using CodeExample.UserService.Application.Users.RegisterUser;

namespace CodeExample.UserService.Api.Endpoints;

/// <summary>
/// Эндпоинты аутентификации.
/// </summary>
internal static class AuthEndpoints
{
    /// <summary>Регистрирует все эндпоинты.</summary>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/" + UserRoutes.AuthBase)
            .WithTags("Authentication");

        group.MapPost(UserRoutes.Register, RegisterAsync)
            .AllowAnonymous()
            .WithName("RegisterUser")
            .WithSummary("Registers a new user and returns an access/refresh token pair.")
            .Produces<RegisterUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost(UserRoutes.Login, LoginAsync)
            .AllowAnonymous()
            .WithName("LoginUser")
            .WithSummary("Authenticates a user and returns an access/refresh token pair.")
            .Produces<LoginUserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost(UserRoutes.Refresh, RefreshAsync)
            .AllowAnonymous()
            .WithName("RefreshToken")
            .WithSummary("Exchanges a refresh token for a new token pair and rotates the session.")
            .Produces<AuthTokensResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost(UserRoutes.Logout, LogoutAsync)
            .RequireAuthorization()
            .WithName("LogoutUser")
            .WithSummary("Revokes the refresh sessions of the authenticated user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet(UserRoutes.Me, GetCurrentUserAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithSummary("Returns the profile of the authenticated user.")
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        [FromBody] RegisterUserRequest request,
        ISender sender,
        HttpContext context,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        var result = await sender
            .Send(new RegisterUserCommand(request.Name, request.Password), cancellationToken);

        if (result.IsFailure)
        {
            return ProblemDetailsWriter.ToProblem(result.Error, context);
        }

        WriteRefreshTokenCookie(context, result.Value.Tokens, jwtOptions.Value);

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> LoginAsync(
        [FromBody] LoginUserRequest request,
        ISender sender,
        HttpContext context,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        var result = await sender
            .Send(new LoginUserCommand(request.Name, request.Password), cancellationToken);

        if (result.IsFailure)
        {
            return ProblemDetailsWriter.ToProblem(result.Error, context);
        }

        WriteRefreshTokenCookie(context, result.Value.Tokens, jwtOptions.Value);

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> RefreshAsync(
        [FromBody] RefreshTokenRequest? request,
        ISender sender,
        HttpContext context,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        var options = jwtOptions.Value;
        var token = request?.RefreshToken;

        if (string.IsNullOrWhiteSpace(token)
            && options.UseRefreshTokenCookie
            && context.Request.Cookies.TryGetValue(options.RefreshTokenCookieName, out var cookieToken))
        {
            token = cookieToken;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return ProblemDetailsWriter.ToProblem(
                Error.Unauthorized(
                    "auth.refresh_token_required",
                    "A refresh token is required, either in the body or in the refresh cookie."),
                context);
        }

        var result = await sender
            .Send(new RefreshTokenCommand(token), cancellationToken);

        if (result.IsFailure)
        {
            // Проигранную гонку ротации не трогаем: у выигравшего запроса уже есть новая пара,
            // и стирание cookie выкинуло бы браузер из живой сессии.
            if (result.Error.Code != RefreshTokenErrors.SessionReplaced)
            {
                ClearRefreshTokenCookie(context, options);
            }

            return ProblemDetailsWriter.ToProblem(result.Error, context);
        }

        WriteRefreshTokenCookie(context, result.Value, options);

        return Results.Ok(result.Value);
    }

    private static async Task<IResult> LogoutAsync(
        [FromBody] LogoutRequest? request,
        ISender sender,
        HttpContext context,
        IOptions<JwtOptions> jwtOptions,
        CancellationToken cancellationToken)
    {
        var userId = context.User.GetUserId();

        if (userId is null)
        {
            return ApiEndpointExtensions.UnauthorizedProblem(context);
        }

        var options = jwtOptions.Value;
        var refreshToken = request?.RefreshToken;

        if (string.IsNullOrWhiteSpace(refreshToken)
            && options.UseRefreshTokenCookie
            && context.Request.Cookies.TryGetValue(options.RefreshTokenCookieName, out var cookieToken))
        {
            refreshToken = cookieToken;
        }

        var result = await sender
            .Send(new LogoutUserCommand(userId.Value, refreshToken), cancellationToken);

        if (result.IsFailure)
        {
            return ProblemDetailsWriter.ToProblem(result.Error, context);
        }

        ClearRefreshTokenCookie(context, options);

        return Results.NoContent();
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var userId = context.User.GetUserId();

        if (userId is null)
        {
            return ApiEndpointExtensions.UnauthorizedProblem(context);
        }

        var result = await sender
            .Send(new GetCurrentUserQuery(userId.Value), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ProblemDetailsWriter.ToProblem(result.Error, context);
    }

    private static void WriteRefreshTokenCookie(
        HttpContext context,
        AuthTokensResponse tokens,
        JwtOptions options)
    {
        if (!options.UseRefreshTokenCookie)
        {
            return;
        }

        context.Response.Cookies.Append(
            options.RefreshTokenCookieName,
            tokens.RefreshToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = $"/{UserRoutes.AuthBase}",
                Expires = new DateTimeOffset(tokens.RefreshTokenExpiresAtUtc, TimeSpan.Zero)
            });
    }

    private static void ClearRefreshTokenCookie(HttpContext context, JwtOptions options)
    {
        if (!options.UseRefreshTokenCookie)
        {
            return;
        }

        context.Response.Cookies.Delete(
            options.RefreshTokenCookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = $"/{UserRoutes.AuthBase}"
            });
    }
}
