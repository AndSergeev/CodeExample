using CodeExample.Shared.Abstractions;
using CodeExample.UserService.Contracts;

namespace CodeExample.UserService.Application.Users;

/// <summary>
/// Маппит доменные сущности на контракты.
/// </summary>
public static class UserMappings
{
    public static UserResponse ToResponse(this Domain.Entities.User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserResponse(user.Id, user.Name, user.CreatedAtUtc);
    }

    public static Result<UserResponse> ToResult(this Domain.Entities.User user) =>
        Result.Success(user.ToResponse());
}
