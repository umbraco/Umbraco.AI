using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

internal sealed class AISpeechToTextService : IAISpeechToTextService
{
    private readonly IAISpeechToTextClientFactory _clientFactory;
    private readonly IAIProfileService _profileService;
    private readonly AIOptions _options;

    public AISpeechToTextService(
        IAISpeechToTextClientFactory clientFactory,
        IAIProfileService profileService,
        IOptionsMonitor<AIOptions> options)
    {
        _clientFactory = clientFactory;
        _profileService = profileService;
        _options = options.CurrentValue;
    }

    public async Task<SpeechToTextResponse> TranscribeAsync(
        Stream audioStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetDefaultProfileAsync(AICapability.SpeechToText, cancellationToken);
        return await TranscribeInternalAsync(profile, audioStream, options, cancellationToken);
    }

    public async Task<SpeechToTextResponse> TranscribeAsync(
        Guid profileId,
        Stream audioStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileId, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsSpeechToText(profile);

        return await TranscribeInternalAsync(profile, audioStream, options, cancellationToken);
    }

    public async Task<ISpeechToTextClient> GetSpeechToTextClientAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        var profile = profileId.HasValue
            ? await _profileService.GetProfileAsync(profileId.Value, cancellationToken)
            : await _profileService.GetDefaultProfileAsync(AICapability.SpeechToText, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsSpeechToText(profile);

        return await _clientFactory.CreateClientAsync(profile, cancellationToken);
    }

    private async Task<SpeechToTextResponse> TranscribeInternalAsync(
        AIProfile profile,
        Stream audioStream,
        SpeechToTextOptions? options,
        CancellationToken cancellationToken)
    {
        var client = await _clientFactory.CreateClientAsync(profile, cancellationToken);
        var mergedOptions = MergeOptions(profile, options);

        return await client.GetTextAsync(audioStream, mergedOptions, cancellationToken);
    }

    private static SpeechToTextOptions? MergeOptions(AIProfile profile, SpeechToTextOptions? callerOptions)
    {
        var profileSettings = profile.Settings as AISpeechToTextProfileSettings;

        if (callerOptions != null)
        {
            return new SpeechToTextOptions
            {
                ModelId = callerOptions.ModelId ?? profile.Model.ModelId,
                SpeechLanguage = callerOptions.SpeechLanguage ?? profileSettings?.Language,
                AdditionalProperties = callerOptions.AdditionalProperties
            };
        }

        return new SpeechToTextOptions
        {
            ModelId = profile.Model.ModelId,
            SpeechLanguage = profileSettings?.Language
        };
    }

    private static void EnsureProfileSupportsSpeechToText(AIProfile profile)
    {
        if (profile.Capability != AICapability.SpeechToText)
        {
            throw new InvalidOperationException($"The profile '{profile.Name}' does not support speech-to-text capability.");
        }
    }
}
