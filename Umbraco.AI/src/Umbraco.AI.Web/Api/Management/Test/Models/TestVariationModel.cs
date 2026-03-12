using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Model for a test variation configuration override.
/// </summary>
public class TestVariationModel
{
    /// <summary>
    /// Unique identifier for the variation.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Display name of the variation.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this variation tests.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional profile ID override. Null means inherit from test.
    /// </summary>
    public Guid? ProfileId { get; set; }

    /// <summary>
    /// Optional run count override. Null means inherit from test.
    /// </summary>
    public int? RunCount { get; set; }

    /// <summary>
    /// Optional context IDs override. Null means inherit from test.
    /// </summary>
    public IReadOnlyList<Guid>? ContextIds { get; set; }

    /// <summary>
    /// Optional feature config overrides (deep-merged with test's config).
    /// Null means inherit the test's TestFeatureConfig as-is.
    /// </summary>
    public JsonElement? TestFeatureConfig { get; set; }
}
