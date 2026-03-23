using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Chat;

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// Chat middleware that processes uploaded files, converting supported file types
/// (e.g., Office documents) into text content that LLMs can understand.
/// </summary>
/// <remarks>
/// This middleware scans chat messages for <see cref="DataContent"/> items and delegates
/// to registered <see cref="IAIFileProcessingHandler"/> implementations to extract text.
/// Unsupported file types (images, PDFs, etc.) pass through untouched.
/// </remarks>
public class AIFileProcessingChatMiddleware : IAIChatMiddleware
{
    private readonly AIFileProcessingHandlerCollection _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFileProcessingChatMiddleware"/> class.
    /// </summary>
    /// <param name="handlers">The collection of file processing handlers.</param>
    public AIFileProcessingChatMiddleware(AIFileProcessingHandlerCollection handlers)
    {
        _handlers = handlers;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AIFileProcessingChatClient(client, _handlers);
    }
}
