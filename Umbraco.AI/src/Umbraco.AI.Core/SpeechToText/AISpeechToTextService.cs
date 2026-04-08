using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Events;

#pragma warning disable MEAI001 // ISpeechToTextClient is experimental in M.E.AI

namespace Umbraco.AI.Core.SpeechToText;

internal sealed class AISpeechToTextService : IAISpeechToTextService
{
    private readonly IAISpeechToTextClientFactory _clientFactory;
    private readonly IAIProfileService _profileService;
    private readonly IAIGuardrailService _guardrailService;
    private readonly AIOptions _options;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    public AISpeechToTextService(
        IAISpeechToTextClientFactory clientFactory,
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

    public Task<SpeechToTextResponse> TranscribeAsync(
        Stream audioStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => TranscribeAsync(
            b => ConfigureLegacy(b, profileId: null, options),
            audioStream, cancellationToken);

    public Task<SpeechToTextResponse> TranscribeAsync(
        Guid profileId,
        Stream audioStream,
        SpeechToTextOptions? options = null,
        CancellationToken cancellationToken = default)
        => TranscribeAsync(
            b => ConfigureLegacy(b, profileId, options),
            audioStream, cancellationToken);

    public Task<ISpeechToTextClient> GetSpeechToTextClientAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
        => CreateSpeechToTextClientAsync(
            b => ConfigureLegacy(b, profileId, options: null),
            cancellationToken);

    #pragma warning restore CS0618

    private static void ConfigureLegacy(AISpeechToTextBuilder builder, Guid? profileId, SpeechToTextOptions? options)
    {
        builder.WithAlias("speech-to-text");
        if (profileId.HasValue)
        {
            builder.WithProfile(profileId.Value);
        }
        if (options is not null)
        {
            builder.WithSpeechToTextOptions(options);
        }
    }

    public async Task<SpeechToTextResponse> TranscribeAsync(
        Action<AISpeechToTextBuilder> configure,
        Stream audioStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(audioStream);

        var builder = BuildTranscription(configure);

        // Pass-through mode: skip notifications and duration tracking.
        // The parent feature handles its own observability.
        if (builder.IsPassThrough)
        {
            return await ExecuteTranscriptionAsync(builder, audioStream, cancellationToken);
        }

        // Publish executing notification
        var eventMessages = new EventMessages();
        var executingNotification = new AISpeechToTextExecutingNotification(
            builder.Id, builder.Alias!, builder.Name, builder.ProfileId, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline speech-to-text execution cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        bool isSuccess = false;

        try
        {
            var response = await ExecuteTranscriptionAsync(builder, audioStream, cancellationToken);
            isSuccess = true;
            return response;
        }
        finally
        {
            var executedNotification = new AISpeechToTextExecutedNotification(
                builder.Id, builder.Alias!, builder.Name, builder.ProfileId,
                stopwatch.Elapsed, isSuccess, eventMessages);
            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    public async IAsyncEnumerable<SpeechToTextResponseUpdate> StreamTranscriptionAsync(
        Action<AISpeechToTextBuilder> configure,
        Stream audioStream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(audioStream);

        var builder = BuildTranscription(configure);

        // Pass-through mode: skip notifications and duration tracking
        if (builder.IsPassThrough)
        {
            await foreach (var update in StreamTranscriptionCoreAsync(builder, audioStream, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        // Publish executing notification
        var eventMessages = new EventMessages();
        var executingNotification = new AISpeechToTextExecutingNotification(
            builder.Id, builder.Alias!, builder.Name, builder.ProfileId, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline speech-to-text execution cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        bool isSuccess = false;

        try
        {
            await foreach (var update in StreamTranscriptionCoreAsync(builder, audioStream, cancellationToken))
            {
                yield return update;
            }

            isSuccess = true;
        }
        finally
        {
            var executedNotification = new AISpeechToTextExecutedNotification(
                builder.Id, builder.Alias!, builder.Name, builder.ProfileId,
                stopwatch.Elapsed, isSuccess, eventMessages);
            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    public async Task<ISpeechToTextClient> CreateSpeechToTextClientAsync(
        Action<AISpeechToTextBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = BuildTranscription(configure);

        // Resolve aliases and profile
        await ResolveBuilderAliasesAsync(builder, cancellationToken);
        var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);

        // Create the base client with middleware
        var client = await _clientFactory.CreateClientAsync(profile, cancellationToken);

        // Wrap in ScopedInlineSpeechToTextClient for per-call scope management
        return new ScopedInlineSpeechToTextClient(client, builder, _contextAccessor, _scopeProvider, _contributors);
    }

    private async Task<SpeechToTextResponse> ExecuteTranscriptionAsync(
        AISpeechToTextBuilder builder,
        Stream audioStream,
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
            var client = await _clientFactory.CreateClientAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, builder.SpeechToTextOptions);

            return await client.GetTextAsync(audioStream, mergedOptions, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    private async IAsyncEnumerable<SpeechToTextResponseUpdate> StreamTranscriptionCoreAsync(
        AISpeechToTextBuilder builder,
        Stream audioStream,
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
            var client = await _clientFactory.CreateClientAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, builder.SpeechToTextOptions);

            await foreach (var update in client.GetStreamingTextAsync(audioStream, mergedOptions, cancellationToken))
            {
                yield return update;
            }
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    private static AISpeechToTextBuilder BuildTranscription(Action<AISpeechToTextBuilder> configure)
    {
        var builder = new AISpeechToTextBuilder();
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
            : await _profileService.GetDefaultProfileAsync(AICapability.SpeechToText, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsSpeechToText(profile);
        return profile;
    }

    /// <summary>
    /// Resolves any alias-based references on the builder to their corresponding IDs.
    /// </summary>
    private async Task ResolveBuilderAliasesAsync(AISpeechToTextBuilder builder, CancellationToken cancellationToken)
    {
        if (builder.GuardrailAliases is { Count: > 0 } aliases)
        {
            builder.SetResolvedGuardrailIds(
                await _guardrailService.GetGuardrailIdsByAliasesAsync(aliases, cancellationToken));
        }
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
