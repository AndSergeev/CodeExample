using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using CodeExample.Shared.Security;

namespace CodeExample.Shared.Web;

/// <summary>
/// Настройки схемы для внутреннего API-ключа.
/// </summary>
public sealed class InternalApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>Имя, под которым регистрируется схема.</summary>
    public const string DefaultScheme = "InternalApiKey";
}

/// <summary>Аутентифицирует соседние микросервисы по общему ключу.</summary>
public sealed class InternalApiKeyAuthenticationHandler
    : AuthenticationHandler<InternalApiKeyAuthenticationOptions>
{
    public const string PolicyName = "InternalApiKey";

    private readonly IOptions<InternalApiKeyOptions> _apiKeyOptions;

    public InternalApiKeyAuthenticationHandler(
        IOptionsMonitor<InternalApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IOptions<InternalApiKeyOptions> apiKeyOptions)
        : base(options, logger, encoder)
    {
        ArgumentNullException.ThrowIfNull(apiKeyOptions);
        _apiKeyOptions = apiKeyOptions;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(InternalApiKeyOptions.HeaderName, out var provided))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var expected = _apiKeyOptions.Value.ApiKey;

        if (string.IsNullOrEmpty(expected))
        {
            Logger.LogError(
                "Аутентификация по внутреннему API-ключу включена, но {Section}:{Property} не настроен.",
                nameof(InternalApiKeyOptions),
                nameof(InternalApiKeyOptions.ApiKey));

            return Task.FromResult(AuthenticateResult.Fail("The internal API key is not configured."));
        }

        if (!ConstantTimeEquals(expected, provided.ToString()))
        {
            Logger.LogWarning("Отклонён запрос с неверным внутренним API-ключом с пути {Path}.", Request.Path);
            return Task.FromResult(AuthenticateResult.Fail("The internal API key is invalid."));
        }

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, "internal-service"),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, "InternalService")
            },
            Scheme.Name);

        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, Scheme.Name)));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Detail = $"A valid {InternalApiKeyOptions.HeaderName} header is required to call this endpoint.",
            Instance = Request.Path
        };

        problem.Extensions["errorCode"] = "internal_api.unauthorized";

        return ProblemDetailsWriter.WriteAsync(Context, problem);
    }

    private static bool ConstantTimeEquals(string expected, string actual)
    {
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
        var actualBytes = System.Text.Encoding.UTF8.GetBytes(actual);

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}
