using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace CodeExample.Shared.Resilience;

/// <summary>
/// Настройка конвейера устойчивости для исходящих HTTP-вызовов.
/// </summary>
public static class ResilienceExtensions
{
    private const int MaxRetryAttempts = 3;

    private static readonly TimeSpan FirstRetryDelay = TimeSpan.FromSeconds(2);

    /// <summary>Проверяет настройки конвейера и отказывает, если попасть в них невозможно.</summary>
    /// <remarks>
    /// Задержки перед повторами растут вдвое, а дрожание разбрасывает каждую из них в пределах
    /// от половины до полутора: <c>2 + 4 + 8</c> превращается в диапазон от 7 до 21 секунды.
    /// Верхняя граница и есть бюджет пауз, ниже которого общий таймаут обрывал бы последнюю
    /// попытку, и любой отказ приходил бы как истёкшее время, каким бы он ни был.
    /// </remarks>
    public static IHttpStandardResiliencePipelineBuilder AddBoundedResilience(
        this IHttpClientBuilder builder,
        TimeSpan attemptTimeout,
        TimeSpan totalTimeout)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var refusal = ValidateAttemptAgainstTotal(attemptTimeout, totalTimeout);

        if (refusal is not null)
        {
            throw new InvalidOperationException(refusal);
        }

        return builder.AddStandardResilienceHandler(resilience =>
        {
            resilience.AttemptTimeout.Timeout = attemptTimeout;

            // Именно настройка, а не расчёт: у вызывающего может быть причина держать
            // общий бюджет шире худшего случая, и она обязана доходить до конвейера.
            resilience.TotalRequestTimeout.Timeout = totalTimeout;

            // Размыкатель цепи требует выборку минимум вдвое больше таймаута попытки,
            // иначе запуск падает на валидации настроек.
            resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2;

            resilience.Retry.MaxRetryAttempts = MaxRetryAttempts;
            resilience.Retry.Delay = FirstRetryDelay;
            resilience.Retry.UseJitter = true;
        });
    }

    /// <summary>
    /// Проверяет, что заданный общий бюджет вмещает все попытки вместе с паузами между ними.
    /// </summary>
    /// <returns>Текст отказа или <c>null</c>, если настройка согласована.</returns>
    public static string? ValidateAttemptAgainstTotal(TimeSpan attemptTimeout, TimeSpan totalTimeout)
    {
        if (attemptTimeout <= TimeSpan.Zero)
        {
            return "Таймаут попытки обязан быть положительным.";
        }

        var worstCase = WorstCaseFor(attemptTimeout);

        return totalTimeout >= worstCase
            ? null
            : $"Общий таймаут {totalTimeout} меньше худшего случая {worstCase}: " +
              $"попыток {MaxRetryAttempts + 1} по {attemptTimeout} плюс паузы. " +
              "Последняя попытка будет обрываться общим таймаутом.";
    }

    /// <summary>Сколько времени уйдёт, если каждая попытка упрётся в свой таймаут.</summary>
    public static TimeSpan WorstCaseFor(TimeSpan attemptTimeout) =>
        attemptTimeout * (MaxRetryAttempts + 1) + BackoffBudget();

    /// <summary>
    /// Общий бюджет для вызывающего, у которого нет своей настройки.
    /// </summary>
    /// <remarks>
    /// Считается от <see cref="WorstCaseFor"/>, а не отдельным множителем: у короткого
    /// таймаута попытки паузы между повторами весят больше самого таймаута, и любой
    /// постоянный множитель на малых значениях дал бы бюджет меньше необходимого.
    /// Четверть сверху это запас на медленный, но живой ответ.
    /// </remarks>
    public static TimeSpan TotalFor(TimeSpan attemptTimeout) => WorstCaseFor(attemptTimeout) * 1.25;

    private static TimeSpan BackoffBudget()
    {
        var median = TimeSpan.Zero;
        var delay = FirstRetryDelay;

        for (var attempt = 0; attempt < MaxRetryAttempts; attempt++)
        {
            median += delay;
            delay *= 2;
        }

        return median * 1.5;
    }
}
