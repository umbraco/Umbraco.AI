namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for batch test execution results.
/// Contains execution results for each test that was executed.
/// </summary>
public class TestBatchResultsResponseModel
{
    /// <summary>
    /// Dictionary mapping test IDs to their execution results.
    /// Tests that failed to execute (not found, etc.) are excluded from results.
    /// </summary>
    public Dictionary<Guid, TestExecutionResultResponseModel> Results { get; set; } = new();
}
