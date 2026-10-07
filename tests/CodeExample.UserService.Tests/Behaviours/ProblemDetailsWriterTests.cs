using FluentAssertions;
using CodeExample.Shared.Abstractions;
using CodeExample.Shared.Web;
using Xunit;

namespace CodeExample.UserService.Tests.Behaviours;

/// <summary>Тесты перевода типа ошибки в HTTP-статус.</summary>
public sealed class ProblemDetailsWriterTests
{
    [Theory]
    [InlineData(ErrorType.Validation, 400)]
    [InlineData(ErrorType.Unauthorized, 401)]
    [InlineData(ErrorType.NotFound, 404)]
    [InlineData(ErrorType.Conflict, 409)]
    [InlineData(ErrorType.Unavailable, 503)]
    [InlineData(ErrorType.Failure, 500)]
    public void ToStatusCode_MapsEveryMemberOfTheEnum(ErrorType errorType, int expected)
    {
        ProblemDetailsWriter.ToStatusCode(errorType).Should().Be(expected);
    }

    [Fact]
    public void ToStatusCode_CoversEveryDeclaredMember()
    {
        // Член перечисления без ветки молча уходил бы в 500, поэтому проверяем полноту.
        foreach (var errorType in Enum.GetValues<ErrorType>())
        {
            var mapped = ProblemDetailsWriter.ToStatusCode(errorType);

            if (errorType == ErrorType.Failure)
            {
                mapped.Should().Be(500);
                continue;
            }

            mapped.Should().NotBe(500, $"тип {errorType} обязан иметь собственный статус");
        }
    }
}
