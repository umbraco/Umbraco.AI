using Umbraco.Abilities.Entities;

namespace Umbraco.Abilities.Samples;

/// <summary>
/// Stands in for each product's own permission model (CMS user groups, Commerce store access): which users may use
/// which entity types. Read, create and update aren't told apart, which is enough to show where the checks happen.
/// </summary>
public sealed class SampleAccess
{
    /// <summary>Can use everything.</summary>
    public static readonly Guid Editor = new("11111111-1111-1111-1111-111111111111");

    /// <summary>Can use content and media, but not Commerce or Automate.</summary>
    public static readonly Guid ContentEditor = new("22222222-2222-2222-2222-222222222222");

    /// <summary>Can build workflows, but has no Commerce section, so isn't offered Commerce's triggers and actions.</summary>
    public static readonly Guid AutomateEditor = new("33333333-3333-3333-3333-333333333333");

    private readonly Dictionary<Guid, HashSet<string>?> _users = new()
    {
        [Editor] = null,
        [ContentEditor] = ["document", "media"],
        [AutomateEditor] = ["document", "media", "umbraco-automate-workflow"],
    };

    // Backoffice sections, which plugin item types can require (an Automate action that needs the Commerce section).
    private readonly Dictionary<Guid, HashSet<string>?> _sections = new()
    {
        [Editor] = null,
        [ContentEditor] = ["content", "media"],
        [AutomateEditor] = ["content", "media", "automate"],
    };

    public bool HasSection(Guid userKey, string section)
        => _sections.TryGetValue(userKey, out var sections) && (sections is null || sections.Contains(section));

    public string? Authorize(string entityType, EntityAccessRequest request)
        => !_users.TryGetValue(request.UserKey, out var allowed) ? "Unknown user."
            : allowed is null || allowed.Contains(entityType) ? null
            : $"You don't have access to {entityType} entities.";
}
