namespace Umbraco.AI.Core.RuntimeContext.Contributors;

/// <summary>
/// Default fallback contributor that adds context item descriptions to system messages.
/// Handles items that weren't processed by more specific contributors.
/// </summary>
internal sealed class DefaultSystemMessageContributor : IAIRuntimeContextContributor
{
    /// <inheritdoc />
    public void Contribute(AIRuntimeContext context)
    {
        context.RequestContextItems.HandleUnhandled(item =>
        {
            if (!string.IsNullOrWhiteSpace(item.Description))
            {
                context.SystemMessageParts.Add($"Context: {item.Description}");
            }
        });
    }
}
