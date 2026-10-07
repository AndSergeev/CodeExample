using FluentAssertions;
using CodeExample.Shared.Security;
using Moq;
using CodeExample.UserService.Application.Users.LoginUser;
using CodeExample.UserService.Domain.Entities;
using Xunit;

namespace CodeExample.UserService.Tests.Users;

/// <summary>Тесты сценария входа.</summary>
public sealed class LoginUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithAWrongPassword_ReturnsInvalidCredentialsAndDoesNotPersist()
    {
        var user = TestFixture.ExistingUser("Alice");
        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.FindByNameAsync("Alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new LoginUserCommandHandler(
            repository.Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<LoginUserCommandHandler>());

        var result = await handler.Handle(new LoginUserCommand("Alice", "wrong-password"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_credentials");
        result.Error.Type.Should().Be(Shared.Abstractions.ErrorType.Unauthorized);

        repository.Verify(
            x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownUser_ReturnsInvalidCredentials()
    {
        var repository = TestFixture.Repository();

        var handler = new LoginUserCommandHandler(
            repository.Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<LoginUserCommandHandler>());

        var result = await handler.Handle(new LoginUserCommand("nobody", "whatever"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("auth.invalid_credentials");

        result.Error.Type.Should().Be(Shared.Abstractions.ErrorType.Unauthorized);

        repository.Verify(
            x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithACorrectPassword_ReturnsTokensAndStoresTheSession()
    {
        var password = "supersecret";

        var user = User.Create("Alice", $"hashed::{password}", TestFixture.Now).Value;

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.FindByNameAsync("Alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new LoginUserCommandHandler(
            repository.Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<LoginUserCommandHandler>());

        var result = await handler.Handle(new LoginUserCommand("Alice", password), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.User.Id.Should().Be(user.Id);
        result.Value.Tokens.AccessToken.Should().NotBeNullOrWhiteSpace();

        repository.Verify(
            x => x.AddRefreshTokenAsync(
                It.Is<RefreshToken>(session => session.UserId == user.Id),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NormalisesTheNameBeforeLookingItUp()
    {
        var repository = TestFixture.Repository();

        var handler = new LoginUserCommandHandler(
            repository.Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<LoginUserCommandHandler>());

        await handler.Handle(new LoginUserCommand("  Alice  ", "whatever"), CancellationToken.None);

        repository.Verify(x => x.FindByNameAsync("Alice", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_StoresTheHashOfTheVeryTokenItHandsToTheCaller()
    {
        var password = "supersecret";
        var user = User.Create("Alice", $"hashed::{password}", TestFixture.Now).Value;
        var generator = new RefreshTokenGenerator();

        RefreshToken? stored = null;

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.FindByNameAsync("Alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repository
            .Setup(x => x.AddRefreshTokenAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<System.Data.Common.DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, System.Data.Common.DbTransaction?, CancellationToken>((session, _, _) => stored = session)
            .ReturnsAsync(1);

        var handler = new LoginUserCommandHandler(
            repository.Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<LoginUserCommandHandler>());

        var result = await handler.Handle(new LoginUserCommand("Alice", password), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        stored.Should().NotBeNull();

        stored!.TokenHash.Should().Be(
            generator.HashToken(result.Value.Tokens.RefreshToken),
            "клиент предъявит именно выданный токен, и обновление ищет сессию по его хешу");
    }
}
