using FluentValidation;
using MediatR;
using CodeExample.CurrencyService.Domain.Abstractions;
using CodeExample.Shared.Abstractions;
using CodeExample.CurrencyService.Contracts;

namespace CodeExample.CurrencyService.Application.Currencies.GetCurrenciesByIds;

/// <summary>
/// Резолвит несколько валют сразу. Этот эндпоинт вызывает сервис финансов, чтобы
/// превратить набор идентификаторов из избранного в курсы.
/// </summary>
/// <param name="Ids">Идентификаторы валют.</param>
public sealed record GetCurrenciesByIdsQuery(IReadOnlyCollection<string> Ids)
    : IRequest<Result<IReadOnlyList<CurrencyResponse>>>;

/// <summary>Правила входных данных для <see cref="GetCurrenciesByIdsQuery"/>.</summary>
internal sealed class GetCurrenciesByIdsQueryValidator : AbstractValidator<GetCurrenciesByIdsQuery>
{
    private const int MaxBatchSize = 500;

    public GetCurrenciesByIdsQueryValidator()
    {
        RuleFor(x => x.Ids)
            .NotNull().WithMessage("The identifier list is required.")
            .Must(ids => ids is { Count: > 0 }).WithMessage("At least one currency id is required.")
            .Must(ids => ids.Count <= MaxBatchSize)
            .WithMessage($"At most {MaxBatchSize} currency ids can be requested at once.");
    }
}

/// <inheritdoc />
internal sealed class GetCurrenciesByIdsQueryHandler
    : IRequestHandler<GetCurrenciesByIdsQuery, Result<IReadOnlyList<CurrencyResponse>>>
{
    private readonly ICurrencyRepository _repository;

    public GetCurrenciesByIdsQueryHandler(ICurrencyRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<CurrencyResponse>>> Handle(
        GetCurrenciesByIdsQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currencies = await _repository
            .GetByIdsAsync(request.Ids, cancellationToken);

        IReadOnlyList<CurrencyResponse> response = currencies
            .Select(currency => currency.ToResponse())
            .ToList();

        return Result.Success(response);
    }
}
