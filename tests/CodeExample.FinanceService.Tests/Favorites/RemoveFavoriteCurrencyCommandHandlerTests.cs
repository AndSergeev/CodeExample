using FluentAssertions;
using Moq;
using CodeExample.FinanceService.Application.Favorites.RemoveFavoriteCurrency;
using CodeExample.FinanceService.Domain.Abstractions;
using CodeExample.Shared.Abstractions;
using Xunit;

namespace CodeExample.FinanceService.Tests.Favorites;

/// <summary>Тесты удаления валюты из избранного.</summary>
public sealed class RemoveFavoriteCurrencyCommandHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string CurrencyId = "R01235";

    [Fact]
    public async Task Handle_ForATrackedCurrency_RemovesIt()
    {
        var repository = TestFixture.FavoritesRepository();
        repository
            .Setup(x => x.RemoveAsync(UserId, CurrencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new RemoveFavoriteCurrencyCommand(UserId, CurrencyId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        repository.Verify(
            x => x.RemoveAsync(UserId, CurrencyId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheCurrencyIsNotTracked_ReturnsNotFound()
    {
        var repository = TestFixture.FavoritesRepository();

        var handler = CreateHandler(repository);

        var result = await handler.Handle(
            new RemoveFavoriteCurrencyCommand(UserId, CurrencyId),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("favorite.not_found");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_PassesTheCommandIdentifiersToTheRepositoryUnchanged()
    {
        var repository = TestFixture.FavoritesRepository();
        repository
            .Setup(x => x.RemoveAsync(UserId, CurrencyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = CreateHandler(repository);

        await handler.Handle(
            new RemoveFavoriteCurrencyCommand(UserId, CurrencyId),
            CancellationToken.None);

        repository.Verify(
            x => x.RemoveAsync(UserId, CurrencyId, It.IsAny<CancellationToken>()),
            Times.Once);

        repository.VerifyNoOtherCalls();
    }

    private static RemoveFavoriteCurrencyCommandHandler CreateHandler(
        Mock<IUserFavoriteCurrencyRepository> repository) =>
        new(repository.Object, TestFixture.Logger<RemoveFavoriteCurrencyCommandHandler>());
}
