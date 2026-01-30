namespace Umbraco.Ai.Core.Tests;

/// <summary>
/// Defines the test case configuration for a test.
/// The actual content is serialized JSON that gets deserialized by the test feature
/// based on its specific TestCaseType.
/// </summary>
public sealed class AiTestCase
{
    /// <summary>
    /// The test case configuration as JSON.
    /// Structure depends on the test feature's TestCaseType.
    /// </summary>
    public required string TestCaseJson { get; set; }
}
