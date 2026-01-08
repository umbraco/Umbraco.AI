using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Umbraco.Ai.Core.Context.Middleware;

/// <summary>
/// A delegating chat client that injects AI context into chat requests.
/// </summary>
/// <remarks>
/// This client:
/// - Resolves context based on the profile ID from request options
/// - Injects "Always" mode resources into the system prompt
/// - Makes the resolved context available via <see cref="IAiContextAccessor"/> for OnDemand tools
/// </remarks>
internal sealed class ContextInjectingChatClient : IChatClient
{
    /// <summary>
    /// Key used to pass the profile ID through ChatOptions.AdditionalProperties.
    /// </summary>
    public const string ProfileIdKey = "Umbraco.Ai.ProfileId";

    private readonly IChatClient _innerClient;
    private readonly IAiContextResolver _contextResolver;
    private readonly IAiContextFormatter _contextFormatter;
    private readonly IAiContextAccessor _contextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextInjectingChatClient"/> class.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="contextResolver">The context resolver.</param>
    /// <param name="contextFormatter">The context formatter.</param>
    /// <param name="contextAccessor">The context accessor for tool access.</param>
    public ContextInjectingChatClient(
        IChatClient innerClient,
        IAiContextResolver contextResolver,
        IAiContextFormatter contextFormatter,
        IAiContextAccessor contextAccessor)
    {
        _innerClient = innerClient;
        _contextResolver = contextResolver;
        _contextFormatter = contextFormatter;
        _contextAccessor = contextAccessor;
    }


    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        var (modifiedMessages, contextScope) = await PrepareContextAsync(messagesList, options, cancellationToken);

        try
        {
            return await _innerClient.GetResponseAsync(modifiedMessages, options, cancellationToken);
        }
        finally
        {
            contextScope?.Dispose();
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        var (modifiedMessages, contextScope) = await PrepareContextAsync(messagesList, options, cancellationToken);

        try
        {
            await foreach (var update in _innerClient.GetStreamingResponseAsync(modifiedMessages, options, cancellationToken))
            {
                yield return update;
            }
        }
        finally
        {
            contextScope?.Dispose();
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? key = null)
    {
        return _innerClient.GetService(serviceType, key);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _innerClient.Dispose();
    }

    private async Task<(IList<ChatMessage> ModifiedMessages, IDisposable? ContextScope)> PrepareContextAsync(
        IList<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        // Extract profile ID from options
        var profileId = GetProfileIdFromOptions(options);
        if (!profileId.HasValue)
        {
            // No profile ID, skip context injection
            return (messages, null);
        }

        // Resolve context for this profile
        var resolvedContext = await _contextResolver.ResolveForProfileAsync(profileId.Value, cancellationToken);

        // If no context resources, nothing to inject
        if (resolvedContext.AllResources.Count == 0)
        {
            return (messages, null);
        }

        // Set the resolved context in the accessor for OnDemand tools
        var contextScope = _contextAccessor.SetContext(resolvedContext);

        // If there are "Always" injection resources, inject them into the system prompt
        if (resolvedContext.InjectedResources.Count > 0)
        {
            var contextContent = _contextFormatter.FormatForSystemPrompt(resolvedContext);
            if (!string.IsNullOrWhiteSpace(contextContent))
            {
                messages = InjectContextIntoMessages(messages, contextContent);
            }
        }

        return (messages, contextScope);
    }

    private static Guid? GetProfileIdFromOptions(ChatOptions? options)
    {
        if (options?.AdditionalProperties is null)
        {
            return null;
        }

        if (options.AdditionalProperties.TryGetValue(ProfileIdKey, out var value))
        {
            return value switch
            {
                Guid guid => guid,
                string str when Guid.TryParse(str, out var parsed) => parsed,
                _ => null
            };
        }

        return null;
    }

    private static IList<ChatMessage> InjectContextIntoMessages(
        IList<ChatMessage> messages,
        string contextContent)
    {
        // Create a modifiable copy
        var modifiedMessages = messages.ToList();

        // Find the first system message, or insert one at the beginning
        var systemMessageIndex = modifiedMessages
            .Select((msg, idx) => new { Message = msg, Index = idx })
            .FirstOrDefault(x => x.Message.Role == ChatRole.System)?.Index;

        if (systemMessageIndex.HasValue)
        {
            // Append context to existing system message
            var existingMessage = modifiedMessages[systemMessageIndex.Value];
            var existingContent = existingMessage.Text ?? string.Empty;
            var newContent = string.IsNullOrWhiteSpace(existingContent)
                ? contextContent
                : $"{existingContent}\n\n{contextContent}";

            modifiedMessages[systemMessageIndex.Value] = new ChatMessage(ChatRole.System, newContent);
        }
        else
        {
            // Insert new system message at the beginning
            modifiedMessages.Insert(0, new ChatMessage(ChatRole.System, contextContent));
        }

        return modifiedMessages;
    }
}
