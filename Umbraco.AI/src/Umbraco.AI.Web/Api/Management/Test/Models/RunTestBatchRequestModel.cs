using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Request model for executing multiple tests in batch.
/// </summary>
public class RunTestBatchRequestModel
{
    /// <summary>
    /// The test IDs to execute.
    /// </summary>
    [Required]
    public IEnumerable<Guid> TestIds { get; set; } = [];

    /// <summary>
    /// Optional profile ID to override for all tests in the batch.
    /// Allows cross-model comparison.
    /// </summary>
    public Guid? ProfileIdOverride { get; set; }

    /// <summary>
    /// Optional context IDs to override for all tests in the batch.
    /// Allows cross-context comparison.
    /// </summary>
    public IEnumerable<Guid>? ContextIdsOverride { get; set; }
}
