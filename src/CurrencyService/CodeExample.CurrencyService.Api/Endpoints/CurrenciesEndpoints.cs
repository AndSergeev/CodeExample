using MediatR;
using Microsoft.AspNetCore.Mvc;
using CodeExample.CurrencyService.Application.Currencies.GetAllCurrencies;
using CodeExample.CurrencyService.Application.Currencies.GetCurrenciesByIds;
using CodeExample.CurrencyService.Application.Currencies.GetCurrencyById;
using CodeExample.CurrencyService.Contracts;
using CodeExample.Shared.Web;

namespace CodeExample.CurrencyService.Api.Endpoints;

/// <summary>
/// Ручки сервиса курсов.
/// </summary>
internal static class CurrenciesEndpoints
{
    /// <summary>Регистрирует все маршруты валют по пути, объявленному в контрактах.</summary>
    public static IEndpointRouteBuilder MapCurrenciesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapInternalApi(CurrencyRoutes.Base);

        group.MapPost(CurrencyRoutes.ByIds, GetByIdsAsync)
            .WithName("GetCurrenciesByIds")
            .WithSummary("Resolves several currencies at once. Called by the Finance service.")
            .Produces<IReadOnlyList<CurrencyResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet(CurrencyRoutes.ById, GetByIdAsync)
            .WithName("GetCurrencyById")
            .WithSummary("Resolves a single currency.")
            .Produces<CurrencyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet(string.Empty, GetAllAsync)
            .WithName("GetAllCurrencies")
            .WithSummary("Returns the currencies the Central Bank of Russia still publishes.")
            .Produces<IReadOnlyList<CurrencyResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return endpoints;
    }

    private static async Task<IResult> GetByIdsAsync(
        [FromBody] CurrenciesByIdsRequest request,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await sender
            .Send(new GetCurrenciesByIdsQuery(request?.Ids ?? Array.Empty<string>()), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ProblemDetailsWriter.ToProblem(result.Error, context);
    }

    private static async Task<IResult> GetByIdAsync(
        string id,
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await sender
            .Send(new GetCurrencyByIdQuery(id), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ProblemDetailsWriter.ToProblem(result.Error, context);
    }

    private static async Task<IResult> GetAllAsync(
        ISender sender,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await sender
            .Send(new GetAllCurrenciesQuery(), cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ProblemDetailsWriter.ToProblem(result.Error, context);
    }
}
