using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Request model for executing tests filtered by tags.
/// </summary>
public class RunTestsByTagsRequestModel
{
    /// <summary>
    /// The tags to filter tests by.
    /// Tests must have ALL specified tags to be included.
    /// </summary>
    [Required]
    public IEnumerable<string> Tags { get; set; } = [];

    /// <summary>
    /// Optional profile ID to override for all matching tests.
    /// Allows cross-model comparison.
    /// </summary>
    public Guid? ProfileIdOverride { get; set; }

    /// <summary>
    /// Optional context IDs to override for all matching tests.
    /// Allows cross-context comparison.
    /// </summary>
    public IEnumerable<Guid>? ContextIdsOverride { get; set; }
}
