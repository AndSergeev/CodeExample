using System.Reflection;

namespace CodeExample.Shared.Abstractions;

public static class ResultFactory
{
    private static readonly MethodInfo GenericFailure = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method =>
            method.Name == nameof(Result.Failure)
            && method.IsGenericMethodDefinition
            && method.GetParameters() is [{ ParameterType: var parameterType }]
            && parameterType == typeof(Error));

    public static TResponse CreateFailure<TResponse>(Error error)
        where TResponse : Result
    {
        ArgumentNullException.ThrowIfNull(error);

        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        var valueType = typeof(TResponse).GetGenericArguments()[0];
        var constructed = GenericFailure.MakeGenericMethod(valueType);

        return (TResponse)constructed.Invoke(null, new object[] { error })!;
    }
}
