using MediatR;
using Microsoft.Extensions.Options;
using CodeExample.CurrencyService.Application.Synchronization.SyncCurrencyRates;
using CodeExample.CurrencyService.Domain.Abstractions;
using CodeExample.CurrencyService.Infrastructure.Configuration;

namespace CodeExample.CurrencyService.Api.HostedServices;

/// <summary>
/// Выполняет одну синхронизацию сразу после старта, но только если таблица курсов пуста.
/// </summary>
public sealed class CurrencySyncStartupRunner : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<CurrencySyncStartupOptions> _options;
    private readonly ILogger<CurrencySyncStartupRunner> _logger;

    private Task? _startupSync;

    public CurrencySyncStartupRunner(
        IServiceScopeFactory scopeFactory,
        IOptions<CurrencySyncStartupOptions> options,
        ILogger<CurrencySyncStartupRunner> logger)
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_options.Value.RunOnStartup)
        {
            _logger.LogInformation("Стартовая синхронизация отключена настройкой");
            return Task.CompletedTask;
        }

        _startupSync = RunInitialSyncAsync(cancellationToken);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_startupSync is not null)
        {
            await _startupSync;
        }
    }

    private async Task RunInitialSyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var provider = scope.ServiceProvider;

            var currencies = provider.GetRequiredService<ICurrencyRepository>();
            var existingCount = await currencies.CountAsync(cancellationToken);

            if (existingCount > 0)
            {
                _logger.LogInformation(
                    "Стартовая синхронизация пропущена: уже сохранено курсов: {CurrencyCount}",
                    existingCount);
                return;
            }

            _logger.LogInformation("Таблица курсов пуста, выполняется начальная синхронизация");

            var sender = provider.GetRequiredService<ISender>();
            var result = await sender
                .Send(new SyncCurrencyRatesCommand(), cancellationToken);

            if (result.IsFailure)
            {
                _logger.LogWarning(
                    "Начальная синхронизация завершилась отказом с кодом {ErrorCode}: {ErrorMessage}. Запланированный прогон повторит попытку.",
                    result.Error.Code,
                    result.Error.Message);
                return;
            }

            _logger.LogInformation(
                "Начальная синхронизация получила от ЦБ {FetchedCurrencies} курс(ов)",
                result.Value.FetchedCurrencies);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Начальная синхронизация прервана остановкой хоста");
        }
        catch (Exception exception)
        {
            // Сбой не должен ждать остановки хоста: иначе он всплыл бы как необработанное
            // исключение при завершении, а сервис всё это время работал бы с пустой таблицей курсов.
            _logger.LogError(
                exception,
                "Начальная синхронизация завершилась сбоем. Запланированный прогон повторит попытку.");
        }
    }
}
