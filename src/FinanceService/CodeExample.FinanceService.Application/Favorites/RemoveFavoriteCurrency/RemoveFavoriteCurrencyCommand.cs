using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.FinanceService.Domain.Abstractions;
using CodeExample.Shared.Abstractions;

namespace CodeExample.FinanceService.Application.Favorites.RemoveFavoriteCurrency;

/// <summary>Удаляет валюту из избранного пользователя.</summary>
/// <param name="UserId">Id пользователя, берётся из проверенного access-токена.</param>
/// <param name="CurrencyId">Идентификатор валюты, которую нужно убрать.</param>
public sealed record RemoveFavoriteCurrencyCommand(Guid UserId, string CurrencyId) : IRequest<Result<bool>>;

internal sealed class RemoveFavoriteCurrencyCommandValidator : AbstractValidator<RemoveFavoriteCurrencyCommand>
{
    public RemoveFavoriteCurrencyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("A user id is required.");
        RuleFor(x => x.CurrencyId).NotEmpty().WithMessage("A currency id is required.");
    }
}

/// <inheritdoc />
internal sealed class RemoveFavoriteCurrencyCommandHandler
    : IRequestHandler<RemoveFavoriteCurrencyCommand, Result<bool>>
{
    private readonly IUserFavoriteCurrencyRepository _repository;
    private readonly ILogger<RemoveFavoriteCurrencyCommandHandler> _logger;

    public RemoveFavoriteCurrencyCommandHandler(
        IUserFavoriteCurrencyRepository repository,
        ILogger<RemoveFavoriteCurrencyCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(
        RemoveFavoriteCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var removed = await _repository
            .RemoveAsync(request.UserId, request.CurrencyId, cancellationToken);

        if (removed == 0)
        {
            return Result.Failure<bool>(Error.NotFound(
                "favorite.not_found",
                "The currency is not in the favorites of this user."));
        }

        _logger.LogInformation(
            "Пользователь {UserId} перестал отслеживать валюту {CurrencyId}",
            request.UserId,
            request.CurrencyId);

        return Result.Success(true);
    }
}
