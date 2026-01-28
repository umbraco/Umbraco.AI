using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Web.Api.Common.Models;

namespace Umbraco.Ai.Web.Api.Management.Embedding.Models;

/// <summary>
/// Request model for generating embeddings.
/// </summary>
public class GenerateEmbeddingRequestModel
{
    /// <summary>
    /// The profile to use for embedding generation, specified by ID or alias.
    /// If not specified, the default embedding profile will be used.
    /// </summary>
    public IdOrAlias? ProfileIdOrAlias { get; init; }

    /// <summary>
    /// The text values to generate embeddings for.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<string> Values { get; init; }
}
