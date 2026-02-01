namespace Umbraco.Ai.Core.RuntimeContext.Contributors;

/// <summary>
/// Default fallback contributor that adds context item descriptions to system messages.
/// Handles items that weren't processed by more specific contributors.
/// </summary>
internal sealed class DefaultSystemMessageContributor : IAiRuntimeContextContributor
{
    /// <inheritdoc />
    public void Contribute(AiRuntimeContext context)
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
