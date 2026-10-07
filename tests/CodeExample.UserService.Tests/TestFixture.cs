using System.Data.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using CodeExample.Shared.Abstractions;
using CodeExample.Shared.Security;
using CodeExample.UserService.Application.Abstractions;
using CodeExample.UserService.Domain.Abstractions;
using CodeExample.UserService.Domain.Entities;

namespace CodeExample.UserService.Tests;

internal static class TestFixture
{
    public static readonly DateTime Now = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    public static JwtOptions JwtOptions => new()
    {
        SigningKey = "unit-test-signing-key-that-is-long-enough-0123456789",
        Issuer = "CodeExample.Tests",
        Audience = "CodeExample.Tests.Clients",
        AccessTokenLifetimeMinutes = 15,
        RefreshTokenLifetimeDays = 7
    };

    public static IOptions<JwtOptions> JwtOptionsWrapper => Options.Create(JwtOptions);

    public static IDateTimeProvider Clock => new FixedDateTimeProvider(Now);

    public static ITokenIssuer TokenIssuer() => new TokenIssuer(
        new JwtTokenGenerator(JwtOptionsWrapper),
        new RefreshTokenGenerator(),
        JwtOptionsWrapper);

    public static IPasswordHasher PasswordHasher => new StubPasswordHasher();

    public static Mock<IUserRepository> Repository()
    {
        var repository = new Mock<IUserRepository>(MockBehavior.Loose);

        repository
            .Setup(x => x.NameExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Вставка сообщает об одной записанной строке - столько даёт свободный логин.
        // Тест на проигранную гонку возвращает 0.
        repository
            .Setup(x => x.AddAsync(
                It.IsAny<User>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        repository
            .Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        repository
            .Setup(x => x.FindByIdAsync(
                It.IsAny<Guid>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        repository
            .Setup(x => x.ConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenConsumptionResult?)null);

        return repository;
    }

    public static Mock<IUnitOfWork> UnitOfWork(DbTransaction? transaction = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Loose);

        unitOfWork
            .Setup(x => x.BeginAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(transaction!);

        return unitOfWork;
    }

    public static User ExistingUser(string name = "existing-user")
    {
        var result = User.Create(name, "$2a$12$abcdefghijklmnopqrstuv", Now);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Тестового пользователя не удалось создать: {result.Error}");
        }

        return result.Value;
    }

    public static NullLogger<T> Logger<T>() => NullLogger<T>.Instance;

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public FixedDateTimeProvider(DateTime now) => UtcNow = now;

        public DateTime UtcNow { get; }
    }

    private sealed class StubPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed::{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == $"hashed::{password}";
    }
}
