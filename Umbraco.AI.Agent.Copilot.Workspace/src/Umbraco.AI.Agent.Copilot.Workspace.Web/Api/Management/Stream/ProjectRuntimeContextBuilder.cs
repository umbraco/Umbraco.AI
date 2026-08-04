using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.Resolvers;
using Umbraco.AI.Core.Contexts.ResourceTypes.BuiltIn;
using CoreConstants = Umbraco.AI.Core.Constants;

namespace Umbraco.AI.Agent.Copilot.Workspace.Web.Api.Management.Stream;

/// <summary>
/// Maps a Copilot Workspace <see cref="AIProject"/> into the runtime-context properties that inject
/// the project's grounding into a chat run. A project contributes:
/// <list type="bullet">
///   <item>a brief <em>framing</em> line (project name + description),</item>
///   <item>its custom <em>instructions</em> (augmenting, not replacing, the agent's own),</item>
///   <item>its directly-attached <em>resources</em>, and</item>
///   <item>its referenced <c>AIContext</c> ids.</item>
/// </list>
/// Framing and instructions are synthesized as <see cref="AIContextResourceInjectionMode.Always"/>
/// <c>text</c> resources so they ride the same resolve→format→inject pipeline as everything else
/// (via <see cref="CoreConstants.ContextKeys.AdditionalResources"/>) — no bespoke system-prompt path.
/// Per-resource <see cref="AIContextResourceInjectionMode"/> remains the single source of truth for
/// always-in-prompt vs. tool-fetched-on-demand.
/// </summary>
internal static class ProjectRuntimeContextBuilder
{
    /// <summary>The <c>text</c> resource type id (see <c>TextResourceType</c>'s attribute).</summary>
    private const string TextResourceTypeId = "text";

    /// <summary>
    /// Stable, well-known ids for the synthesized framing/instructions resources — fixed so they are
    /// deterministic across runs and can never collide with a user-authored resource id.
    /// </summary>
    private static readonly Guid FramingResourceId = Guid.Parse("d0c0ffee-0000-4000-8000-000000000001");
    private static readonly Guid InstructionsResourceId = Guid.Parse("d0c0ffee-0000-4000-8000-000000000002");

    /// <summary>
    /// Builds the runtime-context properties for <paramref name="project"/>, or <c>null</c> when the
    /// project contributes nothing to inject.
    /// </summary>
    public static IReadOnlyDictionary<string, object?>? Build(AIProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        var properties = new Dictionary<string, object?>();

        if (project.ContextIds.Count > 0)
        {
            // Honoured by ProfileContextResolver (the "attach a context" mechanism).
            properties[CoreConstants.ContextKeys.AdditionalContextIds] = project.ContextIds.ToList();
        }

        // Order matters: framing first, then instructions, then the project's own resources.
        var resources = new List<AIContextResolverResource>();

        var framing = BuildFramingResource(project);
        if (framing is not null)
        {
            resources.Add(framing);
        }

        if (!string.IsNullOrWhiteSpace(project.Instructions))
        {
            resources.Add(BuildInstructionsResource(project));
        }

        // The project's directly-attached resources, in author order.
        resources.AddRange(project.Resources
            .OrderBy(r => r.SortOrder)
            .Select(r => new AIContextResolverResource
            {
                Id = r.Id,
                ResourceTypeId = r.ResourceTypeId,
                Name = r.Name ?? string.Empty,
                Description = r.Description,
                Settings = r.Settings,
                InjectionMode = r.InjectionMode,
                ContextName = project.Name,
            }));

        if (resources.Count > 0)
        {
            // Honoured by AdditionalResourcesContextResolver (the "attach a resource" mechanism).
            properties[CoreConstants.ContextKeys.AdditionalResources] = resources;
        }

        return properties.Count > 0 ? properties : null;
    }

    /// <summary>
    /// A short line telling the model which project it is working in. Returns <c>null</c> when the
    /// project has no usable name (nothing meaningful to frame).
    /// </summary>
    private static AIContextResolverResource? BuildFramingResource(AIProject project)
    {
        if (string.IsNullOrWhiteSpace(project.Name))
        {
            return null;
        }

        var content = $"You are working within a project called \"{project.Name.Trim()}\".";
        if (!string.IsNullOrWhiteSpace(project.Description))
        {
            content += $" {project.Description.Trim()}";
        }

        return new AIContextResolverResource
        {
            Id = FramingResourceId,
            ResourceTypeId = TextResourceTypeId,
            Name = project.Name.Trim(),
            Description = "The project this conversation belongs to.",
            Settings = new TextResourceSettings { Content = content },
            InjectionMode = AIContextResourceInjectionMode.Always,
            ContextName = project.Name,
        };
    }

    /// <summary>
    /// The project's custom instructions, injected as an always-on section that augments (does not
    /// replace) the agent's own instructions.
    /// </summary>
    private static AIContextResolverResource BuildInstructionsResource(AIProject project)
        => new()
        {
            Id = InstructionsResourceId,
            ResourceTypeId = TextResourceTypeId,
            Name = "Project instructions",
            Description = "Custom instructions that apply to every conversation in this project.",
            Settings = new TextResourceSettings { Content = project.Instructions!.Trim() },
            InjectionMode = AIContextResourceInjectionMode.Always,
            ContextName = project.Name,
        };
}
