namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Test input configuration - structure depends on the test feature being used.
/// Stored as JSON and deserialized by the test feature at execution time.
/// </summary>
/// <remarks>
/// The TestCaseJson structure is defined by the IAITestFeature.TestCaseType.
/// For example:
/// - PromptTestFeature expects PromptTestTestCase
/// - AgentTestFeature expects AgentTestTestCase
/// </remarks>
public sealed class AITestCase
{
    /// <summary>
    /// The test case configuration as JSON.
    /// Structure depends on the test feature's TestCaseType.
    /// </summary>
    public required string TestCaseJson { get; set; }
}
