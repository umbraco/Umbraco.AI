using System.Diagnostics;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Events;

#pragma warning disable UMBRACOAI_DECISION // Implements the experimental decision capability surface

namespace Umbraco.AI.Core.Decision;

internal sealed class AIDecisionService : IAIDecisionService
{
    private readonly IAIProfileService _profileService;
    private readonly IAIDecisionClientFactory _clientFactory;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    public AIDecisionService(
        IAIProfileService profileService,
        IAIDecisionClientFactory clientFactory,
        IEventAggregator eventAggregator,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
    {
        _profileService = profileService;
        _clientFactory = clientFactory;
        _eventAggregator = eventAggregator;
        _contextAccessor = contextAccessor;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
    }

    public Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(
        AIDecisionQuestion<TAnswer> question,
        string? state = null,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIDecisionAnswer
        => AskAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias: null, options),
            question, state, cancellationToken);

    public Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(
        Guid profileId,
        AIDecisionQuestion<TAnswer> question,
        string? state = null,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIDecisionAnswer
        => AskAsync(
            b => ConfigureFromProfileOverload(b, profileId, profileAlias: null, options),
            question, state, cancellationToken);

    public Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(
        string profileAlias,
        AIDecisionQuestion<TAnswer> question,
        string? state = null,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIDecisionAnswer
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileAlias);

        return AskAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias, options),
            question, state, cancellationToken);
    }

    public async Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(
        Action<AIDecisionBuilder> configure,
        AIDecisionQuestion<TAnswer> question,
        string? state = null,
        CancellationToken cancellationToken = default)
        where TAnswer : AIDecisionAnswer
    {
        ArgumentNullException.ThrowIfNull(question);

        var questionWithId = EnsureId(question);
        var request = new AIDecisionRequest { State = state, Questions = [questionWithId] };

        var response = await GetDecisionResponseAsync(configure, request, cancellationToken);

        return ToTypedResponse<TAnswer>(questionWithId, response);
    }

    public Task<AIDecisionResponse> GetDecisionResponseAsync(
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetDecisionResponseAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias: null, options),
            request, cancellationToken);

    public Task<AIDecisionResponse> GetDecisionResponseAsync(
        Guid profileId,
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetDecisionResponseAsync(
            b => ConfigureFromProfileOverload(b, profileId, profileAlias: null, options),
            request, cancellationToken);

    public Task<AIDecisionResponse> GetDecisionResponseAsync(
        string profileAlias,
        AIDecisionRequest request,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileAlias);

        return GetDecisionResponseAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias, options),
            request, cancellationToken);
    }

    public async Task<AIDecisionResponse> GetDecisionResponseAsync(
        Action<AIDecisionBuilder> configure,
        AIDecisionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(request);

        var builder = BuildDecision(configure);

        // Pass-through mode: skip notifications and duration tracking.
        // The parent feature handles its own observability.
        if (builder.IsPassThrough)
        {
            return await ExecuteDecisionAsync(builder, request, cancellationToken);
        }

        // Publish executing notification
        var eventMessages = new EventMessages();
        var executingNotification = new AIDecisionExecutingNotification(
            builder.Id, builder.Alias!, builder.Name, builder.ProfileId, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline decision execution cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        var isSuccess = false;

        try
        {
            var response = await ExecuteDecisionAsync(builder, request, cancellationToken);
            isSuccess = true;
            return response;
        }
        finally
        {
            var executedNotification = new AIDecisionExecutedNotification(
                builder.Id, builder.Alias!, builder.Name, builder.ProfileId,
                stopwatch.Elapsed, isSuccess, eventMessages);
            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    private static void ConfigureFromProfileOverload(AIDecisionBuilder builder, Guid? profileId, string? profileAlias, AIDecisionOptions? options)
    {
        builder.WithAlias("decision");
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
            builder.WithDecisionOptions(options);
        }
    }

    private async Task<AIDecisionResponse> ExecuteDecisionAsync(
        AIDecisionBuilder builder,
        AIDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);
        var innerClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);

        // Wrap in ScopedInlineDecisionClient for per-call scope management and inline decision metadata.
        var client = new ScopedInlineDecisionClient(innerClient, builder, _contextAccessor, _scopeProvider, _contributors);
        return await client.GetResponseAsync(request, builder.Options, cancellationToken);
    }

    /// <summary>
    /// Unwraps the single answer <paramref name="questionWithId"/>'s id was answered with. <see cref="IAIDecisionClient"/>
    /// stays non-generic (see its remarks), so nothing before this point knows <typeparamref name="TAnswer"/>.
    /// The real check for a provider answering the wrong question shape already happened inside
    /// <see cref="AIErrorClassifyingDecisionClient"/> (see its remarks) — which sits inside the tracking
    /// middleware, so a mismatch is recorded as a tracked/audited failure, not a false success. The casts
    /// below are only a defence-in-depth guard; they should never trip in practice.
    /// </summary>
    private static AIDecisionResponse<TAnswer> ToTypedResponse<TAnswer>(AIDecisionQuestion questionWithId, AIDecisionResponse response)
        where TAnswer : AIDecisionAnswer
    {
        if (!response.Answers.TryGetValue(questionWithId.Id!, out var answer))
        {
            throw AIDecisionExceptionFactory.CreateMissingAnswerException(questionWithId);
        }

        if (answer is not TAnswer typedAnswer)
        {
            throw AIDecisionExceptionFactory.CreateAnswerTypeMismatchException(typeof(TAnswer), answer);
        }

        return new AIDecisionResponse<TAnswer>
        {
            Answer = typedAnswer,
            Answers = response.Answers,
            ModelId = response.ModelId,
            Usage = response.Usage,
            RawRepresentation = response.RawRepresentation,
        };
    }

    /// <summary>
    /// Returns <paramref name="question"/> unchanged when it already carries an <see cref="AIDecisionQuestion.Id"/>,
    /// or a shallow copy with a freshly generated one otherwise (via <see cref="AIDecisionQuestion.WithId"/>,
    /// which preserves every subclass's own properties) — <c>AskAsync</c>'s single-question convenience
    /// over the id'd batch pipeline (see ARCHITECTURE.md's "Core types").
    /// </summary>
    private static AIDecisionQuestion<TAnswer> EnsureId<TAnswer>(AIDecisionQuestion<TAnswer> question)
        where TAnswer : AIDecisionAnswer
    {
        if (question.Id is not null)
        {
            return question;
        }

        return (AIDecisionQuestion<TAnswer>)question.WithId(Guid.NewGuid().ToString("N"));
    }

    private static AIDecisionBuilder BuildDecision(Action<AIDecisionBuilder> configure)
    {
        var builder = new AIDecisionBuilder();
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
            : await _profileService.GetDefaultProfileAsync(AICapability.Decision, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsDecision(profile);
        return profile;
    }

    private static void EnsureProfileSupportsDecision(AIProfile profile)
    {
        if (profile.Capability != AICapability.Decision)
        {
            throw new InvalidOperationException($"The profile '{profile.Name}' does not support decision capability.");
        }
    }
}
