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

    public Task<TResponse> AskAsync<TResponse>(
        AIDecisionQuestion<TResponse> question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TResponse : AIDecisionResponse
        => AskAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias: null, options),
            question, cancellationToken);

    public Task<TResponse> AskAsync<TResponse>(
        Guid profileId,
        AIDecisionQuestion<TResponse> question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TResponse : AIDecisionResponse
        => AskAsync(
            b => ConfigureFromProfileOverload(b, profileId, profileAlias: null, options),
            question, cancellationToken);

    public Task<TResponse> AskAsync<TResponse>(
        string profileAlias,
        AIDecisionQuestion<TResponse> question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        where TResponse : AIDecisionResponse
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileAlias);

        return AskAsync(
            b => ConfigureFromProfileOverload(b, profileId: null, profileAlias, options),
            question, cancellationToken);
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

    public async Task<TResponse> AskAsync<TResponse>(
        Action<AIDecisionBuilder> configure,
        AIDecisionQuestion<TResponse> question,
        CancellationToken cancellationToken = default)
        where TResponse : AIDecisionResponse
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(question);

        var builder = BuildDecision(configure);

        // Pass-through mode: skip notifications and duration tracking.
        // The parent feature handles its own observability.
        if (builder.IsPassThrough)
        {
            return await ExecuteDecisionAsync(builder, question, cancellationToken);
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
            var response = await ExecuteDecisionAsync(builder, question, cancellationToken);
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

    private async Task<TResponse> ExecuteDecisionAsync<TResponse>(
        AIDecisionBuilder builder,
        AIDecisionQuestion<TResponse> question,
        CancellationToken cancellationToken)
        where TResponse : AIDecisionResponse
    {
        var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);
        var innerClient = await _clientFactory.CreateClientAsync(profile, cancellationToken);

        // Wrap in ScopedInlineDecisionClient for per-call scope management and inline decision metadata.
        var client = new ScopedInlineDecisionClient(innerClient, builder, _contextAccessor, _scopeProvider, _contributors);
        var response = await client.AskAsync(question, builder.Options, cancellationToken);

        // IAIDecisionClient stays non-generic (see its remarks), so nothing before this point knows
        // TResponse. The real check for a provider answering the wrong question shape already happened
        // inside AIErrorClassifyingDecisionClient (see its remarks) — which sits inside the tracking
        // middleware, so a mismatch is recorded as a tracked/audited failure, not a false success. This
        // cast is only a defence-in-depth guard; it should never trip in practice.
        if (response is not TResponse typedResponse)
        {
            throw AIDecisionExceptionFactory.CreateResponseTypeMismatchException(typeof(TResponse), response);
        }

        return typedResponse;
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
