using FluentValidation;
using MediatR;
using CodeExample.Shared.Abstractions;
using CodeExample.UserService.Domain.Abstractions;
using CodeExample.UserService.Contracts;

namespace CodeExample.UserService.Application.Users.GetCurrentUser;

/// <summary>Возвращает аутентифицированного пользователя.</summary>
public sealed record GetCurrentUserQuery(Guid UserId) : IRequest<Result<UserResponse>>;

internal sealed class GetCurrentUserQueryValidator : AbstractValidator<GetCurrentUserQuery>
{
    public GetCurrentUserQueryValidator() =>
        RuleFor(x => x.UserId).NotEmpty().WithMessage("A user id is required.");
}

/// <inheritdoc />
internal sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, Result<UserResponse>>
{
    private readonly IUserRepository _repository;

    public GetCurrentUserQueryHandler(IUserRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<Result<UserResponse>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _repository.FindByIdAsync(request.UserId, cancellationToken: cancellationToken);

        return user is null
            ? Result.Failure<UserResponse>(Error.NotFound("user.not_found", "The user was not found."))
            : user.ToResult();
    }
}
