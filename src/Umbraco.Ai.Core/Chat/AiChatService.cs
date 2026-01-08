using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Context.Middleware;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Core.Chat;

internal sealed class AiChatService : IAiChatService
{
    private readonly IAiChatClientFactory _clientFactory;
    private readonly IAiProfileService _profileService;
    private readonly AiOptions _options;

    public AiChatService(
        IAiChatClientFactory clientFactory,
        IAiProfileService profileService,
        IOptionsMonitor<AiOptions> options)
    {
        _clientFactory = clientFactory;
        _profileService = profileService;
        _options = options.CurrentValue;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetDefaultProfileAsync(AiCapability.Chat, cancellationToken);
        return await GetResponseInternalAsync(profile, messages, options, cancellationToken);
    }

    public async Task<ChatResponse> GetResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileId, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }
        
        EnsureProfileSupportsChat(profile);
        
        return await GetResponseInternalAsync(profile, messages, options, cancellationToken);
    }

    private async Task<ChatResponse> GetResponseInternalAsync(
        AiProfile profile,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);
        var mergedOptions = MergeOptions(profile, options);

        return await chatClient.GetResponseAsync(messages.ToList(), mergedOptions, cancellationToken);
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetDefaultProfileAsync(AiCapability.Chat, cancellationToken);
        await foreach (var update in GetStreamingResponseInternalAsync(profile, messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        Guid profileId,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileId, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsChat(profile);

        await foreach (var update in GetStreamingResponseInternalAsync(profile, messages, options, cancellationToken))
        {
            yield return update;
        }
    }

    private async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseInternalAsync(
        AiProfile profile,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var chatClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);
        var mergedOptions = MergeOptions(profile, options);

        await foreach (var update in chatClient.GetStreamingResponseAsync(messages.ToList(), mergedOptions, cancellationToken))
        {
            yield return update;
        }
    }

    public async Task<IChatClient> GetChatClientAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        var profile = profileId.HasValue
            ? await _profileService.GetProfileAsync(profileId.Value, cancellationToken)
            : await _profileService.GetDefaultProfileAsync(AiCapability.Chat, cancellationToken);
        
        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }
        
        EnsureProfileSupportsChat(profile);

        return await _clientFactory.CreateClientAsync(profile, cancellationToken);
    }

    private static ChatOptions MergeOptions(AiProfile profile, ChatOptions? callerOptions)
    {
        var chatSettings = profile.Settings as AiChatProfileSettings;

        // Create additional properties with profile ID for context middleware
        var additionalProperties = callerOptions?.AdditionalProperties != null
            ? new AdditionalPropertiesDictionary(callerOptions.AdditionalProperties)
            : new AdditionalPropertiesDictionary();

        // Always set the profile ID for context injection middleware
        additionalProperties[ContextInjectingChatClient.ProfileIdKey] = profile.Id;

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
                AdditionalProperties = additionalProperties
            };
        }

        // No caller options, use profile defaults
        return new ChatOptions
        {
            ModelId = profile.Model.ModelId,
            Temperature = chatSettings?.Temperature,
            MaxOutputTokens = chatSettings?.MaxTokens,
            AdditionalProperties = additionalProperties
        };
    }
    
    private void EnsureProfileSupportsChat(AiProfile profile)
    {
        if (profile.Capability != AiCapability.Chat)
        {
            throw new InvalidOperationException($"The profile '{profile.Name}' does not support chat capability.");
        }
    }
}
