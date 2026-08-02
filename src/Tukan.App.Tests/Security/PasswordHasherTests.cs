using Chomik.Core.Security;

namespace Tukan.App.Tests.Security;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_I_Verify_HappyPath_Pbkdf2()
    {
        var (hash, salt) = PasswordHasher.HashPassword("tajne");
        hash.Should().StartWith("pbkdf2$");
        PasswordHasher.Verify("tajne", hash, salt).Should().BeTrue();
        PasswordHasher.NeedsRehash(hash).Should().BeFalse();
    }

    [Fact]
    public void Verify_ZleHaslo_False()
    {
        var (hash, salt) = PasswordHasher.HashPassword("tajne");
        PasswordHasher.Verify("inne", hash, salt).Should().BeFalse();
    }

    [Fact]
    public void HashPassword_RozneSoli_DlaTegoSamegoHasla()
    {
        var a = PasswordHasher.HashPassword("1111");
        var b = PasswordHasher.HashPassword("1111");
        a.Salt.Should().NotBe(b.Salt);
        a.Hash.Should().NotBe(b.Hash);
    }

    [Fact]
    public void Verify_PusteHaslo_ZPustymHashNiePasujeDoNiepustego()
    {
        var (hash, salt) = PasswordHasher.HashPassword("x");
        PasswordHasher.Verify("", hash, salt).Should().BeFalse();
    }

    [Fact]
    public void Verify_LegacySha256Base64_NadalDziala()
    {
        // Base64(SHA256("secret" + "c29tZS1zYWx0")) — kompatybilność ze starymi kontami
        const string password = "secret";
        const string salt = "c29tZS1zYWx0";
        var legacyBytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(password + salt));
        var legacyHash = Convert.ToBase64String(legacyBytes);

        PasswordHasher.Verify(password, legacyHash, salt).Should().BeTrue();
        PasswordHasher.NeedsRehash(legacyHash).Should().BeTrue();
        PasswordHasher.Verify("wrong", legacyHash, salt).Should().BeFalse();
    }

    [Fact]
    public void Verify_PustyHash_ZwracaFalse()
    {
        PasswordHasher.Verify("x", "", "salt").Should().BeFalse();
        PasswordHasher.Verify("", "hash", "").Should().BeFalse();
    }
}
