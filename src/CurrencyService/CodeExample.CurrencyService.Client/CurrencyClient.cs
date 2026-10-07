using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using CodeExample.CurrencyService.Contracts;

namespace CodeExample.CurrencyService.Client;

/// <summary>
/// HTTP-реализация <see cref="ICurrencyClient"/>.
/// </summary>
internal sealed class CurrencyClient : ICurrencyClient
{
    private sealed record ProblemBody(string? Title, string? Detail);

    private readonly HttpClient _httpClient;
    private readonly ILogger<CurrencyClient> _logger;

    public CurrencyClient(HttpClient httpClient, ILogger<CurrencyClient> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);

        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CurrencyResponse>> GetByIdsAsync(
        IReadOnlyCollection<string> currencyIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(currencyIds);

        if (currencyIds.Count == 0)
        {
            return Array.Empty<CurrencyResponse>();
        }

        HttpResponseMessage response;

        try
        {
            response = await _httpClient.PostAsJsonAsync(
                $"{CurrencyRoutes.Base}/{CurrencyRoutes.ByIds}",
                new CurrenciesByIdsRequest(currencyIds),
                JsonDefaults.Options,
                cancellationToken);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CurrencyReferenceException(
                $"Сервис курсов недоступен по адресу {_httpClient.BaseAddress}.",
                exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                throw await BuildExceptionAsync(response, cancellationToken);
            }

            try
            {
                var currencies = await response.Content
                    .ReadFromJsonAsync<List<CurrencyResponse>>(JsonDefaults.Options, cancellationToken);

                return currencies ?? new List<CurrencyResponse>();
            }
            catch (Exception exception) when (exception is JsonException or NotSupportedException)
            {
                throw new CurrencyReferenceException(
                    "Сервис курсов вернул тело, которое не удалось прочитать.",
                    exception);
            }
        }
    }

    private async Task<CurrencyReferenceException> BuildExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var statusCode = response.StatusCode;
        string? detail = null;

        try
        {
            var problem = await response.Content
                .ReadFromJsonAsync<ProblemBody>(JsonDefaults.Options, cancellationToken);

            detail = problem?.Detail ?? problem?.Title;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            _logger.LogWarning(
                exception,
                "Сервис курсов ответил статусом {StatusCode} и нечитаемым телом",
                (int)statusCode);
        }

        var message = detail is null
            ? $"Сервис курсов ответил статусом {(int)statusCode}."
            : $"Сервис курсов ответил статусом {(int)statusCode}: {detail}";

        return new CurrencyReferenceException(message)
        {
            StatusCode = (int)statusCode,
            ResponseBody = detail
        };
    }
}

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
