using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.InlineChat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Chat;

internal sealed class AIChatService : IAIChatService
{
    private readonly IAIChatClientFactory _clientFactory;
    private readonly IAIProfileService _profileService;
    private readonly AIOptions _options;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    public AIChatService(
        IAIChatClientFactory clientFactory,
        IAIProfileService profileService,
        IOptionsMonitor<AIOptions> options,
        IEventAggregator eventAggregator,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
    {
        _clientFactory = clientFactory;
        _profileService = profileService;
        _options = options.CurrentValue;
        _eventAggregator = eventAggregator;
        _contextAccessor = contextAccessor;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
    }

    #pragma warning disable CS0618 // Obsolete members - implementing the deprecated interface methods

    public Task<ChatResponse> GetChatResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetInlineChatResponseAsync(
            b => ConfigureLegacyChat(b, profileId: null, options),
            messages, cancellationToken);

    public Task<ChatResponse> GetChatResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetInlineChatResponseAsync(
            b => ConfigureLegacyChat(b, profileId, options),
            messages, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingChatResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => StreamInlineChatResponseAsync(
            b => ConfigureLegacyChat(b, profileId: null, options),
            messages, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingChatResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => StreamInlineChatResponseAsync(
            b => ConfigureLegacyChat(b, profileId, options),
            messages, cancellationToken);

    public Task<IChatClient> GetChatClientAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
        => CreateInlineChatClientAsync(
            b => ConfigureLegacyChat(b, profileId, options: null),
            cancellationToken);

    #pragma warning restore CS0618

    private static void ConfigureLegacyChat(AIInlineChatBuilder builder, Guid? profileId, ChatOptions? options)
    {
        builder.WithAlias("chat");
        if (profileId.HasValue)
        {
            builder.WithProfile(profileId.Value);
        }
        if (options is not null)
        {
            builder.WithChatOptions(options);
        }
    }

    public async Task<ChatResponse> GetInlineChatResponseAsync(
        Action<AIInlineChatBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(messages);

        var builder = BuildInlineChat(configure);

        // Pass-through mode: skip notifications and duration tracking.
        // The parent feature (e.g., prompt) handles its own observability.
        if (builder.IsPassThrough)
        {
            return await ExecuteInlineChatAsync(builder, messages, cancellationToken);
        }

        // Publish executing notification
        var eventMessages = new EventMessages();
        var executingNotification = new AIChatExecutingNotification(
            builder.Id, builder.Alias!, builder.Name, builder.ProfileId, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline chat execution cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        bool isSuccess = false;

        try
        {
            var response = await ExecuteInlineChatAsync(builder, messages, cancellationToken);
            isSuccess = true;
            return response;
        }
        finally
        {
            var executedNotification = new AIChatExecutedNotification(
                builder.Id, builder.Alias!, builder.Name, builder.ProfileId,
                stopwatch.Elapsed, isSuccess, eventMessages);
            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> StreamInlineChatResponseAsync(
        Action<AIInlineChatBuilder> configure,
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(messages);

        var builder = BuildInlineChat(configure);

        // Pass-through mode: skip notifications and duration tracking
        if (builder.IsPassThrough)
        {
            await foreach (var update in StreamInlineChatCoreAsync(builder, messages, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        // Publish executing notification
        var eventMessages = new EventMessages();
        var executingNotification = new AIChatExecutingNotification(
            builder.Id, builder.Alias!, builder.Name, builder.ProfileId, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline chat execution cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        bool isSuccess = false;

        try
        {
            await foreach (var update in StreamInlineChatCoreAsync(builder, messages, cancellationToken))
            {
                yield return update;
            }

            isSuccess = true;
        }
        finally
        {
            var executedNotification = new AIChatExecutedNotification(
                builder.Id, builder.Alias!, builder.Name, builder.ProfileId,
                stopwatch.Elapsed, isSuccess, eventMessages);
            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    private async Task<ChatResponse> ExecuteInlineChatAsync(
        AIInlineChatBuilder builder,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken)
    {
        var scopeExisted = _contextAccessor.Context is not null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(builder.ContextItems ?? []);
                _contributors.Populate(createdScope.Context);
            }

            builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !builder.IsPassThrough);

            var profile = await ResolveProfileAsync(builder.ProfileId, cancellationToken);
            var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, callerOptions: null);

            return await chatClient.GetResponseAsync(messages.ToList(), mergedOptions, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    private async IAsyncEnumerable<ChatResponseUpdate> StreamInlineChatCoreAsync(
        AIInlineChatBuilder builder,
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var scopeExisted = _contextAccessor.Context is not null;
        IAIRuntimeContextScope? createdScope = null;

        try
        {
            if (!scopeExisted)
            {
                createdScope = _scopeProvider.CreateScope(builder.ContextItems ?? []);
                _contributors.Populate(createdScope.Context);
            }

            builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !builder.IsPassThrough);

            var profile = await ResolveProfileAsync(builder.ProfileId, cancellationToken);
            var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, callerOptions: null);

            await foreach (var update in chatClient.GetStreamingResponseAsync(messages.ToList(), mergedOptions, cancellationToken))
            {
                yield return update;
            }
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    public async Task<IChatClient> CreateInlineChatClientAsync(
        Action<AIInlineChatBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = BuildInlineChat(configure);

        // Resolve profile
        var profile = await ResolveProfileAsync(builder.ProfileId, cancellationToken);

        // Create the base client with middleware
        var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);

        // Wrap in ScopedInlineChatClient for per-call scope management
        return new ScopedInlineChatClient(chatClient, builder, _contextAccessor, _scopeProvider, _contributors);
    }

    private static AIInlineChatBuilder BuildInlineChat(Action<AIInlineChatBuilder> configure)
    {
        var builder = new AIInlineChatBuilder();
        configure(builder);
        builder.Validate();
        return builder;
    }

    private async Task<AIProfile> ResolveProfileAsync(Guid? profileId, CancellationToken cancellationToken)
    {
        var profile = profileId.HasValue
            ? await _profileService.GetProfileAsync(profileId.Value, cancellationToken)
            : await _profileService.GetDefaultProfileAsync(AICapability.Chat, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsChat(profile);
        return profile;
    }



    private static ChatOptions MergeOptions(AIProfile profile, ChatOptions? callerOptions)
    {
        var chatSettings = profile.Settings as AIChatProfileSettings;

        // Note: Profile ID and telemetry metadata are automatically injected by AIChatClientFactory via RuntimeContext

        // If caller provides options, merge with profile defaults
        // Caller options take precedence over profile settings
        if (callerOptions != null)
        {
            return new ChatOptions
            {
                ModelId = callerOptions.ModelId ?? profile.Model.ModelId,
                Temperature = callerOptions.Temperature ?? chatSettings?.Temperature,
                MaxOutputTokens = callerOptions.MaxOutputTokens ?? chatSettings?.MaxTokens,
                // Copy other properties from caller options
                TopP = callerOptions.TopP,
                FrequencyPenalty = callerOptions.FrequencyPenalty,
                PresencePenalty = callerOptions.PresencePenalty,
                StopSequences = callerOptions.StopSequences,
                ResponseFormat = callerOptions.ResponseFormat,
                Tools = callerOptions.Tools,
                ToolMode = callerOptions.ToolMode,
                AdditionalProperties = callerOptions.AdditionalProperties
            };
        }

        // No caller options, use profile defaults
        return new ChatOptions
        {
            ModelId = profile.Model.ModelId,
            Temperature = chatSettings?.Temperature,
            MaxOutputTokens = chatSettings?.MaxTokens
        };
    }

    private void EnsureProfileSupportsChat(AIProfile profile)
    {
        if (profile.Capability != AICapability.Chat)
        {
            throw new InvalidOperationException($"The profile '{profile.Name}' does not support chat capability.");
        }
    }
}
