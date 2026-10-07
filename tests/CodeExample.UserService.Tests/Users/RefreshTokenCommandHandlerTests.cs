using System.Data.Common;
using FluentAssertions;
using Moq;
using CodeExample.Shared.Security;
using CodeExample.UserService.Application.Users.RefreshToken;
using CodeExample.UserService.Domain.Abstractions;
using CodeExample.UserService.Domain.Entities;
using Xunit;

namespace CodeExample.UserService.Tests.Users;

/// <summary>Тесты ротации refresh-токена.</summary>
public sealed class RefreshTokenCommandHandlerTests
{
    private static readonly IRefreshTokenGenerator TokenGenerator = new RefreshTokenGenerator();

    [Fact]
    public async Task Handle_WithAnActiveSession_RotatesItAndReturnsANewPair()
    {
        var user = TestFixture.ExistingUser();
        var rawToken = "an-active-refresh-token";

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.ConsumeAsync(
                TokenGenerator.HashToken(rawToken),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenConsumptionResult(RefreshTokenConsumption.Consumed, user.Id));
        repository
            .Setup(x => x.FindByIdAsync(
                user.Id,
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var unitOfWork = TestFixture.UnitOfWork();

        var handler = new RefreshTokenCommandHandler(
            repository.Object,
            unitOfWork.Object,
            TokenGenerator,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RefreshTokenCommandHandler>());

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrWhiteSpace();

        repository.Verify(
            x => x.ConsumeAsync(
                TokenGenerator.HashToken(rawToken),
                TestFixture.Now,
                TestFixture.Now,
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), null, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(x => x.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheTokenWasAlreadyConsumed_ReportsTheReplacedSessionWithoutIssuingAReplacement()
    {
        var rawToken = "an-already-consumed-refresh-token";

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.ConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenConsumptionResult(
                RefreshTokenConsumption.AlreadyReplaced,
                Guid.NewGuid()));

        var handler = new RefreshTokenCommandHandler(
            repository.Object,
            TestFixture.UnitOfWork().Object,
            TokenGenerator,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RefreshTokenCommandHandler>());

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Shared.Abstractions.ErrorType.Unauthorized);
        result.Error.Code.Should().Be(RefreshTokenErrors.SessionReplaced);

        repository.Verify(
            x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(RefreshTokenConsumption.NotUsable)]
    [InlineData(RefreshTokenConsumption.AlreadyReplaced)]
    public async Task Handle_WhenTheTokenIsNotFresh_ReportsTheOutcomeTheRepositoryReturned(
        RefreshTokenConsumption outcome)
    {
        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.ConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenConsumptionResult(outcome, TestFixture.ExistingUser().Id));

        var handler = new RefreshTokenCommandHandler(
            repository.Object,
            TestFixture.UnitOfWork().Object,
            TokenGenerator,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RefreshTokenCommandHandler>());

        var result = await handler.Handle(new RefreshTokenCommand("a-token"), CancellationToken.None);

        var expected = outcome == RefreshTokenConsumption.AlreadyReplaced
            ? RefreshTokenErrors.SessionReplaced
            : RefreshTokenErrors.Invalid;

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WithAnExpiredOrRevokedSession_ReportsItAsInvalidRatherThanAsAReplacedSession()
    {
        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.ConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenConsumptionResult(
                RefreshTokenConsumption.NotUsable,
                TestFixture.ExistingUser().Id));

        var handler = new RefreshTokenCommandHandler(
            repository.Object,
            TestFixture.UnitOfWork().Object,
            TokenGenerator,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RefreshTokenCommandHandler>());

        var result = await handler.Handle(new RefreshTokenCommand("an-expired-refresh-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(RefreshTokenErrors.Invalid);
        result.Error.Code.Should().NotBe(RefreshTokenErrors.SessionReplaced);
    }

    [Fact]
    public async Task Handle_WithATokenNoSessionMatches_ReportsItAsInvalidRatherThanAsAReplacedSession()
    {
        var repository = TestFixture.Repository();
        var unitOfWork = TestFixture.UnitOfWork();

        var handler = new RefreshTokenCommandHandler(
            repository.Object,
            unitOfWork.Object,
            TokenGenerator,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RefreshTokenCommandHandler>());

        var result = await handler.Handle(new RefreshTokenCommand("not-a-real-token"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Shared.Abstractions.ErrorType.Unauthorized);
        result.Error.Code.Should().Be(RefreshTokenErrors.Invalid);
        result.Error.Code.Should().NotBe(RefreshTokenErrors.SessionReplaced);

        repository.Verify(
            x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ConsumesTheTokenAndPassesTheCurrentMomentAsBothTheDeadlineAndTheRevocationTime()
    {
        var user = TestFixture.ExistingUser();
        var rawToken = "an-active-refresh-token";

        DateTime? expiredBefore = null;
        DateTime? revokedAt = null;

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.ConsumeAsync(
                TokenGenerator.HashToken(rawToken),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, DateTime, DbTransaction?, CancellationToken>((_, now, revoked, _, _) =>
            {
                expiredBefore = now;
                revokedAt = revoked;
            })
            .ReturnsAsync(new RefreshTokenConsumptionResult(RefreshTokenConsumption.Consumed, user.Id));
        repository
            .Setup(x => x.FindByIdAsync(
                user.Id,
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new RefreshTokenCommandHandler(
            repository.Object,
            TestFixture.UnitOfWork().Object,
            TokenGenerator,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RefreshTokenCommandHandler>());

        await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        expiredBefore.Should().Be(TestFixture.Now);
        revokedAt.Should().Be(TestFixture.Now);
    }

    [Fact]
    public async Task Handle_WhenTheAccountIsGone_ReturnsUnauthorized()
    {
        var user = TestFixture.ExistingUser();
        var rawToken = "a-session-whose-account-was-deleted";

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.ConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenConsumptionResult(RefreshTokenConsumption.Consumed, user.Id));

        var unitOfWork = TestFixture.UnitOfWork();
        var handler = new RefreshTokenCommandHandler(
            repository.Object,
            unitOfWork.Object,
            TokenGenerator,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RefreshTokenCommandHandler>());

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(RefreshTokenErrors.Invalid);

        repository.Verify(
            x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ReadsTheAccountThroughTheSameTransactionThatLockedTheSession()
    {
        var user = TestFixture.ExistingUser();
        var transaction = new Mock<DbTransaction>().Object;

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.ConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                transaction,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenConsumptionResult(RefreshTokenConsumption.Consumed, user.Id));
        repository
            .Setup(x => x.FindByIdAsync(user.Id, transaction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new RefreshTokenCommandHandler(
            repository.Object,
            TestFixture.UnitOfWork(transaction).Object,
            TokenGenerator,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RefreshTokenCommandHandler>());

        var result = await handler.Handle(new RefreshTokenCommand("an-active-refresh-token"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Строка сессии заперта этой транзакцией и ещё не закоммичена: чтение через другое
        // соединение из пула не увидело бы её состояния.
        repository.Verify(
            x => x.FindByIdAsync(user.Id, transaction, It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            x => x.AddRefreshTokenAsync(
                It.IsAny<RefreshToken>(),
                transaction,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
