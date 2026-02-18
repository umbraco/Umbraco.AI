namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Represents the result of a test operation.
/// </summary>
public sealed class TestOperationResult
{
    private TestOperationResult(AITest? test, string status)
    {
        Test = test;
        Status = status;
    }

    /// <summary>
    /// Gets the test entity if the operation succeeded.
    /// </summary>
    public AITest? Test { get; }

    /// <summary>
    /// Gets the operation status.
    /// </summary>
    public string Status { get; }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess => Status == "Success";

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TestOperationResult Success(AITest test) => new(test, "Success");

    /// <summary>
    /// Creates a failure result with the specified status.
    /// </summary>
    public static TestOperationResult Failed(string status) => new(null, status);

    /// <summary>
    /// Creates a not found result.
    /// </summary>
    public static TestOperationResult NotFound() => Failed("NotFound");

    /// <summary>
    /// Creates a duplicate alias result.
    /// </summary>
    public static TestOperationResult DuplicateAlias() => Failed("DuplicateAlias");

    /// <summary>
    /// Creates an invalid test type result.
    /// </summary>
    public static TestOperationResult InvalidTestType() => Failed("InvalidTestType");

    /// <summary>
    /// Creates an invalid target result.
    /// </summary>
    public static TestOperationResult InvalidTarget() => Failed("InvalidTarget");

    /// <summary>
    /// Creates an invalid run count result.
    /// </summary>
    public static TestOperationResult InvalidRunCount() => Failed("InvalidRunCount");

    /// <summary>
    /// Creates an invalid test case result.
    /// </summary>
    public static TestOperationResult InvalidTestCase() => Failed("InvalidTestCase");

    /// <summary>
    /// Creates a cancelled result.
    /// </summary>
    public static TestOperationResult Cancelled() => Failed("Cancelled");
}
