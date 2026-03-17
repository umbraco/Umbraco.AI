using System.Security.Cryptography;
using System.Text;

namespace Umbraco.AI.Core.Utilities;

/// <summary>
/// Creates deterministic GUIDs from a namespace and name using UUID v5 (SHA-1).
/// Same inputs always produce the same GUID.
/// </summary>
public static class DeterministicGuid
{
    /// <summary>
    /// Creates a deterministic GUID from a namespace and name using UUID v5 (SHA-1 based).
    /// The same namespace and name always produce the same GUID.
    /// </summary>
    /// <param name="namespaceId">The namespace GUID (acts as a seed for the hash).</param>
    /// <param name="name">The name to generate the GUID from.</param>
    /// <returns>A deterministic GUID derived from the namespace and name.</returns>
    public static Guid Create(Guid namespaceId, string name)
    {
        byte[] namespaceBytes = namespaceId.ToByteArray();
        byte[] nameBytes = Encoding.UTF8.GetBytes(name);

        byte[] combined = new byte[namespaceBytes.Length + nameBytes.Length];
        Buffer.BlockCopy(namespaceBytes, 0, combined, 0, namespaceBytes.Length);
        Buffer.BlockCopy(nameBytes, 0, combined, namespaceBytes.Length, nameBytes.Length);

        byte[] hash = SHA1.HashData(combined);

        // Set version to 5 (name-based SHA-1)
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        // Set variant to RFC 4122
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);

        byte[] guidBytes = new byte[16];
        Array.Copy(hash, 0, guidBytes, 0, 16);

        return new Guid(guidBytes);
    }
}
