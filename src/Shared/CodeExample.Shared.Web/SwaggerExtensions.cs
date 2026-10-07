using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using CodeExample.Shared.Security;

namespace CodeExample.Shared.Web;

/// <summary>Регистрация Swagger: собирается одинаково, из конфигурации приходит только описание.</summary>
public static class SwaggerExtensions
{
    private const string BearerSchemeId = "Bearer";

    /// <summary>Регистрирует документ OpenAPI.</summary>
    public static IServiceCollection AddSwaggerDocumentation(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SwaggerOptions>(configuration);

        var options = configuration.ReadOptions<SwaggerOptions>();

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(swagger =>
        {
            swagger.SwaggerDoc(
                options.Version,
                new OpenApiInfo
                {
                    Title = options.Title,
                    Version = options.Version,
                    Description = options.Description
                });

            if (BuildSecurityScheme(options.Scheme) is { } scheme)
            {
                swagger.AddSecurityDefinition(scheme.Id, scheme.Scheme);
                swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    [scheme.Scheme] = Array.Empty<string>()
                });
            }
        });

        return services;
    }

    public static string SwaggerJsonPath(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var version = configuration.ReadOptions<SwaggerOptions>().Version;

        return $"/swagger/{version}/swagger.json";
    }

    private static (string Id, OpenApiSecurityScheme Scheme)? BuildSecurityScheme(
        SwaggerSecurityScheme securityScheme)
    {
        switch (securityScheme)
        {
            case SwaggerSecurityScheme.Bearer:
                return (BearerSchemeId, WithReference(BearerSchemeId, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description =
                        "Paste the access token returned by the User service. " +
                        "It is validated by the API gateway and by every service."
                }));

            case SwaggerSecurityScheme.ApiKey:
                return (InternalApiKeyAuthenticationHandler.PolicyName, WithReference(
                    InternalApiKeyAuthenticationHandler.PolicyName,
                    new OpenApiSecurityScheme
                    {
                        Name = InternalApiKeyOptions.HeaderName,
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Description =
                            $"Shared key configured as {nameof(InternalApiKeyOptions)}:" +
                            $"{nameof(InternalApiKeyOptions.ApiKey)}."
                    }));

            case SwaggerSecurityScheme.None:
                return null;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(securityScheme),
                    securityScheme,
                    "Неизвестная схема безопасности.");
        }
    }

    private static OpenApiSecurityScheme WithReference(string id, OpenApiSecurityScheme scheme)
    {
        scheme.Reference = new OpenApiReference
        {
            Id = id,
            Type = ReferenceType.SecurityScheme
        };

        return scheme;
    }
}
