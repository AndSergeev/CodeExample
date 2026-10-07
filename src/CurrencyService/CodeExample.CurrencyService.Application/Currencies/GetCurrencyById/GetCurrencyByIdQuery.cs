using FluentValidation;
using MediatR;
using CodeExample.CurrencyService.Domain.Abstractions;
using CodeExample.Shared.Abstractions;
using CodeExample.CurrencyService.Contracts;

namespace CodeExample.CurrencyService.Application.Currencies.GetCurrencyById;

/// <summary>Находит одну валюту, например чтобы проверить избранное перед сохранением.</summary>
/// <param name="Id">Идентификатор валюты.</param>
public sealed record GetCurrencyByIdQuery(string Id) : IRequest<Result<CurrencyResponse>>;

/// <summary>Валидатор для <see cref="GetCurrencyByIdQuery"/>.</summary>
internal sealed class GetCurrencyByIdQueryValidator : AbstractValidator<GetCurrencyByIdQuery>
{
    public GetCurrencyByIdQueryValidator() =>
        RuleFor(x => x.Id).NotEmpty().WithMessage("A currency id is required.");
}

/// <inheritdoc />
internal sealed class GetCurrencyByIdQueryHandler : IRequestHandler<GetCurrencyByIdQuery, Result<CurrencyResponse>>
{
    private readonly ICurrencyRepository _repository;

    public GetCurrencyByIdQueryHandler(ICurrencyRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    public async Task<Result<CurrencyResponse>> Handle(
        GetCurrencyByIdQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var currency = await _repository.GetByIdAsync(request.Id, cancellationToken);

        return currency is null
            ? Result.Failure<CurrencyResponse>(Error.NotFound(
                "currency.not_found",
                $"The currency '{request.Id}' was not found."))
            : Result.Success(currency.ToResponse());
    }
}
