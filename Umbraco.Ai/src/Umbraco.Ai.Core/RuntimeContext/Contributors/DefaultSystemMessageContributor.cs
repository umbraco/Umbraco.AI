namespace Umbraco.Ai.Core.RuntimeContext.Contributors;

/// <summary>
/// Default fallback contributor that adds context item descriptions to system messages.
/// Handles items that weren't processed by more specific contributors.
/// </summary>
internal sealed class DefaultSystemMessageContributor : IAiRuntimeContextContributor
{
    /// <inheritdoc />
    public bool CanHandle(AiRuntimeContextItem item)
    {
        // This is a fallback - only handle items with descriptions that haven't
        // been fully processed by other contributors. Since we can't know if other
        // contributors handled an item, we always return true and let the description
        // be added to the system message.
        return !string.IsNullOrWhiteSpace(item.Description);
    }

    /// <inheritdoc />
    public void Contribute(AiRuntimeContextItem item, AiRuntimeContext context)
    {
        // Add the description as a system message part if it's meaningful
        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            context.SystemMessageParts.Add($"Context: {item.Description}");
        }
    }
}
