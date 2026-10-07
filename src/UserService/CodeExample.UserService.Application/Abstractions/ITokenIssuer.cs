using Microsoft.Extensions.Options;
using CodeExample.Shared.Security;
using CodeExample.UserService.Domain.Entities;
using CodeExample.UserService.Contracts;

namespace CodeExample.UserService.Application.Abstractions;

/// <summary>
/// Создаёт пару access/refresh вместе с сессией, которой она принадлежит.
/// </summary>
public interface ITokenIssuer
{
    /// <summary>Строит пару токенов и сессию, которую идентифицирует refresh-токен.</summary>
    /// <returns>Ответ для отправки и сессию, которую вызывающий обязан сохранить.</returns>
    (AuthTokensResponse Tokens, RefreshToken Session) Issue(User user, DateTime now);
}

/// <inheritdoc />
public sealed class TokenIssuer : ITokenIssuer
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRefreshTokenGenerator _refreshTokenGenerator;
    private readonly JwtOptions _options;

    public TokenIssuer(
        IJwtTokenGenerator jwtTokenGenerator,
        IRefreshTokenGenerator refreshTokenGenerator,
        IOptions<JwtOptions> options)
    {
        ArgumentNullException.ThrowIfNull(jwtTokenGenerator);
        ArgumentNullException.ThrowIfNull(refreshTokenGenerator);
        ArgumentNullException.ThrowIfNull(options);

        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenGenerator = refreshTokenGenerator;
        _options = options.Value;
    }

    public (AuthTokensResponse Tokens, RefreshToken Session) Issue(User user, DateTime now)
    {
        ArgumentNullException.ThrowIfNull(user);

        var accessToken = _jwtTokenGenerator.CreateAccessToken(user.Id, user.Name, now);
        var accessTokenExpiresAt = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var refreshToken = _refreshTokenGenerator.CreateToken();
        var refreshTokenLifetime = TimeSpan.FromDays(_options.RefreshTokenLifetimeDays);

        var session = RefreshToken.Issue(
            user.Id,
            _refreshTokenGenerator.HashToken(refreshToken),
            now,
            refreshTokenLifetime);

        var tokens = new AuthTokensResponse(
            accessToken,
            refreshToken,
            accessTokenExpiresAt,
            session.ExpiresAtUtc);

        return (tokens, session);
    }
}
