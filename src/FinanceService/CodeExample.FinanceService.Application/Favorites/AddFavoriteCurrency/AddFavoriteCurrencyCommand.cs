using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using CodeExample.FinanceService.Domain.Abstractions;
using CodeExample.FinanceService.Domain.Entities;
using CodeExample.Shared.Abstractions;
using CodeExample.CurrencyService.Contracts;
using CodeExample.FinanceService.Contracts;

namespace CodeExample.FinanceService.Application.Favorites.AddFavoriteCurrency;

/// <summary>Добавляет валюту в избранное пользователя.</summary>
/// <param name="UserId">Id пользователя, берётся из проверенного access-токена.</param>
/// <param name="CurrencyId">Идентификатор валюты в сервисе курсов.</param>
public sealed record AddFavoriteCurrencyCommand(Guid UserId, string CurrencyId)
    : IRequest<Result<FavoriteCurrencyResponse>>;

internal sealed class AddFavoriteCurrencyCommandValidator : AbstractValidator<AddFavoriteCurrencyCommand>
{
    public AddFavoriteCurrencyCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().WithMessage("A user id is required.");
        RuleFor(x => x.CurrencyId)
            .NotEmpty().WithMessage("A currency id is required.")
            .MaximumLength(UserFavoriteCurrency.CurrencyIdMaxLength)
            .WithMessage($"A currency id is at most {UserFavoriteCurrency.CurrencyIdMaxLength} characters long.");
    }
}

/// <inheritdoc />
internal sealed class AddFavoriteCurrencyCommandHandler
    : IRequestHandler<AddFavoriteCurrencyCommand, Result<FavoriteCurrencyResponse>>
{
    private readonly IUserFavoriteCurrencyRepository _repository;
    private readonly ICurrencyClient _currencyClient;
    private readonly ILogger<AddFavoriteCurrencyCommandHandler> _logger;

    public AddFavoriteCurrencyCommandHandler(
        IUserFavoriteCurrencyRepository repository,
        ICurrencyClient currencyClient,
        ILogger<AddFavoriteCurrencyCommandHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(currencyClient);
        ArgumentNullException.ThrowIfNull(logger);

        _repository = repository;
        _currencyClient = currencyClient;
        _logger = logger;
    }

    public async Task<Result<FavoriteCurrencyResponse>> Handle(
        AddFavoriteCurrencyCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<CurrencyResponse> found;
        try
        {
            found = await _currencyClient.GetByIdsAsync([request.CurrencyId], cancellationToken);
        }
        catch (CurrencyReferenceException exception)
        {
            _logger.LogError(
                exception,
                "Не удалось проверить валюту {CurrencyId} в сервисе курсов",
                request.CurrencyId);

            return Result.Failure<FavoriteCurrencyResponse>(Error.Unavailable(
                "currency_reference.unavailable",
                "The currency service could not be reached."));
        }

        if (found.Count == 0)
        {
            return Result.Failure<FavoriteCurrencyResponse>(Error.NotFound(
                "currency.not_found",
                $"The currency '{request.CurrencyId}' was not found in the currency service."));
        }

        var favorite = UserFavoriteCurrency.Create(request.UserId, request.CurrencyId);

        var added = await _repository.AddAsync(favorite, cancellationToken: cancellationToken);

        if (added == 0)
        {
            return Result.Failure<FavoriteCurrencyResponse>(Error.Conflict(
                "favorite.already_tracked",
                $"The currency '{request.CurrencyId}' is already tracked by this user."));
        }

        _logger.LogInformation(
            "Пользователь {UserId} начал отслеживать валюту {CurrencyId}",
            request.UserId,
            request.CurrencyId);

        return Result.Success(favorite.ToResponse());
    }
}
