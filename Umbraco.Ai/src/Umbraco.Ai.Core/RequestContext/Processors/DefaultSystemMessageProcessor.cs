namespace Umbraco.Ai.Core.RequestContext.Processors;

/// <summary>
/// Default fallback processor that adds context item descriptions to system messages.
/// Handles items that weren't processed by more specific processors.
/// </summary>
internal sealed class DefaultSystemMessageProcessor : IAiRequestContextProcessor
{
    /// <inheritdoc />
    public bool CanHandle(AiRequestContextItem item)
    {
        // This is a fallback - only handle items with descriptions that haven't
        // been fully processed by other processors. Since we can't know if other
        // processors handled an item, we always return true and let the description
        // be added to the system message.
        return !string.IsNullOrWhiteSpace(item.Description);
    }

    /// <inheritdoc />
    public void Process(AiRequestContextItem item, AiRequestContext context)
    {
        // Add the description as a system message part if it's meaningful
        // and doesn't look like it's already been processed (e.g., by SerializedEntityProcessor)
        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            // Check if this is an entity context item - SerializedEntityProcessor already handles these
            if (item.Value.HasValue &&
                item.Value.Value.ValueKind == System.Text.Json.JsonValueKind.Object &&
                item.Value.Value.TryGetProperty("entityType", out _) &&
                item.Value.Value.TryGetProperty("properties", out _))
            {
                // Skip - SerializedEntityProcessor handles this type
                return;
            }

            // Add the description as context
            context.SystemMessageParts.Add($"Context: {item.Description}");
        }
    }
}
