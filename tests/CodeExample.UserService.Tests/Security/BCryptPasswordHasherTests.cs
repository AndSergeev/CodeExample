using FluentAssertions;
using CodeExample.UserService.Infrastructure.Security;
using Xunit;

namespace CodeExample.UserService.Tests.Security;

public sealed class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesAValueThatIsNotThePlainPassword()
    {
        var hash = _hasher.Hash("supersecret");

        hash.Should().NotBe("supersecret");
        hash.Should().NotContain("supersecret");
    }

    [Fact]
    public void Hash_ProducesADifferentValueEveryTime()
    {
        var first = _hasher.Hash("supersecret");
        var second = _hasher.Hash("supersecret");

        first.Should().NotBe(second);
    }

    [Fact]
    public void Verify_AcceptsThePasswordThatWasHashed()
    {
        var hash = _hasher.Hash("supersecret");

        _hasher.Verify("supersecret", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_RejectsAnotherPassword()
    {
        var hash = _hasher.Hash("supersecret");

        _hasher.Verify("supersecre", hash).Should().BeFalse();
        _hasher.Verify("Supersecret", hash).Should().BeFalse();
        _hasher.Verify("", hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_AcceptsAHashProducedByAnotherInstance()
    {
        var hash = new BCryptPasswordHasher().Hash("supersecret");

        _hasher.Verify("supersecret", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_RejectsAValueThatIsNotAHash()
    {
        _hasher.Verify("supersecret", "not-a-hash").Should().BeFalse();
        _hasher.Verify("supersecret", "").Should().BeFalse();
    }

    [Fact]
    public void Hash_RejectsAnEmptyPassword()
    {
        var act = () => _hasher.Hash("   ");

        act.Should().Throw<ArgumentException>();
    }
}
