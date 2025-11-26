using System.ComponentModel.DataAnnotations;
using Umbraco.Ai.Web.Api.Management.Common.Models;

namespace Umbraco.Ai.Web.Api.Management.Profile.Models;

/// <summary>
/// Request model for updating an existing profile.
/// </summary>
public class UpdateProfileRequestModel
{
    /// <summary>
    /// The display name of the profile.
    /// </summary>
    [Required]
    public required string Name { get; init; }

    /// <summary>
    /// The model reference for this profile.
    /// </summary>
    [Required]
    public required ModelRefModel Model { get; init; }

    /// <summary>
    /// The ID of the connection to use for this profile.
    /// </summary>
    [Required]
    public required Guid ConnectionId { get; init; }

    /// <summary>
    /// The temperature setting for the AI model.
    /// </summary>
    public float? Temperature { get; init; }

    /// <summary>
    /// The maximum number of tokens the AI model can generate.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// The system prompt template to be used with the AI model.
    /// </summary>
    public string? SystemPromptTemplate { get; init; }

    /// <summary>
    /// Tags associated with the profile.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
