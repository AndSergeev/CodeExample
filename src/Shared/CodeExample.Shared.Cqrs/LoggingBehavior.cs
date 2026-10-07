using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.Shared.Abstractions;

namespace CodeExample.Shared.Cqrs;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly string RequestName = typeof(TRequest).Name;

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        _logger.LogInformation("Обработка {RequestName}", RequestName);

        var response = await next();

        if (response is Result { IsFailure: true } failure)
        {
            _logger.LogWarning(
                "{RequestName} завершился отказом с кодом {ErrorCode}: {ErrorMessage}",
                RequestName,
                failure.Error.Code,
                failure.Error.Message);
        }
        else
        {
            _logger.LogInformation("{RequestName} обработан", RequestName);
        }

        return response;
    }
}
