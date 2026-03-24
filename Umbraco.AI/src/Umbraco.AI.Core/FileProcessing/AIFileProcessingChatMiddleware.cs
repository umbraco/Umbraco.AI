using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Chat;
using Umbraco.Cms.Core.Configuration.Models;

namespace Umbraco.AI.Core.FileProcessing;

/// <summary>
/// Chat middleware that processes uploaded files, converting supported file types
/// (e.g., Office documents) into text content that LLMs can understand.
/// </summary>
/// <remarks>
/// This middleware scans chat messages for <see cref="DataContent"/> items and delegates
/// to registered <see cref="IAIFileProcessingHandler"/> implementations to extract text.
/// Unsupported file types (images, PDFs, etc.) pass through untouched.
/// Files with disallowed extensions (per CMS content settings) are silently stripped.
/// </remarks>
public class AIFileProcessingChatMiddleware : IAIChatMiddleware
{
    private readonly AIFileProcessingHandlerCollection _handlers;
    private readonly IOptionsMonitor<ContentSettings> _contentSettings;
    private readonly ILogger<AIFileProcessingChatMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFileProcessingChatMiddleware"/> class.
    /// </summary>
    /// <param name="handlers">The collection of file processing handlers.</param>
    /// <param name="contentSettings">The CMS content settings for file upload validation.</param>
    /// <param name="logger">The logger.</param>
    public AIFileProcessingChatMiddleware(
        AIFileProcessingHandlerCollection handlers,
        IOptionsMonitor<ContentSettings> contentSettings,
        ILogger<AIFileProcessingChatMiddleware> logger)
    {
        _handlers = handlers;
        _contentSettings = contentSettings;
        _logger = logger;
    }

    /// <inheritdoc />
    public IChatClient Apply(IChatClient client)
    {
        return new AIFileProcessingChatClient(client, _handlers, _contentSettings, _logger);
    }
}
