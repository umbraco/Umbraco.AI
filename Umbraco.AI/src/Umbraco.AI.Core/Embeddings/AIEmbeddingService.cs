using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;

namespace Umbraco.AI.Core.Embeddings;

internal sealed class AIEmbeddingService : IAIEmbeddingService
{
    private readonly IAIEmbeddingGeneratorFactory _generatorFactory;
    private readonly IAIProfileService _profileService;
    private readonly AIOptions _options;

    public AIEmbeddingService(
        IAIEmbeddingGeneratorFactory generatorFactory,
        IAIProfileService profileService,
        IOptionsMonitor<AIOptions> options)
    {
        _generatorFactory = generatorFactory;
        _profileService = profileService;
        _options = options.CurrentValue;
    }

    public async Task<Embedding<float>> GenerateEmbeddingAsync(
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetDefaultProfileAsync(AICapability.Embedding, cancellationToken);
        return await GenerateEmbeddingInternalAsync(profile, value, options, cancellationToken);
    }

    public async Task<Embedding<float>> GenerateEmbeddingAsync(
        Guid profileId,
        string value,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileId, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsEmbedding(profile);

        return await GenerateEmbeddingInternalAsync(profile, value, options, cancellationToken);
    }

    private async Task<Embedding<float>> GenerateEmbeddingInternalAsync(
        AIProfile profile,
        string value,
        EmbeddingGenerationOptions? options,
        CancellationToken cancellationToken)
    {
        var generator = await _generatorFactory.CreateGeneratorAsync(profile, cancellationToken);
        var mergedOptions = MergeOptions(profile, options);

        var result = await generator.GenerateAsync([value], mergedOptions, cancellationToken);
        return result[0];
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetDefaultProfileAsync(AICapability.Embedding, cancellationToken);
        return await GenerateEmbeddingsInternalAsync(profile, values, options, cancellationToken);
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsAsync(
        Guid profileId,
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var profile = await _profileService.GetProfileAsync(profileId, cancellationToken);
        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsEmbedding(profile);

        return await GenerateEmbeddingsInternalAsync(profile, values, options, cancellationToken);
    }

    private async Task<GeneratedEmbeddings<Embedding<float>>> GenerateEmbeddingsInternalAsync(
        AIProfile profile,
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options,
        CancellationToken cancellationToken)
    {
        var generator = await _generatorFactory.CreateGeneratorAsync(profile, cancellationToken);
        var mergedOptions = MergeOptions(profile, options);

        return await generator.GenerateAsync(values, mergedOptions, cancellationToken);
    }

    public async Task<IEmbeddingGenerator<string, Embedding<float>>> GetEmbeddingGeneratorAsync(
        Guid? profileId = null,
        CancellationToken cancellationToken = default)
    {
        var profile = profileId.HasValue
            ? await _profileService.GetProfileAsync(profileId.Value, cancellationToken)
            : await _profileService.GetDefaultProfileAsync(AICapability.Embedding, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsEmbedding(profile);

        return await _generatorFactory.CreateGeneratorAsync(profile, cancellationToken);
    }

    private static EmbeddingGenerationOptions? MergeOptions(AIProfile profile, EmbeddingGenerationOptions? callerOptions)
    {
        // If caller provides options, merge with profile defaults
        // Caller options take precedence over profile settings
        if (callerOptions != null)
        {
            return new EmbeddingGenerationOptions
            {
                ModelId = callerOptions.ModelId ?? profile.Model.ModelId,
                Dimensions = callerOptions.Dimensions,
                AdditionalProperties = callerOptions.AdditionalProperties
            };
        }

        // No caller options, use profile defaults
        return new EmbeddingGenerationOptions
        {
            ModelId = profile.Model.ModelId
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
