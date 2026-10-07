using System.ComponentModel.DataAnnotations;

namespace CodeExample.Shared.Security;

/// <summary>
/// Настройки JWT. Все сервисы проверяют токены одним и тем же симметричным ключом, поэтому тип общий.
/// </summary>
public sealed class JwtOptions
{
    [Required(ErrorMessage = "JwtOptions:SigningKey is required.")]
    [MinLength(32, ErrorMessage = "JwtOptions:SigningKey must be at least 32 characters long.")]
    public string SigningKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "JwtOptions:Issuer is required.")]
    public string Issuer { get; set; } = "CodeExample";

    [Required(ErrorMessage = "JwtOptions:Audience is required.")]
    public string Audience { get; set; } = "CodeExample.Clients";

    [Range(1, 1440, ErrorMessage = "JwtOptions:AccessTokenLifetimeMinutes must be between 1 and 1440.")]
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    [Range(1, 365, ErrorMessage = "JwtOptions:RefreshTokenLifetimeDays must be between 1 and 365.")]
    public int RefreshTokenLifetimeDays { get; set; } = 7;

    public bool UseRefreshTokenCookie { get; set; } = true;

    public string RefreshTokenCookieName { get; set; } = "codeexample_refresh_token";
}
