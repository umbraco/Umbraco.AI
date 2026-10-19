using Umbraco.AI.Core.Contexts;

namespace Umbraco.AI.Deploy.Artifacts;

/// <summary>
/// The shape of one context resource inside <see cref="AIContextArtifact.Resources"/>.
/// Mirrors <see cref="AIContextResource"/> as exported, but keeps the <c>Id</c>, which System.Text.Json
/// cannot set on the entity itself.
/// </summary>
internal sealed class AIContextResourceArtifactItem
{
    public Guid Id { get; init; }

    public required string ResourceTypeId { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public int SortOrder { get; init; }

    public object? Settings { get; init; }

    public AIContextResourceInjectionMode InjectionMode { get; init; } = AIContextResourceInjectionMode.Always;

    /// <summary>
    /// Creates the resource with the artifact's Id. An empty Id (older artifacts) is generated on save.
    /// </summary>
    public AIContextResource ToResource()
        => new(Id)
        {
            ResourceTypeId = ResourceTypeId,
            Name = Name,
            Description = Description,
            SortOrder = SortOrder,
            Settings = Settings,
            InjectionMode = InjectionMode,
        };
}
