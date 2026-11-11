using Ludex.WebApi.Features.Users.Services;

namespace Ludex.UnitTest.Users;

/// <summary>
/// Password hashes: salted, checked in constant time, and never accepting a malformed hash.
/// </summary>
public sealed class PasswordHasherTests
{
    [Fact]
    public void TheRightPasswordMatchesAndAWrongOneDoesnt()
    {
        var hash = PasswordHasher.Hash("ludex-2026");

        Assert.True(PasswordHasher.Verify("ludex-2026", hash));
        Assert.False(PasswordHasher.Verify("ludex-2027", hash));
    }

    [Fact]
    public void TheSamePasswordHashesDifferentlyEachTime()
    {
        Assert.NotEqual(PasswordHasher.Hash("same"), PasswordHasher.Hash("same"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain")]
    [InlineData("pbkdf2-sha256.x.abc.def")]
    [InlineData("pbkdf2-sha256.1000.%%%.###")]
    public void AMalformedHashNeverMatches(string stored)
    {
        Assert.False(PasswordHasher.Verify("anything", stored));
    }
}
