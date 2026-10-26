using System.Diagnostics;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Events;

#pragma warning disable UMBRACOAI_EXTRACTION // Implements the experimental extraction capability surface

namespace Umbraco.AI.Core.Extraction;

internal sealed class AIExtractionService : IAIExtractionService
{
    private readonly IAIProfileService _profileService;
    private readonly IAIExtractionClientFactory _clientFactory;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    public AIExtractionService(
        IAIProfileService profileService,
        IAIExtractionClientFactory clientFactory,
        IEventAggregator eventAggregator,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors,
        IAIExperimentalFeatures experimentalFeatures)
    {
        _profileService = profileService;
        _clientFactory = clientFactory;
        _eventAggregator = eventAggregator;
        _contextAccessor = contextAccessor;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
        _experimentalFeatures = experimentalFeatures;
    }

    public Task<AIExtractionResponse<TAnswer>> ExtractAsync<TAnswer>(
        AIExtractionField<TAnswer> field,
        string? state = null,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIExtractionAnswer
        => ExtractAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias: null, options),
            field, state, cancellationToken);

    public Task<AIExtractionResponse<TAnswer>> ExtractAsync<TAnswer>(
        Guid profileId,
        AIExtractionField<TAnswer> field,
        string? state = null,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIExtractionAnswer
        => ExtractAsync(
            b => ConfigureFromProfileOverload(b, profileId, profileAlias: null, options),
            field, state, cancellationToken);

    public Task<AIExtractionResponse<TAnswer>> ExtractAsync<TAnswer>(
        string profileAlias,
        AIExtractionField<TAnswer> field,
        string? state = null,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIExtractionAnswer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileAlias);

        return ExtractAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias, options),
            field, state, cancellationToken);
    }

    public async Task<AIExtractionResponse<TAnswer>> ExtractAsync<TAnswer>(
        Action<AIExtractionBuilder> configure,
        AIExtractionField<TAnswer> field,
        string? state = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIExtractionAnswer
    {
        ArgumentNullException.ThrowIfNull(field);
        EnsureEnabled();

        var fieldWithId = EnsureId(field);
        var request = new AIExtractionRequest { State = state, Fields = [fieldWithId] };

        var response = await GetExtractionResponseAsync(configure, request, cancellationToken);

        return ToTypedResponse<TAnswer>(fieldWithId, response);
    }

    public Task<AIExtractionResponse> GetExtractionResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetExtractionResponseAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias: null, options),
            request, cancellationToken);

    public Task<AIExtractionResponse> GetExtractionResponseAsync(
        Guid profileId,
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetExtractionResponseAsync(
            b => ConfigureFromProfileOverload(b, profileId, profileAlias: null, options),
            request, cancellationToken);

    public Task<AIExtractionResponse> GetExtractionResponseAsync(
        string profileAlias,
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileAlias);

        return GetExtractionResponseAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias, options),
            request, cancellationToken);
    }

    public async Task<AIExtractionResponse> GetExtractionResponseAsync(
        Action<AIExtractionBuilder> configure,
        AIExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(request);
        EnsureEnabled();

        var builder = BuildExtraction(configure);

        // Pass-through mode: skip notifications and duration tracking.
        // The parent feature handles its own observability.
        if (builder.IsPassThrough)
        {
            return await ExecuteExtractionAsync(builder, request, cancellationToken);
        }

        // Publish executing notification
        var eventMessages = new EventMessages();
        var executingNotification = new AIExtractionExecutingNotification(
            builder.Id, builder.Alias!, builder.Name, builder.ProfileId, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline extraction execution cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        var isSuccess = false;

        try
        {
            var response = await ExecuteExtractionAsync(builder, request, cancellationToken);
            isSuccess = true;
            return response;
        }
        finally
        {
            var executedNotification = new AIExtractionExecutedNotification(
                builder.Id, builder.Alias!, builder.Name, builder.ProfileId,
                stopwatch.Elapsed, isSuccess, eventMessages);
            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    /// <summary>
    /// The service is registered unconditionally (the experimental gate lives only in
    /// <see cref="IAIExperimentalFeatures"/>), so every public path refuses here while the flag is off —
    /// before any profile lookup, notification or provider call.
    /// </summary>
    private void EnsureEnabled()
    {
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Extraction))
        {
            throw new InvalidOperationException(
                "Extraction is turned off. Set Umbraco:AI:Experimental:Extraction to true to use it.");
        }
    }

    private static void ConfigureFromProfileOverload(AIExtractionBuilder builder, Guid? profileId, string? profileAlias, AIExtractionOptions? options)
    {
        builder.WithAlias("extraction");
        if (profileId.HasValue)
        {
            builder.WithProfile(profileId.Value);
        }
        else if (profileAlias is not null)
        {
            builder.WithProfile(profileAlias);
        }

        if (options is not null)
        {
            builder.WithExtractionOptions(options);
        }
    }

    private async Task<AIExtractionResponse> ExecuteExtractionAsync(
        AIExtractionBuilder builder,
        AIExtractionRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);
        var innerClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);

        // Wrap in ScopedInlineExtractionClient for per-call scope management and inline extraction metadata.
        var client = new ScopedInlineExtractionClient(innerClient, builder, _contextAccessor, _scopeProvider, _contributors);
        return await client.GetResponseAsync(request, builder.Options, cancellationToken);
    }

    /// <summary>
    /// Unwraps the single answer <paramref name="fieldWithId"/>'s id was answered with.
    /// <see cref="IAIExtractionClient"/> stays non-generic, so nothing before this point knows
    /// <typeparamref name="TAnswer"/>. The real check for a provider answering the wrong shape already
    /// happened inside <see cref="AIErrorClassifyingExtractionClient"/>, which sits inside the tracking
    /// middleware, so a mismatch is recorded as a failure, not a false success. The checks below are only
    /// a defence-in-depth guard; they should never trip in practice.
    /// </summary>
    private static AIExtractionResponse<TAnswer> ToTypedResponse<TAnswer>(AIExtractionField fieldWithId, AIExtractionResponse response)
        where TAnswer : AIExtractionAnswer
    {
        if (!response.Answers.TryGetValue(fieldWithId.Id!, out var answer))
        {
            throw AIExtractionExceptionFactory.CreateMissingAnswerException(fieldWithId);
        }

        if (answer is not TAnswer typedAnswer)
        {
            throw AIExtractionExceptionFactory.CreateAnswerTypeMismatchException(fieldWithId, answer);
        }

        return new AIExtractionResponse<TAnswer>
        {
            Answer = typedAnswer,
            Answers = response.Answers,
            ModelId = response.ModelId,
            Usage = response.Usage,
            RawRepresentation = response.RawRepresentation,
        };
    }

    /// <summary>
    /// Returns <paramref name="field"/> unchanged when it already carries an <see cref="AIExtractionField.Id"/>,
    /// or a shallow copy with a freshly generated one otherwise (via <see cref="AIExtractionField.WithId"/>,
    /// which preserves every subclass's own properties) — <c>ExtractAsync</c>'s single-field convenience
    /// over the id'd batch pipeline.
    /// </summary>
    private static AIExtractionField<TAnswer> EnsureId<TAnswer>(AIExtractionField<TAnswer> field)
        where TAnswer : AIExtractionAnswer
    {
        if (field.Id is not null)
        {
            return field;
        }

        // Valid per ExtractionFieldValidator's id rule (^[A-Za-z_][A-Za-z0-9_]{0,63}$): a leading
        // letter, then 32 hex characters.
        return (AIExtractionField<TAnswer>)field.WithId("f" + Guid.NewGuid().ToString("N"));
    }

    private static AIExtractionBuilder BuildExtraction(Action<AIExtractionBuilder> configure)
    {
        var builder = new AIExtractionBuilder();
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
            : await _profileService.GetDefaultProfileAsync(AICapability.Extraction, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsExtraction(profile);
        return profile;
    }

    private static void EnsureProfileSupportsExtraction(AIProfile profile)
    {
        if (profile.Capability != AICapability.Extraction)
        {
            throw new InvalidOperationException($"The profile '{profile.Name}' does not support extraction capability.");
        }
    }
}
