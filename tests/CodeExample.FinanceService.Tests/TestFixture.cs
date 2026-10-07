using System.Data.Common;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CodeExample.FinanceService.Application.Favorites;
using CodeExample.CurrencyService.Contracts;
using CodeExample.FinanceService.Domain.Abstractions;
using CodeExample.FinanceService.Contracts;
using CodeExample.FinanceService.Domain.Entities;
using CodeExample.Shared.Abstractions;

namespace CodeExample.FinanceService.Tests;

internal static class TestFixture
{
    public static readonly DateTime Now = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    public static NullLogger<T> Logger<T>() => NullLogger<T>.Instance;

    /// <summary>Мок репозитория избранного со значениями по умолчанию.</summary>
    public static Mock<IUserFavoriteCurrencyRepository> FavoritesRepository()
    {
        var repository = new Mock<IUserFavoriteCurrencyRepository>(MockBehavior.Loose);

        repository
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserFavoriteCurrency>());

        repository
            .Setup(x => x.AddAsync(
                It.IsAny<UserFavoriteCurrency>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        return repository;
    }

    /// <summary>Мок клиента справочника со значениями по умолчанию.</summary>
    public static Mock<ICurrencyClient> CurrencyClient()
    {
        var client = new Mock<ICurrencyClient>(MockBehavior.Loose);

        client
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CurrencyResponse>());

        return client;
    }

    /// <summary>Создаёт избранное через настоящую фабрику домена.</summary>
    public static UserFavoriteCurrency Favorite(Guid userId, string currencyId) =>
        UserFavoriteCurrency.Create(userId, currencyId);

    /// <summary>Создаёт ответ, который вернул бы справочник.</summary>
    /// <param name="rate">Рубли за одну единицу.</param>
    public static CurrencyResponse Currency(string id, string name = "Доллар США", decimal rate = 83.4839m) =>
        new(id, name, rate);
}
