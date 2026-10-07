namespace CodeExample.UserService.Domain.Abstractions;

/// <summary>Чем закончилась попытка погасить сессию обновления.</summary>
public enum RefreshTokenConsumption
{
    /// <summary>Сессия была активна и погашена этим вызовом.</summary>
    Consumed = 0,

    /// <summary>Для сессии уже выдали замену: её обменял другой запрос.</summary>
    AlreadyReplaced = 1,

    /// <summary>Сессия просрочена или отозвана выходом из аккаунта.</summary>
    NotUsable = 2
}

/// <summary>Итог попытки погасить сессию.</summary>
/// <param name="Outcome">Что случилось с сессией.</param>
/// <param name="UserId">Владелец сессии, если строка с таким хешем вообще есть.</param>
public sealed record RefreshTokenConsumptionResult(
    RefreshTokenConsumption Outcome,
    Guid UserId);
