namespace Umbraco.Ai.Core.Security;

/// <summary>
/// Provides encryption and decryption services for sensitive field values.
/// </summary>
public interface IAiSensitiveFieldProtector
{
    /// <summary>
    /// Encrypts a sensitive value and returns it with the encrypted prefix.
    /// </summary>
    /// <param name="value">The plaintext value to encrypt.</param>
    /// <returns>The encrypted value prefixed with <c>ENC:</c>, or the original value if null/empty.</returns>
    string? Protect(string? value);

    /// <summary>
    /// Decrypts a protected value, removing the encrypted prefix.
    /// </summary>
    /// <param name="value">The encrypted value (with <c>ENC:</c> prefix) to decrypt.</param>
    /// <returns>The decrypted plaintext value, or the original value if not encrypted.</returns>
    string? Unprotect(string? value);

    /// <summary>
    /// Checks if a value is encrypted (has the <c>ENC:</c> prefix).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns><c>true</c> if the value is encrypted; otherwise, <c>false</c>.</returns>
    bool IsProtected(string? value);
}
