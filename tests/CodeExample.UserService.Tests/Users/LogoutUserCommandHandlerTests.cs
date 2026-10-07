using FluentAssertions;
using System.Data.Common;
using Moq;
using CodeExample.Shared.Security;
using CodeExample.UserService.Application.Users.LogoutUser;
using Xunit;

namespace CodeExample.UserService.Tests.Users;

/// <summary>Тесты сценария выхода.</summary>
public sealed class LogoutUserCommandHandlerTests
{
    private static readonly IRefreshTokenGenerator TokenGenerator = new RefreshTokenGenerator();

    [Fact]
    public async Task Handle_WithoutAToken_RevokesEveryActiveSessionOfTheUser()
    {
        var user = TestFixture.ExistingUser();

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repository
            .Setup(x => x.RevokeAllRefreshTokensAsync(
                user.Id,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var handler = CreateHandler(repository);

        var result = await handler.Handle(new LogoutUserCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        repository.Verify(
            x => x.RevokeAllRefreshTokensAsync(user.Id, TestFixture.Now, It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            x => x.RevokeRefreshTokenAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithASpecificToken_RevokesOnlyThatSession()
    {
        var user = TestFixture.ExistingUser();
        var rawToken = "the-refresh-token-returned-to-the-client";

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        repository
            .Setup(x => x.RevokeRefreshTokenAsync(
                user.Id,
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new LogoutUserCommand(user.Id, rawToken),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        repository.Verify(
            x => x.RevokeRefreshTokenAsync(
                user.Id,
                TokenGenerator.HashToken(rawToken),
                TestFixture.Now,
                It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            x => x.RevokeAllRefreshTokensAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheUserIsGone_ReturnsNotFound()
    {
        var repository = TestFixture.Repository();

        var handler = CreateHandler(repository);

        var result = await handler.Handle(new LogoutUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Shared.Abstractions.ErrorType.NotFound);

        repository.Verify(
            x => x.RevokeAllRefreshTokensAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNothingIsActive_SucceedsWithoutReportingAnError()
    {
        var user = TestFixture.ExistingUser();

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = CreateHandler(repository);

        var result = await handler.Handle(new LogoutUserCommand(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    private static LogoutUserCommandHandler CreateHandler(Mock<Domain.Abstractions.IUserRepository> repository) =>
        new(
            repository.Object,
            TokenGenerator,
            TestFixture.Clock,
            TestFixture.Logger<LogoutUserCommandHandler>());
}
