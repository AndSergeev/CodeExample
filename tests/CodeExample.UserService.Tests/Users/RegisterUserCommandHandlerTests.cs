using System.Data.Common;
using FluentAssertions;
using Moq;
using CodeExample.Shared.Security;
using CodeExample.UserService.Application.Users.RegisterUser;
using CodeExample.UserService.Domain.Entities;
using Xunit;

namespace CodeExample.UserService.Tests.Users;

/// <summary>Тесты сценария регистрации.</summary>
public sealed class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithAFreeName_WritesBothRowsInsideOneTransactionAndCommitsIt()
    {
        var transaction = new Mock<DbTransaction>().Object;
        var repository = TestFixture.Repository();
        var unitOfWork = TestFixture.UnitOfWork(transaction);

        var handler = new RegisterUserCommandHandler(
            repository.Object,
            unitOfWork.Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RegisterUserCommandHandler>());

        var result = await handler.Handle(new RegisterUserCommand("Alice", "supersecret"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Обе строки обязаны уйти в одну транзакцию и закоммититься один раз: если коммит
        // потерять, в базе не останется ничего, а ответ при этом будет успешным.
        repository.Verify(
            x => x.AddAsync(It.IsAny<User>(), transaction, It.IsAny<CancellationToken>()),
            Times.Once);

        repository.Verify(
            x => x.AddRefreshTokenAsync(It.IsAny<RefreshToken>(), transaction, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAFreeName_RegistersTheUserAndIssuesTokens()
    {
        var repository = TestFixture.Repository();
        var unitOfWork = TestFixture.UnitOfWork();
        var handler = new RegisterUserCommandHandler(
            repository.Object,
            unitOfWork.Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RegisterUserCommandHandler>());

        var result = await handler.Handle(
            new RegisterUserCommand("  Alice  ", "supersecret"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        result.Value.User.Name.Should().Be("Alice");
        result.Value.User.CreatedAtUtc.Should().Be(TestFixture.Now);

        result.Value.Tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        result.Value.Tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        result.Value.Tokens.AccessTokenExpiresAtUtc.Should().Be(TestFixture.Now.AddMinutes(15));
        result.Value.Tokens.RefreshTokenExpiresAtUtc.Should().Be(TestFixture.Now.AddDays(7));

        repository.Verify(
            x => x.AddAsync(It.Is<User>(user => user.Name == "Alice"), null, It.IsAny<CancellationToken>()),
            Times.Once);

        unitOfWork.Verify(x => x.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_StoresAHashAndNeverThePlainPassword()
    {
        var hasher = TestFixture.PasswordHasher;
        var repository = TestFixture.Repository();
        var handler = new RegisterUserCommandHandler(
            repository.Object,
            TestFixture.UnitOfWork().Object,
            hasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RegisterUserCommandHandler>());

        await handler.Handle(new RegisterUserCommand("Bob", "supersecret"), CancellationToken.None);

        repository.Verify(
            x => x.AddAsync(
                It.Is<User>(user => user.Password != "supersecret"
                                    && hasher.Verify("supersecret", user.Password)),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WritesTheSessionOfTheNewUser()
    {
        var repository = TestFixture.Repository();
        var handler = new RegisterUserCommandHandler(
            repository.Object,
            TestFixture.UnitOfWork().Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RegisterUserCommandHandler>());

        var result = await handler.Handle(
            new RegisterUserCommand("Carol", "supersecret"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        repository.Verify(
            x => x.AddRefreshTokenAsync(
                It.Is<RefreshToken>(token =>
                    token.UserId == result.Value.User.Id
                    && token.ExpiresAtUtc == TestFixture.Now.AddDays(7)),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheNameIsTaken_ReturnsAConflictAndDoesNotPersist()
    {
        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.NameExistsAsync("Alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var unitOfWork = TestFixture.UnitOfWork();

        var handler = new RegisterUserCommandHandler(
            repository.Object,
            unitOfWork.Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RegisterUserCommandHandler>());

        var result = await handler.Handle(new RegisterUserCommand("Alice", "supersecret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.name_taken");
        result.Error.Type.Should().Be(Shared.Abstractions.ErrorType.Conflict);

        repository.Verify(x => x.AddAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()), Times.Never);

        unitOfWork.Verify(x => x.BeginAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheInsertLosesTheRaceForTheName_ReturnsTheSameConflictAndWritesNoSession()
    {
        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var unitOfWork = TestFixture.UnitOfWork();

        var handler = new RegisterUserCommandHandler(
            repository.Object,
            unitOfWork.Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RegisterUserCommandHandler>());

        var result = await handler.Handle(new RegisterUserCommand("Alice", "supersecret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.name_taken");
        result.Error.Type.Should().Be(Shared.Abstractions.ErrorType.Conflict);

        repository.Verify(
            x => x.AddRefreshTokenAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    public async Task Handle_WithAnUnusableName_ReturnsAValidationFailure(string name)
    {
        var repository = TestFixture.Repository();
        var handler = new RegisterUserCommandHandler(
            repository.Object,
            TestFixture.UnitOfWork().Object,
            TestFixture.PasswordHasher,
            TestFixture.TokenIssuer(),
            TestFixture.Clock,
            TestFixture.Logger<RegisterUserCommandHandler>());

        var result = await handler.Handle(new RegisterUserCommand(name, "supersecret"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(Shared.Abstractions.ErrorType.Validation);
        repository.Verify(x => x.AddAsync(It.IsAny<User>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }
}
