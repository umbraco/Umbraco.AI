using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.InlineChat;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Chat;

internal sealed class AIChatService : IAIChatService
{
    private readonly IAIChatClientFactory _clientFactory;
    private readonly IAIProfileService _profileService;
    private readonly IAIGuardrailService _guardrailService;
    private readonly AIOptions _options;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    public AIChatService(
        IAIChatClientFactory clientFactory,
        IAIProfileService profileService,
        IAIGuardrailService guardrailService,
        IOptionsMonitor<AIOptions> options,
        IEventAggregator eventAggregator,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
    {
        _clientFactory = clientFactory;
        _profileService = profileService;
        _guardrailService = guardrailService;
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
        => GetChatResponseAsync(
            b => ConfigureLegacyChat(b, profileId: null, options),
            messages, cancellationToken);

    public Task<ChatResponse> GetChatResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetChatResponseAsync(
            b => ConfigureLegacyChat(b, profileId, options),
            messages, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingChatResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => StreamChatResponseAsync(
            b => ConfigureLegacyChat(b, profileId: null, options),
            messages, cancellationToken);

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingChatResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => StreamChatResponseAsync(
            b => ConfigureLegacyChat(b, profileId, options),
            messages, cancellationToken);

    public Task<IChatClient> GetChatClientAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
        => CreateChatClientAsync(
            b => ConfigureLegacyChat(b, profileId, options: null),
            cancellationToken);

    #pragma warning restore CS0618

    private static void ConfigureLegacyChat(AIChatBuilder builder, Guid? profileId, ChatOptions? options)
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

    public async Task<ChatResponse> GetChatResponseAsync(
        Action<AIChatBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(messages);

        var builder = BuildChat(configure);

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

    public async IAsyncEnumerable<ChatResponseUpdate> StreamChatResponseAsync(
        Action<AIChatBuilder> configure,
        IEnumerable<ChatMessage> messages,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(messages);

        var builder = BuildChat(configure);

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

    public async Task<ChatResponse<T>> GetStructuredChatResponseAsync<T>(
        Action<AIChatBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(messages);

        var builder = BuildChat(configure);

        // Pass-through mode: skip notifications and duration tracking.
        if (builder.IsPassThrough)
        {
            return await ExecuteStructuredChatAsync<T>(builder, messages, cancellationToken);
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
            var response = await ExecuteStructuredChatAsync<T>(builder, messages, cancellationToken);
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

    private async Task<ChatResponse<T>> ExecuteStructuredChatAsync<T>(
        AIChatBuilder builder,
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

            await ResolveBuilderAliasesAsync(builder, cancellationToken);
            builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !builder.IsPassThrough);

            var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);
            var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, builder.ChatOptions);

            return await chatClient.GetResponseAsync<T>(messages.ToList(), mergedOptions, cancellationToken: cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    public async Task<ChatResponse<JsonElement>> GetStructuredChatResponseAsync(
        AIOutputSchema schema,
        Action<AIChatBuilder> configure,
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(messages);

        var builder = BuildChat(configure);

        // Pass-through mode: skip notifications and duration tracking.
        if (builder.IsPassThrough)
        {
            return await ExecuteSchemaStructuredChatAsync(schema, builder, messages, cancellationToken);
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
            var response = await ExecuteSchemaStructuredChatAsync(schema, builder, messages, cancellationToken);
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

    private async Task<ChatResponse<JsonElement>> ExecuteSchemaStructuredChatAsync(
        AIOutputSchema schema,
        AIChatBuilder builder,
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

            await ResolveBuilderAliasesAsync(builder, cancellationToken);
            builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !builder.IsPassThrough);

            var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);
            var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, builder.ChatOptions);

            // Apply schema as response format, then use M.E.AI's typed extension
            // to get the response deserialized as JsonElement
            mergedOptions.ResponseFormat = schema.ResponseFormat;

            return await chatClient.GetResponseAsync<JsonElement>(
                messages.ToList(), mergedOptions, cancellationToken: cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    private async Task<ChatResponse> ExecuteInlineChatAsync(
        AIChatBuilder builder,
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

            await ResolveBuilderAliasesAsync(builder, cancellationToken);
            builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !builder.IsPassThrough);

            var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);
            var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, builder.ChatOptions);

            return await chatClient.GetResponseAsync(messages.ToList(), mergedOptions, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    private async IAsyncEnumerable<ChatResponseUpdate> StreamInlineChatCoreAsync(
        AIChatBuilder builder,
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

            await ResolveBuilderAliasesAsync(builder, cancellationToken);
            builder.PopulateContext(_contextAccessor.Context!, setFeatureMetadata: !builder.IsPassThrough);

            var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);
            var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, builder.ChatOptions);

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

    public async Task<IChatClient> CreateChatClientAsync(
        Action<AIChatBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = BuildChat(configure);

        // Resolve aliases and profile
        await ResolveBuilderAliasesAsync(builder, cancellationToken);
        var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);

        // Create the base client with middleware
        var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);

        // Wrap in ScopedInlineChatClient for per-call scope management
        return new ScopedInlineChatClient(chatClient, builder, _contextAccessor, _scopeProvider, _contributors);
    }

    private static AIChatBuilder BuildChat(Action<AIChatBuilder> configure)
    {
        var builder = new AIChatBuilder();
        configure(builder);
        builder.Validate();
        return builder;
    }

    private async Task<AIProfile> ResolveProfileAsync(Guid? profileId, string? profileAlias, CancellationToken cancellationToken)
    {
        // Resolve alias to ID if needed
        if (!profileId.HasValue && !string.IsNullOrWhiteSpace(profileAlias))
        {
            profileId = await _profileService.GetProfileIdByAliasAsync(profileAlias, cancellationToken);
        }

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

    /// <summary>
    /// Resolves any alias-based references on the builder to their corresponding IDs.
    /// </summary>
    private async Task ResolveBuilderAliasesAsync(AIChatBuilder builder, CancellationToken cancellationToken)
    {
        if (builder.GuardrailAliases is { Count: > 0 } aliases)
        {
            builder.SetResolvedGuardrailIds(
                await _guardrailService.GetGuardrailIdsByAliasesAsync(aliases, cancellationToken));
        }
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
