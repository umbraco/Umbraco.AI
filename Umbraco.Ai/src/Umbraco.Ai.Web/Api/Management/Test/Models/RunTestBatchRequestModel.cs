using System.ComponentModel.DataAnnotations;

namespace Umbraco.Ai.Web.Api.Management.Test.Models;

/// <summary>
/// Request model for running multiple tests in a batch.
/// </summary>
public class RunTestBatchRequestModel
{
    /// <summary>
    /// The test IDs to run.
    /// </summary>
    [Required]
    public required IReadOnlyList<Guid> TestIds { get; init; }

    /// <summary>
    /// Optional profile ID override for all tests.
    /// When provided, overrides the target's default profile.
    /// </summary>
    public Guid? ProfileIdOverride { get; init; }

    /// <summary>
    /// Optional context IDs override for all tests.
    /// When provided, overrides the target's default contexts.
    /// </summary>
    public IReadOnlyList<Guid>? ContextIdsOverride { get; init; }
}
