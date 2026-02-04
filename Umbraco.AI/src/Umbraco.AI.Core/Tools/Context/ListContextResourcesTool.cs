using Umbraco.AI.Core.Contexts;

namespace Umbraco.AI.Core.Tools.Context;

/// <summary>
/// Tool that lists available on-demand context resources.
/// </summary>
/// <remarks>
/// This tool is dynamically injected when OnDemand resources are available.
/// It lists resources that the LLM can retrieve using the GetContextResource tool.
/// </remarks>
[AITool("list_context_resources", "List Context Resources", ScopeId = "navigation")]
public class ListContextResourcesTool : AIToolBase, IAISystemTool
{
    private readonly IAIContextAccessor _contextAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="ListContextResourcesTool"/>.
    /// </summary>
    /// <param name="contextAccessor">The context accessor.</param>
    public ListContextResourcesTool(IAIContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    /// <inheritdoc />
    public override string Description =>
        "Lists available context resources that can be retrieved on demand. " +
        "Use this to discover what reference materials, guidelines, or documents are available.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var context = _contextAccessor.Context;
        if (context is null || context.OnDemandResources.Count == 0)
        {
            return Task.FromResult<object>(new ListContextResourcesResult(
                [],
                "No on-demand context resources are available."));
        }

        var resources = context.OnDemandResources.Select(r => new ContextResourceSummary(
            r.Id,
            r.Name,
            r.Description,
            r.ResourceTypeId,
            r.ContextName)).ToList();

        return Task.FromResult<object>(new ListContextResourcesResult(
            resources,
            $"Found {resources.Count} on-demand resource(s). Use get_context_resource with a resource ID to retrieve content."));
    }
}

/// <summary>
/// Result of the list context resources tool.
/// </summary>
/// <param name="Resources">The available resources.</param>
/// <param name="Message">A message describing the result.</param>
public record ListContextResourcesResult(
    IReadOnlyList<ContextResourceSummary> Resources,
    string Message);

/// <summary>
/// Summary information about a context resource.
/// </summary>
/// <param name="Id">The resource ID.</param>
/// <param name="Name">The resource name.</param>
/// <param name="Description">The resource description.</param>
/// <param name="ResourceType">The resource type identifier.</param>
/// <param name="ContextName">The name of the parent context.</param>
public record ContextResourceSummary(
    Guid Id,
    string Name,
    string? Description,
    string ResourceType,
    string ContextName);
