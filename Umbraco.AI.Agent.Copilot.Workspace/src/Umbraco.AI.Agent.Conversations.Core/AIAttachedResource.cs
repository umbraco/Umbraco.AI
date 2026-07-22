using Umbraco.AI.Core.Contexts;

namespace Umbraco.AI.Agent.Conversations.Core;

/// <summary>
/// A resource directly attached to a context owner — a project or a single conversation (the "attach a
/// direct resource" mechanism). Parallel to the core <see cref="AIContextResource"/> — a distinct type
/// because <c>AIContextResource</c> is constructed only inside <c>Umbraco.AI</c>'s own assemblies (its
/// <c>Id</c> setter is internal). It reuses the public resource-type machinery
/// (<c>AIContextResourceTypeCollection</c>, the editable-model serializer, and
/// <c>[AIContextResourceType]</c> registrations) for schema and settings serialization, and converts to
/// <see cref="AIResolvedResource"/> at resolve time.
/// </summary>
public sealed class AIAttachedResource
{
    /// <summary>The unique identifier of the resource.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The immutable identifier of the resource type (e.g., "content", "media"). Links to
    /// the registered <c>IAIContextResourceType.Id</c>.
    /// </summary>
    public required string ResourceTypeId { get; set; }

    /// <summary>The display name of the resource.</summary>
    public string? Name { get; set; }

    /// <summary>Optional description shown in the UI and to the LLM for OnDemand resources.</summary>
    public string? Description { get; set; }

    /// <summary>Ordering within the owner's resource list.</summary>
    public int SortOrder { get; set; }

    /// <summary>Type-specific settings object configured by the user.</summary>
    public object? Settings { get; set; }

    /// <summary>Determines how and when this resource is included in AI operations.</summary>
    public AIContextResourceInjectionMode InjectionMode { get; set; } = AIContextResourceInjectionMode.Always;
}
