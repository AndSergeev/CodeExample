using System.Data.Common;
using FluentAssertions;
using Moq;
using CodeExample.CurrencyService.Contracts;
using CodeExample.FinanceService.Application.Favorites.AddFavoriteCurrency;
using CodeExample.FinanceService.Domain.Entities;
using CodeExample.Shared.Abstractions;
using Xunit;

namespace CodeExample.FinanceService.Tests.Favorites;

/// <summary>Тесты добавления валюты в отслеживаемый набор.</summary>
public sealed class AddFavoriteCurrencyCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string CurrencyId = "R01235";

    [Fact]
    public async Task Handle_ForAKnownCurrency_StoresTheFavorite()
    {
        var repository = TestFixture.FavoritesRepository();
        var currencyClient = TestFixture.CurrencyClient();
        currencyClient
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { TestFixture.Currency(CurrencyId) });

        var handler = new AddFavoriteCurrencyCommandHandler(
            repository.Object,
            currencyClient.Object,
            TestFixture.Logger<AddFavoriteCurrencyCommandHandler>());

        var result = await handler.Handle(
            new AddFavoriteCurrencyCommand(UserId, CurrencyId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CurrencyId.Should().Be(CurrencyId);

        repository.Verify(
            x => x.AddAsync(
                It.Is<UserFavoriteCurrency>(favorite =>
                    favorite.UserId == UserId && favorite.CurrencyId == CurrencyId),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheCurrencyIsAlreadyTracked_ReturnsAConflict()
    {
        var repository = TestFixture.FavoritesRepository();

        repository
            .Setup(x => x.AddAsync(
                It.IsAny<UserFavoriteCurrency>(),
                It.IsAny<DbTransaction>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var currencyClient = TestFixture.CurrencyClient();
        currencyClient
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { TestFixture.Currency(CurrencyId) });

        var handler = new AddFavoriteCurrencyCommandHandler(
            repository.Object,
            currencyClient.Object,
            TestFixture.Logger<AddFavoriteCurrencyCommandHandler>());

        var result = await handler.Handle(
            new AddFavoriteCurrencyCommand(UserId, CurrencyId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("favorite.already_tracked");
        result.Error.Type.Should().Be(ErrorType.Conflict);

        repository.Verify(
            x => x.AddAsync(
                It.Is<UserFavoriteCurrency>(favorite =>
                    favorite.UserId == UserId && favorite.CurrencyId == CurrencyId),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheCurrencyDoesNotExist_ReturnsNotFoundAndStoresNothing()
    {
        var repository = TestFixture.FavoritesRepository();

        var currencyClient = TestFixture.CurrencyClient();
        currencyClient
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CurrencyResponse>());

        var handler = new AddFavoriteCurrencyCommandHandler(
            repository.Object,
            currencyClient.Object,
            TestFixture.Logger<AddFavoriteCurrencyCommandHandler>());

        var result = await handler.Handle(
            new AddFavoriteCurrencyCommand(UserId, CurrencyId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("currency.not_found");
        result.Error.Type.Should().Be(ErrorType.NotFound);

        repository.Verify(
            x => x.AddAsync(It.IsAny<UserFavoriteCurrency>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheCurrencyServiceIsUnavailable_PropagatesTheFailure()
    {
        var repository = TestFixture.FavoritesRepository();

        var currencyClient = TestFixture.CurrencyClient();
        currencyClient
            .Setup(x => x.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CurrencyReferenceException("The currency service responded with status 503."));

        var handler = new AddFavoriteCurrencyCommandHandler(
            repository.Object,
            currencyClient.Object,
            TestFixture.Logger<AddFavoriteCurrencyCommandHandler>());

        var result = await handler.Handle(
            new AddFavoriteCurrencyCommand(UserId, CurrencyId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("currency_reference.unavailable");

        repository.Verify(
            x => x.AddAsync(It.IsAny<UserFavoriteCurrency>(), null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Validator_RequiresBothIdentifiers()
    {
        var validator = new AddFavoriteCurrencyCommandValidator();

        validator.Validate(new AddFavoriteCurrencyCommand(UserId, CurrencyId)).IsValid.Should().BeTrue();
        validator.Validate(new AddFavoriteCurrencyCommand(Guid.Empty, CurrencyId)).IsValid.Should().BeFalse();
        validator.Validate(new AddFavoriteCurrencyCommand(UserId, string.Empty)).IsValid.Should().BeFalse();
    }
}
