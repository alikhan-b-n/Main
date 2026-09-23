using Lama.Infrastructure.Security;

namespace Lama.Tests.AccessControl;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_AcceptsTheRightPasswordOnly()
    {
        var hash = _hasher.Hash("correct horse battery staple");

        Assert.True(_hasher.Verify("correct horse battery staple", hash));
        Assert.False(_hasher.Verify("Correct horse battery staple", hash));
        Assert.False(_hasher.Verify("", hash));
    }

    [Fact]
    public void Hash_NeverStoresThePasswordAndIsSaltedPerCall()
    {
        const string password = "Super secret 123";

        var first = _hasher.Hash(password);
        var second = _hasher.Hash(password);

        Assert.DoesNotContain(password, first);
        Assert.NotEqual(first, second); // random salt
        Assert.True(_hasher.Verify(password, first));
        Assert.True(_hasher.Verify(password, second));
        Assert.StartsWith("pbkdf2-sha256.210000.", first);
    }

    [Theory]
    [InlineData("")]
    [InlineData("plain-text-password")]
    [InlineData("pbkdf2-sha256.notanumber.c2FsdA==.aGFzaA==")]
    [InlineData("pbkdf2-sha256.210000.!!!notbase64!!!.aGFzaA==")]
    [InlineData("md5.210000.c2FsdA==.aGFzaA==")]
    public void Verify_RejectsBrokenHashesInsteadOfThrowing(string hash)
    {
        Assert.False(_hasher.Verify("whatever", hash));
    }
}
