namespace SalesAR.UnitTests;

using Xunit;
using SalesAR.CoreBusiness.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_GeneratesNonEmptyHashedStringWithSalt()
    {
        string hash = _hasher.HashPassword("Admin@123");
        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.Contains(":", hash);
        Assert.NotEqual("Admin@123", hash);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        string hash = _hasher.HashPassword("SecretPassword123");
        bool isValid = _hasher.VerifyPassword("SecretPassword123", hash);
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ReturnsFalse()
    {
        string hash = _hasher.HashPassword("SecretPassword123");
        bool isValid = _hasher.VerifyPassword("WrongPassword", hash);
        Assert.False(isValid);
    }

    [Fact]
    public void TwoHashesOfSamePassword_HaveDifferentSalts()
    {
        string hash1 = _hasher.HashPassword("SamePassword");
        string hash2 = _hasher.HashPassword("SamePassword");

        Assert.NotEqual(hash1, hash2); // Muối ngẫu nhiên khác nhau
        Assert.True(_hasher.VerifyPassword("SamePassword", hash1));
        Assert.True(_hasher.VerifyPassword("SamePassword", hash2));
    }

    [Fact]
    public void GenerateSeedHashes_ForDocumentation()
    {
        string adminHash = _hasher.HashPassword("Admin@123");
        string salesHash = _hasher.HashPassword("Sales@123");
        string accHash = _hasher.HashPassword("Acc@123");

        System.IO.File.WriteAllText("seed_hashes.txt", $"ADMIN={adminHash}\nSALES={salesHash}\nACCOUNTANT={accHash}");
        Assert.True(_hasher.VerifyPassword("Admin@123", adminHash));
        Assert.True(_hasher.VerifyPassword("Sales@123", salesHash));
        Assert.True(_hasher.VerifyPassword("Acc@123", accHash));
    }
}
