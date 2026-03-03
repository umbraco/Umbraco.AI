using System.Text.Json;

namespace Umbraco.AI.Core.Tests;

/// <summary>
/// Defines a named configuration override for a test.
/// Variations allow running the same test with different profiles, contexts, run counts,
/// or feature config overrides to compare results across models or configurations.
/// </summary>
public sealed class AITestVariation
{
    /// <summary>
    /// Unique identifier for the variation within the test.
    /// </summary>
    public Guid Id { get; internal set; } = Guid.NewGuid();

    /// <summary>
    /// Display name for the variation (e.g., "GPT-4 Turbo", "Claude Sonnet").
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional description of what this variation tests.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional profile ID override. When null, inherits from the test's ProfileId.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Optional run count override. When null, inherits from the test's RunCount.
    /// </summary>
    public int? RunCount { get; set; }

    /// <summary>
    /// Optional context IDs override. When null, inherits from the test's ContextIds.
    /// </summary>
    public IReadOnlyList<Guid>? ContextIds { get; set; }

    /// <summary>
    /// Optional feature config overrides as JsonElement.
    /// When provided, deep-merged with the test's TestFeatureConfig
    /// (object properties are recursively merged; arrays and primitives are replaced).
    /// When null, inherits the test's TestFeatureConfig as-is.
    /// </summary>
    public JsonElement? TestFeatureConfig { get; set; }
}
