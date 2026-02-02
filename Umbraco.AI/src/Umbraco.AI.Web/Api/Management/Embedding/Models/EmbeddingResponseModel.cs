using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Embedding.Models;

/// <summary>
/// Response model for generated embeddings.
/// </summary>
public class EmbeddingResponseModel
{
    /// <summary>
    /// The generated embeddings, one per input value.
    /// </summary>
    [Required]
    public IReadOnlyList<EmbeddingItemModel> Embeddings { get; set; } = [];
}

/// <summary>
/// Represents a single embedding result.
/// </summary>
public class EmbeddingItemModel
{
    /// <summary>
    /// The index of the input value this embedding corresponds to.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// The embedding vector as an array of floats.
    /// </summary>
    [Required]
    public float[] Vector { get; set; } = [];
}
