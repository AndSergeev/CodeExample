using MediatR;
using CodeExample.FinanceService.Application.Favorites;
using CodeExample.FinanceService.Application.Favorites.AddFavoriteCurrency;
using CodeExample.FinanceService.Application.Favorites.GetFavoriteCurrencies;
using CodeExample.FinanceService.Application.Favorites.RemoveFavoriteCurrency;
using CodeExample.Shared.Web;
using CodeExample.FinanceService.Contracts;

namespace CodeExample.FinanceService.Api.Endpoints;

/// <summary>
/// Валюты, которые отслеживает пользователь.
/// </summary>
internal static class FavoriteCurrenciesEndpoints
{
    /// <summary>Регистрация эндпоинтов.</summary>
    public static IEndpointRouteBuilder MapFavoriteCurrenciesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapUserApi(FinanceRoutes.FavoritesBase, "Favorites");

        group.MapGet(string.Empty, GetFavoritesAsync)
            .WithName("GetFavoriteCurrencies")
            .WithSummary("Lists the currencies the authenticated user tracks, with their current rates.")
            .Produces<IReadOnlyList<FavoriteRateResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapPost(string.Empty, AddFavoriteAsync)
            .WithName("AddFavoriteCurrency")
            .WithSummary("Starts tracking a currency for the authenticated user.")
            .Produces<FavoriteCurrencyResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        group.MapDelete(FinanceRoutes.ByCurrencyId, RemoveFavoriteAsync)
            .WithName("RemoveFavoriteCurrency")
            .WithSummary("Stops tracking a currency for the authenticated user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> GetFavoritesAsync(
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (context.User.GetUserId() is not { } userId)
        {
            return ApiEndpointExtensions.UnauthorizedProblem(context);
        }

        var result = await sender
            .Send(new GetFavoriteCurrenciesQuery(userId), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ProblemDetailsWriter.ToProblem(result.Error, context);
    }

    private static async Task<IResult> AddFavoriteAsync(
        AddFavoriteCurrencyRequest request,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (context.User.GetUserId() is not { } userId)
        {
            return ApiEndpointExtensions.UnauthorizedProblem(context);
        }

        var result = await sender
            .Send(new AddFavoriteCurrencyCommand(userId, request.CurrencyId), cancellationToken);

        return result.IsFailure
            ? ProblemDetailsWriter.ToProblem(result.Error, context)
            : Results.Created(FinanceRoutes.FavoritesPath, result.Value);
    }

    private static async Task<IResult> RemoveFavoriteAsync(
        string currencyId,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (context.User.GetUserId() is not { } userId)
        {
            return ApiEndpointExtensions.UnauthorizedProblem(context);
        }

        var result = await sender
            .Send(new RemoveFavoriteCurrencyCommand(userId, currencyId), cancellationToken);

        return result.IsSuccess
            ? Results.NoContent()
            : ProblemDetailsWriter.ToProblem(result.Error, context);
    }
}
