using System.ComponentModel;
using Umbraco.Ai.Core.Context;

namespace Umbraco.Ai.Core.Tools.Context;

/// <summary>
/// Arguments for the GetContextResource tool.
/// </summary>
/// <param name="ResourceId">The ID of the resource to retrieve.</param>
public record GetContextResourceArgs(
    [property: Description("The ID of the context resource to retrieve. Use list_context_resources to see available resources.")]
    Guid ResourceId);

/// <summary>
/// Tool that retrieves a specific on-demand context resource.
/// </summary>
/// <remarks>
/// This tool is dynamically injected when OnDemand resources are available.
/// Use list_context_resources first to see what's available.
/// </remarks>
[AiTool("get_context_resource", "Get Context Resource", Category = "Context")]
public class GetContextResourceTool : AiToolBase<GetContextResourceArgs>, IAiSystemTool
{
    private readonly IAiContextAccessor _contextAccessor;
    private readonly IAiContextFormatter _formatter;

    /// <summary>
    /// Initializes a new instance of <see cref="GetContextResourceTool"/>.
    /// </summary>
    /// <param name="contextAccessor">The context accessor.</param>
    /// <param name="formatter">The context formatter.</param>
    public GetContextResourceTool(
        IAiContextAccessor contextAccessor,
        IAiContextFormatter formatter)
    {
        _contextAccessor = contextAccessor;
        _formatter = formatter;
    }

    /// <inheritdoc />
    public override string Description =>
        "Retrieves the content of a specific context resource by ID. " +
        "Use list_context_resources first to discover available resources.";

    /// <inheritdoc />
    protected override Task<object> ExecuteAsync(GetContextResourceArgs args, CancellationToken cancellationToken = default)
    {
        var context = _contextAccessor.Context;
        if (context is null)
        {
            return Task.FromResult<object>(new GetContextResourceResult(
                false,
                null,
                "No context is available."));
        }

        var resource = context.OnDemandResources.FirstOrDefault(r => r.Id == args.ResourceId);
        if (resource is null)
        {
            return Task.FromResult<object>(new GetContextResourceResult(
                false,
                null,
                $"Resource with ID '{args.ResourceId}' not found. Use list_context_resources to see available resources."));
        }

        var formattedContent = _formatter.FormatResourceForLlm(resource);

        return Task.FromResult<object>(new GetContextResourceResult(
            true,
            new ContextResourceContent(
                resource.Id,
                resource.Name,
                resource.Description,
                resource.ResourceTypeId,
                resource.ContextName,
                formattedContent),
            null));
    }
}

/// <summary>
/// Result of the get context resource tool.
/// </summary>
/// <param name="Success">Whether the resource was found.</param>
/// <param name="Resource">The resource content, if found.</param>
/// <param name="Error">Error message if not found.</param>
public record GetContextResourceResult(
    bool Success,
    ContextResourceContent? Resource,
    string? Error);

/// <summary>
/// Full content of a context resource.
/// </summary>
/// <param name="Id">The resource ID.</param>
/// <param name="Name">The resource name.</param>
/// <param name="Description">The resource description.</param>
/// <param name="ResourceType">The resource type identifier.</param>
/// <param name="ContextName">The name of the parent context.</param>
/// <param name="Content">The formatted resource content.</param>
public record ContextResourceContent(
    Guid Id,
    string Name,
    string? Description,
    string ResourceType,
    string ContextName,
    string Content);
