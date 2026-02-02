using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Umbraco.AI.Core.Security;

/// <summary>
/// Implements field protection using ASP.NET Core Data Protection API.
/// Uses a marker-based approach with <c>ENC:</c> prefix for encrypted values.
/// </summary>
internal sealed class AISensitiveFieldProtector : IAiSensitiveFieldProtector
{
    private const string EncryptedPrefix = "ENC:";
    private const string Purpose = "Umbraco.Ai.SensitiveFields.v1";

    private readonly IDataProtector _protector;
    private readonly ILogger<AISensitiveFieldProtector> _logger;

    public AISensitiveFieldProtector(
        IDataProtectionProvider dataProtectionProvider,
        ILogger<AISensitiveFieldProtector> logger)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);
        _logger = logger;
    }

    /// <inheritdoc />
    public string? Protect(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Already encrypted - return as-is
        if (IsProtected(value))
        {
            return value;
        }

        try
        {
            var encrypted = _protector.Protect(value);
            return $"{EncryptedPrefix}{encrypted}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt sensitive field value");
            throw;
        }
    }

    /// <inheritdoc />
    public string? Unprotect(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        // Not encrypted - return as-is (graceful handling of plain text values)
        if (!IsProtected(value))
        {
            return value;
        }

        try
        {
            var encryptedData = value[EncryptedPrefix.Length..];
            return _protector.Unprotect(encryptedData);
        }
        catch (CryptographicException ex)
        {
            // Graceful degradation - return as-is if decryption fails
            // This can happen if keys have rotated or data is corrupted
            _logger.LogWarning(ex, "Failed to decrypt sensitive field value - returning as-is");
            return value;
        }
    }

    /// <inheritdoc />
    public bool IsProtected(string? value)
        => !string.IsNullOrEmpty(value) && value.StartsWith(EncryptedPrefix, StringComparison.Ordinal);
}
