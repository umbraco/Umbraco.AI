using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Umbraco.Ai.Core.Factories;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Profiles;

namespace Umbraco.Ai.Core.Services;

internal sealed class AiEmbeddingService : IAiEmbeddingService
{
    private readonly IAiEmbeddingGeneratorFactory _generatorFactory;
    private readonly IAiProfileService _profileService;
    private readonly AiOptions _options;

    public AiEmbeddingService(
        IAiEmbeddingGeneratorFactory generatorFactory,
        IAiProfileService profileService,
        IOptionsMonitor<AiOptions> options)
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
        var profile = await _profileService.GetDefaultProfileAsync(AiCapability.Embedding, cancellationToken);
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
        AiProfile profile,
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
        var profile = await _profileService.GetDefaultProfileAsync(AiCapability.Embedding, cancellationToken);
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
        AiProfile profile,
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
            : await _profileService.GetDefaultProfileAsync(AiCapability.Embedding, cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException($"AI profile with ID '{profileId}' not found.");
        }

        EnsureProfileSupportsEmbedding(profile);

        return await _generatorFactory.CreateGeneratorAsync(profile, cancellationToken);
    }

    private static EmbeddingGenerationOptions? MergeOptions(AiProfile profile, EmbeddingGenerationOptions? callerOptions)
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

    private static void EnsureProfileSupportsEmbedding(AiProfile profile)
    {
        if (profile.Capability != AiCapability.Embedding)
        {
            throw new InvalidOperationException($"The profile '{profile.Name}' does not support embedding capability.");
        }
    }
}
