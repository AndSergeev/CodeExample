using MediatR;
using Quartz;
using CodeExample.CurrencyService.Application.Synchronization.SyncCurrencyRates;

namespace CodeExample.CurrencyService.Api.HostedServices;

[DisallowConcurrentExecution]
public sealed class CurrencyRateSyncJob : IJob
{
    private readonly ISender _sender;
    private readonly ILogger<CurrencyRateSyncJob> _logger;

    public CurrencyRateSyncJob(ISender sender, ILogger<CurrencyRateSyncJob> logger)
    {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(logger);

        _sender = sender;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _logger.LogInformation(
            "Синхронизация курсов запущена экземпляром {InstanceId} по срабатыванию {FireInstanceId}",
            context.Scheduler.SchedulerInstanceId,
            context.FireInstanceId);

        var result = await _sender
            .Send(new SyncCurrencyRatesCommand(), context.CancellationToken);

        if (result.IsFailure)
        {
            _logger.LogError(
                "Синхронизация курсов завершилась отказом с кодом {ErrorCode}: {ErrorMessage}",
                result.Error.Code,
                result.Error.Message);

            throw new JobExecutionException(
                new InvalidOperationException($"{result.Error.Code}: {result.Error.Message}"),
                refireImmediately: false);
        }

        _logger.LogInformation(
            "Синхронизация курсов получила от ЦБ {FetchedCurrencies} курс(ов)",
            result.Value.FetchedCurrencies);
    }
}
