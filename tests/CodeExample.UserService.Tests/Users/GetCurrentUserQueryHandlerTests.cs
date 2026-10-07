using FluentAssertions;
using System.Data.Common;
using Moq;
using CodeExample.UserService.Application.Users.GetCurrentUser;
using CodeExample.UserService.Domain.Entities;
using Xunit;

namespace CodeExample.UserService.Tests.Users;

/// <summary>Тесты запроса текущего пользователя.</summary>
public sealed class GetCurrentUserQueryHandlerTests
{
    [Fact]
    public async Task Handle_ForAnExistingUser_ReturnsTheProfile()
    {
        var user = TestFixture.ExistingUser("Alice");

        var repository = TestFixture.Repository();
        repository
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<DbTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new GetCurrentUserQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCurrentUserQuery(user.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Name.Should().Be("Alice");
        result.Value.CreatedAtUtc.Should().Be(TestFixture.Now);
    }

    [Fact]
    public async Task Handle_ForAnUnknownUser_ReturnsNotFound()
    {
        var repository = TestFixture.Repository();
        var handler = new GetCurrentUserQueryHandler(repository.Object);

        var result = await handler.Handle(new GetCurrentUserQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.not_found");
        result.Error.Type.Should().Be(Shared.Abstractions.ErrorType.NotFound);
    }
}
