using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Umbraco.AI.Core.Security;

namespace Umbraco.AI.Tests.Unit.Security;

public class AISensitiveFieldProtectorTests
{
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly Mock<ILogger<AISensitiveFieldProtector>> _loggerMock;
    private readonly AISensitiveFieldProtector _protector;

    public AISensitiveFieldProtectorTests()
    {
        // Use ephemeral data protection for tests (keys not persisted)
        _dataProtectionProvider = DataProtectionProvider.Create("TestApp");
        _loggerMock = new Mock<ILogger<AISensitiveFieldProtector>>();
        _protector = new AISensitiveFieldProtector(_dataProtectionProvider, _loggerMock.Object);
    }

    #region Protect

    [Fact]
    public void Protect_WithValidValue_ReturnsEncryptedValueWithPrefix()
    {
        // Arrange
        var plainText = "my-secret-api-key";

        // Act
        var result = _protector.Protect(plainText);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldStartWith("ENC:");
        result.ShouldNotBe($"ENC:{plainText}"); // Should be encrypted, not just prefixed
    }

    [Fact]
    public void Protect_WithNullValue_ReturnsNull()
    {
        // Act
        var result = _protector.Protect(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Protect_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = _protector.Protect(string.Empty);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Protect_WithAlreadyEncryptedValue_ReturnsValueAsIs()
    {
        // Arrange
        var plainText = "my-secret-api-key";
        var encrypted = _protector.Protect(plainText);

        // Act
        var result = _protector.Protect(encrypted);

        // Assert - Should not double-encrypt
        result.ShouldBe(encrypted);
    }

    #endregion

    #region Unprotect

    [Fact]
    public void Unprotect_WithEncryptedValue_ReturnsDecryptedValue()
    {
        // Arrange
        var plainText = "my-secret-api-key";
        var encrypted = _protector.Protect(plainText);

        // Act
        var result = _protector.Unprotect(encrypted);

        // Assert
        result.ShouldBe(plainText);
    }

    [Fact]
    public void Unprotect_WithNullValue_ReturnsNull()
    {
        // Act
        var result = _protector.Unprotect(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Unprotect_WithEmptyString_ReturnsEmptyString()
    {
        // Act
        var result = _protector.Unprotect(string.Empty);

        // Assert
        result.ShouldBe(string.Empty);
    }

    [Fact]
    public void Unprotect_WithNonEncryptedValue_ReturnsValueAsIs()
    {
        // Arrange
        var plainText = "plain-text-api-key";

        // Act
        var result = _protector.Unprotect(plainText);

        // Assert - Graceful handling of unencrypted values
        result.ShouldBe(plainText);
    }

    [Fact]
    public void Unprotect_WithCorruptedEncryptedValue_ReturnsValueAsIs()
    {
        // Arrange - Value with ENC: prefix but invalid encrypted data
        var corrupted = "ENC:not-valid-base64-encrypted-data!!!";

        // Act
        var result = _protector.Unprotect(corrupted);

        // Assert - Graceful degradation, returns as-is with warning logged
        result.ShouldBe(corrupted);
    }

    #endregion

    #region IsProtected

    [Fact]
    public void IsProtected_WithEncryptedValue_ReturnsTrue()
    {
        // Arrange
        var plainText = "my-secret-api-key";
        var encrypted = _protector.Protect(plainText);

        // Act
        var result = _protector.IsProtected(encrypted);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsProtected_WithNonEncryptedValue_ReturnsFalse()
    {
        // Arrange
        var plainText = "plain-text-api-key";

        // Act
        var result = _protector.IsProtected(plainText);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsProtected_WithNullValue_ReturnsFalse()
    {
        // Act
        var result = _protector.IsProtected(null);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsProtected_WithEmptyString_ReturnsFalse()
    {
        // Act
        var result = _protector.IsProtected(string.Empty);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsProtected_WithValueStartingWithEnc_ReturnsTrue()
    {
        // Arrange - Even if content is invalid, prefix determines protection status
        var prefixed = "ENC:anything";

        // Act
        var result = _protector.IsProtected(prefixed);

        // Assert
        result.ShouldBeTrue();
    }

    #endregion

    #region Round Trip

    [Fact]
    public void ProtectAndUnprotect_RoundTrip_PreservesOriginalValue()
    {
        // Arrange
        var originalValues = new[]
        {
            "simple-key",
            "sk-proj-abc123",
            "key with spaces and special chars !@#$%^&*()",
            "very-long-key-" + new string('x', 1000),
            "unicode: 你好世界 🔑"
        };

        foreach (var original in originalValues)
        {
            // Act
            var encrypted = _protector.Protect(original);
            var decrypted = _protector.Unprotect(encrypted);

            // Assert
            decrypted.ShouldBe(original, $"Failed for value: {original}");
        }
    }

    #endregion
}
