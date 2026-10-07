using FluentAssertions;
using Moq;
using CodeExample.CurrencyService.Contracts;
using CodeExample.FinanceService.Application.Favorites.GetFavoriteCurrencies;
using CodeExample.FinanceService.Domain.Abstractions;
using CodeExample.FinanceService.Domain.Entities;
using Xunit;

namespace CodeExample.FinanceService.Tests.Favorites;

/// <summary>
/// Тесты read-модели контекста финансов.
/// </summary>
public sealed class GetFavoriteCurrenciesQueryHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private const string UsdId = "R01235";
    private const string EurId = "R01239";
    private const string JpyId = "R01820";

    private static GetFavoriteCurrenciesQueryHandler CreateHandler(
        Mock<IUserFavoriteCurrencyRepository> repository,
        Mock<ICurrencyClient> currencyClient) =>
        new(repository.Object, currencyClient.Object, TestFixture.Logger<GetFavoriteCurrenciesQueryHandler>());

    [Fact]
    public async Task Handle_ResolvesEveryFavoriteInASingleCurrencyCall()
    {
        var repository = TestFixture.FavoritesRepository();
        repository
            .Setup(x => x.GetAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFavoriteCurrency>
            {
                TestFixture.Favorite(UserId, EurId),
                TestFixture.Favorite(UserId, UsdId)
            });

        var currencyClient = TestFixture.CurrencyClient();
        currencyClient
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CurrencyResponse>
            {
                TestFixture.Currency(UsdId, "Доллар США", 83.4839m),
                TestFixture.Currency(EurId, "Евро", 91.1234m)
            });

        var result = await CreateHandler(repository, currencyClient)
            .Handle(new GetFavoriteCurrenciesQuery(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);

        result.Value[0].CurrencyId.Should().Be(EurId);
        result.Value[1].CurrencyId.Should().Be(UsdId);
        result.Value[1].Name.Should().Be("Доллар США");
        result.Value[1].Rate.Should().Be(83.4839m);

        currencyClient.Verify(
            x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_KeepsTheOrderTheRepositoryReturned()
    {
        var repository = TestFixture.FavoritesRepository();
        repository
            .Setup(x => x.GetAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFavoriteCurrency>
            {
                TestFixture.Favorite(UserId, EurId),
                TestFixture.Favorite(UserId, UsdId)
            });

        var currencyClient = TestFixture.CurrencyClient();
        currencyClient
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CurrencyResponse>
            {
                TestFixture.Currency(EurId),
                TestFixture.Currency(UsdId)
            });

        var result = await CreateHandler(repository, currencyClient)
            .Handle(new GetFavoriteCurrenciesQuery(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value[0].CurrencyId.Should().Be(EurId);
        result.Value[1].CurrencyId.Should().Be(UsdId);
    }

    [Fact]
    public async Task Handle_ForAUserWithoutFavorites_DoesNotCallTheCurrencyService()
    {
        var repository = TestFixture.FavoritesRepository();
        var currencyClient = TestFixture.CurrencyClient();

        var result = await CreateHandler(repository, currencyClient)
            .Handle(new GetFavoriteCurrenciesQuery(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();

        currencyClient.Verify(
            x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheCurrencyServiceFails_SurfacesTheFailureInsteadOfPartialData()
    {
        var repository = TestFixture.FavoritesRepository();
        repository
            .Setup(x => x.GetAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFavoriteCurrency> { TestFixture.Favorite(UserId, UsdId) });

        var currencyClient = TestFixture.CurrencyClient();
        currencyClient
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CurrencyReferenceException("The currency service responded with status 503."));

        var result = await CreateHandler(repository, currencyClient)
            .Handle(new GetFavoriteCurrenciesQuery(UserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("currency_reference.unavailable");
    }

    [Fact]
    public async Task Handle_SkipsAFavoriteWhoseCurrencyCouldNotBeResolved()
    {
        const string danglingId = "R99999";

        var repository = TestFixture.FavoritesRepository();
        repository
            .Setup(x => x.GetAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFavoriteCurrency>
            {
                TestFixture.Favorite(UserId, UsdId),
                TestFixture.Favorite(UserId, danglingId)
            });

        var currencyClient = TestFixture.CurrencyClient();
        currencyClient
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CurrencyResponse>
            {
                TestFixture.Currency(UsdId)
            });

        var result = await CreateHandler(repository, currencyClient)
            .Handle(new GetFavoriteCurrenciesQuery(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value[0].CurrencyId.Should().Be(UsdId);
    }

    [Fact]
    public async Task Handle_ReturnsTheRateForOneUnitWhateverTheBankQuoted()
    {
        var repository = TestFixture.FavoritesRepository();
        repository
            .Setup(x => x.GetAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserFavoriteCurrency> { TestFixture.Favorite(UserId, JpyId) });

        var currencyClient = TestFixture.CurrencyClient();
        currencyClient
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CurrencyResponse>
            {
                TestFixture.Currency(JpyId, "Японская йена", 0.542753m)
            });

        var result = await CreateHandler(repository, currencyClient)
            .Handle(new GetFavoriteCurrenciesQuery(UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value[0].Rate.Should().Be(0.542753m);
    }

    [Fact]
    public void Validator_RequiresAUserId()
    {
        var validator = new GetFavoriteCurrenciesQueryValidator();

        validator.Validate(new GetFavoriteCurrenciesQuery(UserId)).IsValid.Should().BeTrue();
        validator.Validate(new GetFavoriteCurrenciesQuery(Guid.Empty)).IsValid.Should().BeFalse();
    }
}
