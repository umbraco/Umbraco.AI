using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Embedding.Models;

/// <summary>
/// Request model for generating embeddings.
/// </summary>
public class GenerateEmbeddingRequestModel
{
    /// <summary>
    /// The profile ID to use for embedding generation.
    /// If not specified, the default embedding profile will be used.
    /// </summary>
    public Guid? ProfileId { get; init; }

    /// <summary>
    /// The text values to generate embeddings for.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<string> Values { get; init; }
}
