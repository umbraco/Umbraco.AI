using System.ComponentModel.DataAnnotations;

namespace Umbraco.AI.Web.Api.Management.Test.Models;

/// <summary>
/// Response model for a test list item (minimal info).
/// </summary>
public class TestItemResponseModel
{
    /// <summary>
    /// The unique identifier of the test.
    /// </summary>
    [Required]
    public Guid Id { get; set; }

    /// <summary>
    /// The alias of the test.
    /// </summary>
    [Required]
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the test.
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this test validates.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The test feature ID (test feature implementation).
    /// </summary>
    [Required]
    public string TestFeatureId { get; set; } = string.Empty;

    /// <summary>
    /// Tags for organizing tests.
    /// </summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>
    /// Number of times to run this test for pass@k calculation.
    /// </summary>
    public int RunCount { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the test was created.
    /// </summary>
    public DateTime DateCreated { get; set; }

    /// <summary>
    /// The date and time (in UTC) when the test was last modified.
    /// </summary>
    public DateTime DateModified { get; set; }

    /// <summary>
    /// The current version number of the entity.
    /// </summary>
    public int Version { get; set; }
}
