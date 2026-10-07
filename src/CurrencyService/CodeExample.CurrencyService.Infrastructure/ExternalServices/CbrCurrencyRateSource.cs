using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Polly.Timeout;
using Microsoft.Extensions.Logging;
using CodeExample.CurrencyService.Domain.Abstractions;
using CodeExample.Shared.Abstractions;

namespace CodeExample.CurrencyService.Infrastructure.ExternalServices;

/// <summary>
/// Читает курсы на день у ЦБ РФ.
/// </summary>
public sealed class CbrCurrencyRateSource : ICurrencyRateSource
{
    private const string ValuteElement = "Valute";
    private const string ExternalIdAttribute = "ID";

    private readonly HttpClient _httpClient;
    private readonly ILogger<CbrCurrencyRateSource> _logger;

    static CbrCurrencyRateSource() =>
        // .NET Core несёт лишь часть кодировок, Windows-1251 нужно зарегистрировать.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public CbrCurrencyRateSource(HttpClient httpClient, ILogger<CbrCurrencyRateSource> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<CurrencyRate>>> FetchDailyRatesAsync(
        CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;

        try
        {
            response = await _httpClient
                .GetAsync(string.Empty, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Эндпоинт ЦБ РФ недоступен");

            return Result.Failure<IReadOnlyList<CurrencyRate>>(Error.Failure(
                "cbr.unreachable",
                $"The Central Bank of Russia endpoint could not be reached: {exception.Message}"));
        }
        catch (Exception exception) when (
            (exception is TaskCanceledException or TimeoutRejectedException)
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Запрос в ЦБ РФ превысил время ожидания");

            return Result.Failure<IReadOnlyList<CurrencyRate>>(Error.Failure(
                "cbr.timeout",
                "The request to the Central Bank of Russia timed out."));
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Эндпоинт ЦБ РФ ответил статусом {StatusCode}",
                    (int)response.StatusCode);

                return Result.Failure<IReadOnlyList<CurrencyRate>>(Error.Failure(
                    "cbr.bad_status",
                    $"The Central Bank of Russia responded with status {(int)response.StatusCode}."));
            }

            try
            {
                await using var stream = await response.Content
                    .ReadAsStreamAsync(cancellationToken);

                var document = await XDocument
                    .LoadAsync(stream, LoadOptions.None, cancellationToken);

                var rates = Parse(document);

                return Result.Success(rates);
            }
            catch (Exception exception) when (
                exception is XmlException or IOException or DecoderFallbackException)
            {
                _logger.LogError(exception, "Ответ ЦБ РФ не удалось прочитать или разобрать");

                return Result.Failure<IReadOnlyList<CurrencyRate>>(Error.Failure(
                    "cbr.malformed_response",
                    "The Central Bank of Russia returned a response that could not be read."));
            }
        }
    }

    private IReadOnlyList<CurrencyRate> Parse(XDocument document)
    {
        var rates = new List<CurrencyRate>();

        var valutes = document
            .Descendants()
            .Where(element => element.Name.LocalName == ValuteElement);

        foreach (var valute in valutes)
        {
            var id = valute.Attribute(ExternalIdAttribute)?.Value;
            var name = ReadElementValue(valute, "Name");
            var nominalText = ReadElementValue(valute, "Nominal");
            var valueText = ReadElementValue(valute, "Value");

            if (string.IsNullOrWhiteSpace(id)
                || string.IsNullOrWhiteSpace(name)
                || string.IsNullOrWhiteSpace(valueText))
            {
                _logger.LogWarning("Пропущена неполная запись ЦБ РФ с идентификатором {CurrencyId}", id);
                continue;
            }

            if (!TryParseIntInvariant(nominalText, out var nominal) || nominal <= 0)
            {
                _logger.LogWarning("Пропущена запись ЦБ РФ {CurrencyId}: нечитаемый номинал '{Nominal}'", id, nominalText);
                continue;
            }

            if (!TryParseInvariant(valueText, out var value) || value <= 0)
            {
                _logger.LogWarning("Пропущена запись ЦБ РФ {CurrencyId}: нечитаемое значение '{Value}'", id, valueText);
                continue;
            }

            rates.Add(new CurrencyRate(id.Trim(), name.Trim(), nominal, value));
        }

        return rates;
    }

    private static string? ReadElementValue(XElement parent, string localName) =>
        parent.Elements().FirstOrDefault(element => element.Name.LocalName == localName)?.Value;

    private static bool TryParseInvariant(string? text, out decimal value)
    {
        value = 0m;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return decimal.TryParse(
            NormalizeNumber(text),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static bool TryParseIntInvariant(string? text, out int value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return int.TryParse(
            NormalizeNumber(text),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static string NormalizeNumber(string text)
    {
        var normalized = text.Trim()
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return normalized.Contains(',', StringComparison.Ordinal)
            ? normalized.Replace(",", ".", StringComparison.Ordinal)
            : normalized;
    }
}
