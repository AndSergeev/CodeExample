using FluentAssertions;
using CodeExample.UserService.Application.Users.LoginUser;
using CodeExample.UserService.Application.Users.RegisterUser;
using CodeExample.UserService.Domain.Entities;
using Xunit;

namespace CodeExample.UserService.Tests.Users;

/// <summary>
/// Тесты входных валидаторов.
/// </summary>
public sealed class UserValidatorTests
{
    [Theory]
    [InlineData("Alice", "supersecret", true)]
    [InlineData("ab", "supersecret", false)]
    [InlineData("", "supersecret", false)]
    [InlineData("Alice", "short", false)]
    [InlineData("Alice", "", false)]
    public void RegisterValidator_EnforcesNameAndPasswordRules(string name, string password, bool expected)
    {
        var validator = new RegisterUserCommandValidator();

        var result = validator.Validate(new RegisterUserCommand(name, password));

        result.IsValid.Should().Be(expected);
    }

    [Fact]
    public void RegisterValidator_AcceptsANameAtTheMaximumLength()
    {
        var name = new string('a', User.NameMaxLength);
        var validator = new RegisterUserCommandValidator();

        var result = validator.Validate(new RegisterUserCommand(name, "supersecret"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void RegisterValidator_RejectsANameBeyondTheMaximumLength()
    {
        var name = new string('a', User.NameMaxLength + 1);
        var validator = new RegisterUserCommandValidator();

        var result = validator.Validate(new RegisterUserCommand(name, "supersecret"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterValidator_RejectsAPasswordBeyondTheBcryptLimit()
    {
        var ascii = new string('a', RegisterUserCommandValidator.PasswordMaxBytes + 1);
        var validator = new RegisterUserCommandValidator();

        validator.Validate(new RegisterUserCommand("Alice", ascii)).IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterValidator_MeasuresTheBcryptLimitInBytesRatherThanCharacters()
    {
        // Тридцать семь кириллических букв - это 74 байта при 37 символах, то есть
        // предел по символам такой пароль пропустил бы, а bcrypt обрезал бы.
        var cyrillic = new string('я', 37);
        var fitsByCharacters = new string('я', 36);
        var validator = new RegisterUserCommandValidator();

        validator.Validate(new RegisterUserCommand("Alice", cyrillic)).IsValid.Should().BeFalse();
        validator.Validate(new RegisterUserCommand("Alice", fitsByCharacters)).IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginValidator_RequiresBothFields()
    {
        var validator = new LoginUserCommandValidator();

        validator.Validate(new LoginUserCommand("Alice", "whatever")).IsValid.Should().BeTrue();
        validator.Validate(new LoginUserCommand("", "whatever")).IsValid.Should().BeFalse();
        validator.Validate(new LoginUserCommand("Alice", "")).IsValid.Should().BeFalse();
    }
}
