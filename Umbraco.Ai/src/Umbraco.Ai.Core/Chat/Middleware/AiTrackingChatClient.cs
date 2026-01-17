using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Chat.Middleware;

/// <summary>
/// A chat client that tracks the last usage details and response text.
/// </summary>
/// <param name="innerClient"></param>
internal sealed class AiTrackingChatClient(IChatClient innerClient) : AiBoundChatClientBase(innerClient)
{
    /// <summary>
    /// The last usage details received from the chat client.
    /// </summary>
    public UsageDetails? LastUsageDetails { get; private set; }
    
    /// <summary>
    /// The last response text received from the chat client.
    /// </summary>
    public string? LastResponse { get; private set; }

    /// <inheritdoc />
    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var response =  await base.GetResponseAsync(chatMessages, options, cancellationToken); 
        
        LastUsageDetails = response.Usage;
        LastResponse = response.Text;
        
        return response;
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var stream = base.GetStreamingResponseAsync(chatMessages, options, cancellationToken);
        
        var lastUpdate = default(ChatResponseUpdate);
        var lastContents = new StringBuilder();
            
        // Stream updates - audit-log completion handled after streaming completes
        await foreach (var update in stream)
        {
            lastUpdate = update;
            lastContents.Append(update.Text);
            yield return update;
        }
        
        var usage = lastUpdate?.Contents.OfType<UsageContent>().FirstOrDefault();
        
        LastUsageDetails = usage?.Details;
        LastResponse = lastContents.ToString();
    }
}