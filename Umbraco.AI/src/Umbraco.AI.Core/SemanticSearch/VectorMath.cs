using System.Runtime.InteropServices;

namespace Umbraco.AI.Core.SemanticSearch;

/// <summary>
/// Provides vector math operations for embedding similarity computation.
/// </summary>
internal static class VectorMath
{
    /// <summary>
    /// Computes the cosine similarity between two vectors.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>A value between -1.0 and 1.0, where 1.0 indicates identical vectors.</returns>
    public static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        if (a.Length != b.Length || a.Length == 0)
        {
            return 0f;
        }

        float dot = 0f, magA = 0f, magB = 0f;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }

        var denominator = MathF.Sqrt(magA) * MathF.Sqrt(magB);
        return denominator == 0f ? 0f : dot / denominator;
    }

    /// <summary>
    /// Serializes a float array to a byte array using zero-copy memory marshalling.
    /// </summary>
    public static byte[] SerializeVector(ReadOnlySpan<float> vector)
    {
        return MemoryMarshal.AsBytes(vector).ToArray();
    }

    /// <summary>
    /// Deserializes a byte array back to a float array using zero-copy memory marshalling.
    /// </summary>
    public static float[] DeserializeVector(ReadOnlySpan<byte> bytes)
    {
        return MemoryMarshal.Cast<byte, float>(bytes).ToArray();
    }
}
