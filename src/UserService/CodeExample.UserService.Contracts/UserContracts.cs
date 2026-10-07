namespace CodeExample.UserService.Contracts;

/// <summary>Данные аккаунта, безопасные для возврата клиенту.</summary>
public sealed record UserResponse(Guid Id, string Name, DateTime CreatedAtUtc);

/// <summary>Пара токенов, выданная клиенту после успешной аутентификации.</summary>
public sealed record AuthTokensResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAtUtc,
    DateTime RefreshTokenExpiresAtUtc);

/// <summary>Результат успешной регистрации.</summary>
public sealed record RegisterUserResponse(UserResponse User, AuthTokensResponse Tokens);

/// <summary>Результат успешного входа.</summary>
public sealed record LoginUserResponse(UserResponse User, AuthTokensResponse Tokens);

/// <summary>Тело запроса на регистрацию.</summary>
public sealed record RegisterUserRequest(string Name, string Password);

/// <summary>Тело запроса на вход.</summary>
public sealed record LoginUserRequest(string Name, string Password);

/// <summary>Тело запроса на обновление токена; токен необязателен, потому что может прийти в HttpOnly cookie.</summary>
public sealed record RefreshTokenRequest(string? RefreshToken);

/// <summary>Тело запроса на выход; токен необязателен по той же причине.</summary>
public sealed record LogoutRequest(string? RefreshToken);

/// <summary>
/// Пути эндпоинтов аутентификации.
/// </summary>
public static class UserRoutes
{
    public const string AuthBase = "api/auth";

    public const string Register = "register";

    public const string Login = "login";

    public const string Refresh = "refresh";

    public const string Logout = "logout";

    public const string Me = "me";
}
