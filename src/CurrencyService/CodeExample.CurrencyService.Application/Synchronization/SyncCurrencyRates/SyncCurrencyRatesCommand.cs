using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.CurrencyService.Domain.Abstractions;
using CodeExample.CurrencyService.Domain.Entities;
using CodeExample.Shared.Abstractions;

namespace CodeExample.CurrencyService.Application.Synchronization.SyncCurrencyRates;

/// <summary>
/// Забирает курсы на день у ЦБ РФ и сохраняет их.
/// </summary>
public sealed record SyncCurrencyRatesCommand : IRequest<Result<SyncCurrencyRatesResult>>;

/// <summary>Итог прогона синхронизации.</summary>
/// <param name="FetchedCurrencies">Сколько курсов прогон получил от ЦБ и попытался записать.</param>
public sealed record SyncCurrencyRatesResult(int FetchedCurrencies);

/// <inheritdoc />
internal sealed class SyncCurrencyRatesCommandHandler
    : IRequestHandler<SyncCurrencyRatesCommand, Result<SyncCurrencyRatesResult>>
{
    private readonly ICurrencyRateSource _rateSource;
    private readonly ICurrencyRepository _currencies;
    private readonly ILogger<SyncCurrencyRatesCommandHandler> _logger;

    public SyncCurrencyRatesCommandHandler(
        ICurrencyRateSource rateSource,
        ICurrencyRepository currencies,
        ILogger<SyncCurrencyRatesCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(rateSource);
        ArgumentNullException.ThrowIfNull(currencies);
        ArgumentNullException.ThrowIfNull(logger);

        _rateSource = rateSource;
        _currencies = currencies;
        _logger = logger;
    }

    public async Task<Result<SyncCurrencyRatesResult>> Handle(
        SyncCurrencyRatesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ratesResult = await _rateSource
            .FetchDailyRatesAsync(cancellationToken);

        if (ratesResult.IsFailure)
        {
            _logger.LogError(
                "Синхронизация курсов завершилась отказом: {ErrorCode} {ErrorMessage}",
                ratesResult.Error.Code,
                ratesResult.Error.Message);

            return Result.Failure<SyncCurrencyRatesResult>(ratesResult.Error);
        }

        var cbRates = ratesResult.Value;

        if (cbRates.Count == 0)
        {
            _logger.LogWarning("Синхронизация курсов получила от ЦБ РФ пустой список курсов");

            return Result.Failure<SyncCurrencyRatesResult>(Error.Failure(
                "sync.empty_response",
                "The Central Bank of Russia returned no currencies."));
        }

        var currencies = cbRates
            .Select(cbRate => Currency.CreateFromSource(
                cbRate.Id,
                cbRate.Name,
                cbRate.Value,
                cbRate.Nominal))
            .ToArray();

        var written = await _currencies.SynchronizeAsync(currencies, cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Синхронизация курсов записала {CurrencyCount} курс(ов) в {WrittenRows} строк",
            currencies.Length,
            written);

        return Result.Success(new SyncCurrencyRatesResult(currencies.Length));
    }
}
