using FluentAssertions;
using FluentValidation;
using MediatR;
using CodeExample.Shared.Abstractions;
using CodeExample.Shared.Cqrs;
using Xunit;

namespace CodeExample.UserService.Tests.Behaviours;

/// <summary>Тесты behaviour конвейера запросов.</summary>
public sealed class ValidationBehaviorTests
{
    /// <summary>Запрос, обработчик которого возвращает необобщённый результат.</summary>
    public sealed record PlainRequest(string Value) : IRequest<Result>;

    /// <summary>Запрос, обработчик которого возвращает результат с полезной нагрузкой.</summary>
    public sealed record PayloadRequest(string Value) : IRequest<Result<string>>;

    public sealed class PlainRequestValidator : AbstractValidator<PlainRequest>
    {
        public PlainRequestValidator() =>
            RuleFor(x => x.Value).NotEmpty().WithMessage("The value is required.");
    }

    public sealed class PayloadRequestValidator : AbstractValidator<PayloadRequest>
    {
        public PayloadRequestValidator() =>
            RuleFor(x => x.Value).MinimumLength(3).WithMessage("The value is too short.");
    }

    [Fact]
    public async Task Handle_WithAValidRequest_InvokesTheHandler()
    {
        var behavior = new ValidationBehavior<PlainRequest, Result>(
            new[] { new PlainRequestValidator() });

        var invoked = false;

        var response = await behavior.Handle(
            new PlainRequest("ok"),
            _ =>
            {
                invoked = true;
                return Task.FromResult(Result.Success());
            },
            CancellationToken.None);

        invoked.Should().BeTrue();
        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAnInvalidRequest_ReturnsAValidationFailureForANonGenericResult()
    {
        var behavior = new ValidationBehavior<PlainRequest, Result>(
            new[] { new PlainRequestValidator() });

        var response = await behavior.Handle(
            new PlainRequest(string.Empty),
            _ => throw new InvalidOperationException("Обработчик не должен быть достигнут."),
            CancellationToken.None);

        response.IsFailure.Should().BeTrue();
        response.Error.Type.Should().Be(ErrorType.Validation);
        response.Error.Code.Should().Be("request.validation_failed");
        response.Error.Message.Should().Contain("The value is required.");
    }

    [Fact]
    public async Task Handle_WithAnInvalidRequest_ReturnsAValidationFailureForAGenericResult()
    {
        var behavior = new ValidationBehavior<PayloadRequest, Result<string>>(
            new[] { new PayloadRequestValidator() });

        var response = await behavior.Handle(
            new PayloadRequest("ab"),
            _ => throw new InvalidOperationException("Обработчик не должен быть достигнут."),
            CancellationToken.None);

        response.IsFailure.Should().BeTrue();
        response.Error.Code.Should().Be("request.validation_failed");
        response.Error.Message.Should().Contain("The value is too short.");
    }

    [Fact]
    public async Task Handle_WithoutValidators_InvokesTheHandler()
    {
        var behavior = new ValidationBehavior<PlainRequest, Result>(Array.Empty<IValidator<PlainRequest>>());

        var response = await behavior.Handle(
            new PlainRequest(string.Empty),
            _ => Task.FromResult(Result.Success()),
            CancellationToken.None);

        response.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UnhandledExceptionBehavior_TurnsAnExceptionIntoATechnicalFailure()
    {
        var behavior = new UnhandledExceptionBehavior<PlainRequest, Result>(TestFixture.Logger<UnhandledExceptionBehavior<PlainRequest, Result>>());

        var response = await behavior.Handle(
            new PlainRequest("ok"),
            _ => throw new InvalidOperationException("бум"),
            CancellationToken.None);

        response.IsFailure.Should().BeTrue();
        response.Error.Code.Should().Be("request.unhandled_exception");
    }

    [Fact]
    public async Task UnhandledExceptionBehavior_TurnsAnExceptionIntoATechnicalFailureForAGenericResult()
    {
        var behavior = new UnhandledExceptionBehavior<PayloadRequest, Result<string>>(
            TestFixture.Logger<UnhandledExceptionBehavior<PayloadRequest, Result<string>>>());

        var response = await behavior.Handle(
            new PayloadRequest("ok"),
            _ => throw new InvalidOperationException("бум"),
            CancellationToken.None);

        response.IsFailure.Should().BeTrue();
        response.Error.Code.Should().Be("request.unhandled_exception");
    }

    [Fact]
    public async Task UnhandledExceptionBehavior_DoesNotSwallowCancellation()
    {
        var behavior = new UnhandledExceptionBehavior<PlainRequest, Result>(TestFixture.Logger<UnhandledExceptionBehavior<PlainRequest, Result>>());

        var act = () => behavior.Handle(
            new PlainRequest("ok"),
            _ => throw new OperationCanceledException(),
            CancellationToken.None);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
