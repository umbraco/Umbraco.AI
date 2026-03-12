namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Request model for running a test.
/// </summary>
public class RunTestRequestModel
{
    /// <summary>
    /// Optional profile ID to override for this test run.
    /// Allows cross-model comparison.
    /// </summary>
    public Guid? ProfileIdOverride { get; set; }

    /// <summary>
    /// Optional context IDs to override for this test run.
    /// Allows cross-context comparison.
    /// </summary>
    public IEnumerable<Guid>? ContextIdsOverride { get; set; }
}
