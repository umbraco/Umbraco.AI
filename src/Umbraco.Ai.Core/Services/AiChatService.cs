using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Factories;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Core.Services;

internal sealed class AiChatService : IAiChatService
{
    private readonly IAiChatClientFactory _clientFactory;
    private readonly IAiProfileResolver _profileResolver;
    private readonly AiOptions _options;

    public AiChatService(
        IAiChatClientFactory clientFactory,
        IAiProfileResolver profileResolver,
        IOptionsMonitor<AiOptions> options)
    {
        _clientFactory = clientFactory;
        _profileResolver = profileResolver;
        _options = options.CurrentValue;
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = _profileResolver.GetDefaultProfile(AiCapability.Chat);
        return GetResponseInternalAsync(profile, messages, options, cancellationToken);
    }

    public Task<ChatResponse> GetResponseAsync(
        string profileName,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = _profileResolver.GetProfile(profileName);
        
        EnsureProfileSupportsChat(profile);
        
        return GetResponseInternalAsync(profile, messages, options, cancellationToken);
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

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = _profileResolver.GetDefaultProfile(AiCapability.Chat);
        return GetStreamingResponseInternalAsync(profile, messages, options, cancellationToken);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        string profileName,
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = _profileResolver.GetProfile(profileName);
        
        EnsureProfileSupportsChat(profile);
        
        return GetStreamingResponseInternalAsync(profile, messages, options, cancellationToken);
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
        string? profileName = null,
        CancellationToken cancellationToken = default)
    {
        var profile = string.IsNullOrWhiteSpace(profileName)
            ? _profileResolver.GetDefaultProfile(AiCapability.Chat)
            : _profileResolver.GetProfile(profileName);
        
        EnsureProfileSupportsChat(profile);

        return await _clientFactory.CreateClientAsync(profile, cancellationToken);
    }

    private static ChatOptions MergeOptions(AiProfile profile, ChatOptions? callerOptions)
    {
        // If caller provides options, merge with profile defaults
        // Caller options take precedence over profile settings
        if (callerOptions != null)
        {
            return new ChatOptions
            {
                ModelId = callerOptions.ModelId ?? profile.Model.ModelId,
                Temperature = callerOptions.Temperature ?? (float?)profile.Temperature,
                MaxOutputTokens = callerOptions.MaxOutputTokens ?? profile.MaxTokens,
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
            Temperature = (float?)profile.Temperature,
            MaxOutputTokens = profile.MaxTokens
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
