using System.Diagnostics;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Events;

namespace Umbraco.AI.Core.Embeddings;

internal sealed class AIEmbeddingService : IAIEmbeddingService
{
    private readonly IAIEmbeddingGeneratorFactory _generatorFactory;
    private readonly IAIProfileService _profileService;
    private readonly IAIGuardrailService _guardrailService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IAIRuntimeContextAccessor _contextAccessor;
    private readonly IAIRuntimeContextScopeProvider _scopeProvider;
    private readonly AIRuntimeContextContributorCollection _contributors;

    public AIEmbeddingService(
        IAIEmbeddingGeneratorFactory generatorFactory,
        IAIProfileService profileService,
        IAIGuardrailService guardrailService,
        IEventAggregator eventAggregator,
        IAIRuntimeContextAccessor contextAccessor,
        IAIRuntimeContextScopeProvider scopeProvider,
        AIRuntimeContextContributorCollection contributors)
    {
        _generatorFactory = generatorFactory;
        _profileService = profileService;
        _guardrailService = guardrailService;
        _eventAggregator = eventAggregator;
        _contextAccessor = contextAccessor;
        _scopeProvider = scopeProvider;
        _contributors = contributors;
    }

    #pragma warning disable CS0618 // Obsolete members - implementing the deprecated interface methods

    public Task<Embedding<float>> GenerateEmbeddingAsync(
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => GenerateEmbeddingAsync(
            b => ConfigureLegacy(b, profileId: null, options),
            value, cancellationToken);

    public Task<Embedding<float>> GenerateEmbeddingAsync(
        Guid profileId,
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => GenerateEmbeddingAsync(
            b => ConfigureLegacy(b, profileId, options),
            value, cancellationToken);

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => GenerateEmbeddingsAsync(
            b => ConfigureLegacy(b, profileId: null, options),
            values, cancellationToken);

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        Guid profileId,
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => GenerateEmbeddingsAsync(
            b => ConfigureLegacy(b, profileId, options),
            values, cancellationToken);

    public Task<IEmbeddingGenerator<string, Embedding<float>>> GetEmbeddingGeneratorAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
        => CreateEmbeddingGeneratorAsync(
            b => ConfigureLegacy(b, profileId, options: null),
            cancellationToken);

    #pragma warning restore CS0618

    private static void ConfigureLegacy(AIEmbeddingBuilder builder, Guid? profileId, EmbeddingGenerationOptions? options)
    {
        builder.WithAlias("embedding");
        if (profileId.HasValue)
        {
            builder.WithProfile(profileId.Value);
        }
        if (options is not null)
        {
            builder.WithEmbeddingOptions(options);
        }
    }

    public async Task<Embedding<float>> GenerateEmbeddingAsync(
        Action<AIEmbeddingBuilder> configure,
        string value,
        CancellationToken cancellationToken = default)
    {
        var result = await GenerateEmbeddingsAsync(configure, [value], cancellationToken);
        return result[0];
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        Action<AIEmbeddingBuilder> configure,
        IEnumerable<string> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        ArgumentNullException.ThrowIfNull(values);

        var builder = BuildEmbedding(configure);

        if (builder.IsPassThrough)
        {
            return await ExecuteEmbeddingAsync(builder, values, cancellationToken);
        }

        var eventMessages = new EventMessages();
        var executingNotification = new AIEmbeddingExecutingNotification(
            builder.Id, builder.Alias!, builder.Name, builder.ProfileId, eventMessages);
        await _eventAggregator.PublishAsync(executingNotification, cancellationToken);

        if (executingNotification.Cancel)
        {
            var errorMessages = string.Join("; ", eventMessages.GetAll().Select(m => m.Message));
            throw new InvalidOperationException($"Inline embedding execution cancelled: {errorMessages}");
        }

        var stopwatch = Stopwatch.StartNew();
        bool isSuccess = false;

        try
        {
            var response = await ExecuteEmbeddingAsync(builder, values, cancellationToken);
            isSuccess = true;
            return response;
        }
        finally
        {
            var executedNotification = new AIEmbeddingExecutedNotification(
                builder.Id, builder.Alias!, builder.Name, builder.ProfileId,
                stopwatch.Elapsed, isSuccess, eventMessages);
            await _eventAggregator.PublishAsync(executedNotification, cancellationToken);
        }
    }

    public async Task<IEmbeddingGenerator<string, Embedding<float>>> CreateEmbeddingGeneratorAsync(
        Action<AIEmbeddingBuilder> configure,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);

        var builder = BuildEmbedding(configure);

        await ResolveBuilderAliasesAsync(builder, cancellationToken);
        var profile = await ResolveProfileAsync(builder.ProfileId, builder.ProfileAlias, cancellationToken);

        var generator = await _generatorFactory.CreateGeneratorAsync(profile, cancellationToken);

        return new ScopedInlineEmbeddingGenerator(generator, builder, _contextAccessor, _scopeProvider, _contributors);
    }

    private async Task<GeneratedEmbeddings<Embedding<float>>> ExecuteEmbeddingAsync(
        AIEmbeddingBuilder builder,
        IEnumerable<string> values,
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
            var generator = await _generatorFactory.CreateGeneratorAsync(profile, cancellationToken);
            var mergedOptions = MergeOptions(profile, builder.EmbeddingOptions);

            return await generator.GenerateAsync(values, mergedOptions, cancellationToken);
        }
        finally
        {
            createdScope?.Dispose();
        }
    }

    private static AIEmbeddingBuilder BuildEmbedding(Action<AIEmbeddingBuilder> configure)
    {
        var builder = new AIEmbeddingBuilder();
        configure(builder);
        builder.Validate();
        return builder;
    }

    private async Task<AIProfile> ResolveProfileAsync(Guid? profileId, string? profileAlias, CancellationToken cancellationToken)
    {
        if (!profileId.HasValue && !string.IsNullOrWhiteSpace(profileAlias))
        {
            profileId = await _profileService.GetProfileIdByAliasAsync(profileAlias, cancellationToken);
        }

        var profile = profileId.HasValue
            ? await _profileService.GetProfileAsync(profileId.Value, cancellationToken)
            : await _profileService.GetDefaultProfileAsync(AICapability.Embedding, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsEmbedding(profile);
        return profile;
    }

    private async Task ResolveBuilderAliasesAsync(AIEmbeddingBuilder builder, CancellationToken cancellationToken)
    {
        if (builder.GuardrailAliases is { Count: > 0 } aliases)
        {
            builder.SetResolvedGuardrailIds(
                await _guardrailService.GetGuardrailIdsByAliasesAsync(aliases, cancellationToken));
        }
    }

    private static EmbeddingGenerationOptions? MergeOptions(AIProfile profile, EmbeddingGenerationOptions? callerOptions)
    {
        var profileDimensions = (profile.Settings as AIEmbeddingProfileSettings)?.Dimensions;

        if (callerOptions != null)
        {
            return new EmbeddingGenerationOptions
            {
                ModelId = callerOptions.ModelId ?? profile.Model.ModelId,
                Dimensions = callerOptions.Dimensions ?? profileDimensions,
                AdditionalProperties = callerOptions.AdditionalProperties
            };
        }

        return new EmbeddingGenerationOptions
        {
            ModelId = profile.Model.ModelId,
            Dimensions = profileDimensions
        };
    }

    private static void EnsureProfileSupportsEmbedding(AIProfile profile)
    {
        if (profile.Capability != AICapability.Embedding)
        {
            throw new InvalidOperationException($"The profile '{profile.Name}' does not support embedding capability.");
        }
    }
}
