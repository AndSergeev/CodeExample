using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using CodeExample.Shared.Security;

namespace CodeExample.Shared.Web;

/// <summary>Аутентификация всех сервисов.</summary>
public static class AuthenticationExtensions
{
    /// <summary>Регистрирует JWT bearer по секции Jwt и валидирует настройки при старте.</summary>
    public static IServiceCollection AddSharedJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JwtOptions>(configuration);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options => ConfigureJwtBearer(options, configuration));

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Регистрирует схему общего ключа для вызовов между микросервисами.
    /// </summary>
    public static IServiceCollection AddInternalApiKeyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Проверка при старте, а не при первом вызове: пустой ключ означает, что справочник
        // недоступен соседям, и узнать об этом лучше сразу.
        services
            .AddOptions<InternalApiKeyOptions>()
            .Bind(configuration.GetSection(nameof(InternalApiKeyOptions)))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services
            .AddAuthentication()
            .AddScheme<InternalApiKeyAuthenticationOptions, InternalApiKeyAuthenticationHandler>(
                InternalApiKeyAuthenticationOptions.DefaultScheme,
                _ => { });

        return services;
    }

    /// <summary>
    /// Регистрирует схему общего ключа вместе с требующей его политикой.
    /// </summary>
    public static IServiceCollection AddInternalOnlyAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddInternalApiKeyAuthentication(configuration);

        services
            .AddAuthorizationBuilder()
            .AddPolicy(InternalApiKeyAuthenticationHandler.PolicyName, policy => policy
                .AddAuthenticationSchemes(InternalApiKeyAuthenticationOptions.DefaultScheme)
                .RequireAuthenticatedUser());

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions options, IConfiguration configuration)
    {
        var jwt = configuration.ReadOptions<JwtOptions>();

        options.MapInboundClaims = false;
        options.SaveToken = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();

                if (context.Response.HasStarted)
                {
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;

                await ProblemDetailsWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    "A valid bearer token is required to call this endpoint.",
                    "auth.unauthorized");
            },
            OnForbidden = context => ProblemDetailsWriter.WriteAsync(
                context.HttpContext,
                StatusCodes.Status403Forbidden,
                "Forbidden",
                "The authenticated user is not allowed to perform this action.",
                "auth.forbidden")
        };
    }
}
