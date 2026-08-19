using System.ComponentModel;

using Umbraco.AI.Core.PropertyValueOperations;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.Cms.Core.Services;

namespace Umbraco.AI.Core.Tools.Umbraco;

/// <summary>
/// Arguments for the ClearUmbracoContentValue tool.
/// </summary>
public record ClearUmbracoContentValueArgs(
    [property: Description("The unique key (GUID) of the content item to update.")]
    Guid Key,

    [property: Description("Path identifying the property to clear, potentially nested inside a block. An array alternating property-alias segments and block-key segments. Must start and end with an alias segment.")]
    IReadOnlyList<UmbracoPropertyPathSegmentArg> Path,

    [property: Description("Optional culture code (e.g., 'en-US') when the content item varies by culture.")]
    string? Culture = null,

    [property: Description("Optional segment identifier when the content item is segmented.")]
    string? Segment = null);

/// <summary>
/// Tool that clears a content property's value back to the editor's empty/null state, including
/// properties nested inside blocks.
/// </summary>
[AITool("clear_umbraco_content_value", "Clear Umbraco Content Value", ScopeId = ContentWriteScope.ScopeId, IsDestructive = true)]
public class ClearUmbracoContentValueTool(
    IContentEditingService contentEditingService,
    IAIPropertyValueDispatcher dispatcher,
    IUmbracoWriteAuthorizer authorizer)
    : AIToolBase<ClearUmbracoContentValueArgs>
{
    /// <inheritdoc />
    public override string Description =>
        "Clears a content property's value back to the editor's empty/null state, including properties " +
        "nested inside blocks (identify the nesting via Path). Persists immediately as a draft — call " +
        "publish_umbraco_content afterward to make the change live.";

    /// <inheritdoc />
    protected override async Task<object> ExecuteAsync(ClearUmbracoContentValueArgs args, CancellationToken cancellationToken = default)
    {
        var outcome = await ContentPropertyValueOperationHelper.ExecuteAsync(
            authorizer,
            contentEditingService,
            dispatcher,
            args.Key,
            args.Path,
            AIPropertyOperation.ClearValue,
            args: null,
            args.Culture,
            args.Segment,
            cancellationToken);

        return new ClearUmbracoContentValueResult(outcome.Success, outcome.Message);
    }
}

/// <summary>
/// Result of the clear Umbraco content value tool.
/// </summary>
public record ClearUmbracoContentValueResult(
    bool Success,
    string? Message);
