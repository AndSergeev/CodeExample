using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.Shared.Abstractions;

namespace CodeExample.Shared.Cqrs;

public sealed class UnhandledExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private static readonly string RequestName = typeof(TRequest).Name;

    private readonly ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> _logger;

    public UnhandledExceptionBehavior(ILogger<UnhandledExceptionBehavior<TRequest, TResponse>> logger)
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

        try
        {
            return await next();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "{RequestName} выбросил необработанное исключение",
                RequestName);

            return ResultFactory.CreateFailure<TResponse>(Error.Failure(
                "request.unhandled_exception",
                "The request could not be completed because of an unexpected error."));
        }
    }
}
