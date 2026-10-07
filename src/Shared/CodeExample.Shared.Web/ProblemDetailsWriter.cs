using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CodeExample.Shared.Web;

/// <summary>
/// Пишет problem-ответы RFC 7807 вручную: конвейер должен выразить отказ в любой точке, в том числе
/// внутри событий JWT, где MVC ещё недоступен.
/// </summary>
public static class ProblemDetailsWriter
{
    public const string ContentType = "application/problem+json";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Сопоставляет тип доменной ошибки с HTTP-статусом для клиента.</summary>
    public static int ToStatusCode(Abstractions.ErrorType errorType) => errorType switch
    {
        Abstractions.ErrorType.Validation => StatusCodes.Status400BadRequest,
        Abstractions.ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        Abstractions.ErrorType.NotFound => StatusCodes.Status404NotFound,
        Abstractions.ErrorType.Conflict => StatusCodes.Status409Conflict,
        Abstractions.ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>Сериализует problem-нагрузку в уже созданный ответ.</summary>
    public static Task WriteAsync(HttpContext context, ProblemDetails problem)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(problem);

        problem.Status ??= context.Response.StatusCode;
        problem.Extensions.TryAdd("traceId", context.TraceIdentifier);

        context.Response.ContentType = ContentType;
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }

    /// <summary>Собирает и пишет problem-ответ. Ничего не делает, если ответ уже начат.</summary>
    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string? errorCode = null)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        context.Response.StatusCode = statusCode;

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            problem.Extensions["errorCode"] = errorCode;
        }

        problem.Extensions["traceId"] = context.TraceIdentifier;

        return WriteAsync(context, problem);
    }

    /// <summary>Превращает доменную ошибку в результат, который возвращает эндпоинт.</summary>
    public static IResult ToProblem(Abstractions.Error error, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(context);

        var statusCode = ToStatusCode(error.Type);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = TitleFor(statusCode),
            Detail = error.Message,
            Instance = context.Request.Path
        };

        problem.Extensions["errorCode"] = error.Code;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        return Results.Json(problem, contentType: ContentType, statusCode: statusCode);
    }

    /// <summary>Пишет problem-ответ на необработанное исключение конвейера.</summary>
    /// <param name="context">Текущий запрос.</param>
    /// <param name="exception">Исключение, всплывшее из обработчика, или null.</param>
    /// <param name="errorCode">Код отказа этого хоста.</param>
    public static Task WriteUnhandledAsync(HttpContext context, Exception? exception, string errorCode)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);

        if (context.RequestAborted.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        // Битое тело запроса это отказ вызывающего, а не сбой сервиса.
        if (exception is BadHttpRequestException badRequest)
        {
            return WriteAsync(
                context,
                badRequest.StatusCode,
                "Bad Request",
                "The request could not be read. Check the route, query string and body parameters.",
                "request.malformed");
        }

        return WriteAsync(
            context,
            StatusCodes.Status500InternalServerError,
            "Internal Server Error",
            "The request could not be completed because of an unexpected error.",
            errorCode);
    }

    private static string TitleFor(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        StatusCodes.Status503ServiceUnavailable => "Service Unavailable",
        _ => "Internal Server Error"
    };
}
