using Microsoft.Extensions.Options;

namespace CodeExample.CurrencyService.Client;

/// <summary>
/// Добавляет общий внутренний ключ к каждому вызову.
/// </summary>
internal sealed class InternalApiKeyHandler : DelegatingHandler
{
    private readonly IOptions<CurrencyClientOptions> _options;

    public InternalApiKeyHandler(IOptions<CurrencyClientOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var apiKey = _options.Value.ApiKey;

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.TryAddWithoutValidation(CurrencyClientOptions.ApiKeyHeaderName, apiKey);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
