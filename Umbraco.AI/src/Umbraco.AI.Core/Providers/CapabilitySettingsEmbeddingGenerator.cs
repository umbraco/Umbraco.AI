using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Embedding generator decorator that applies provider-declared capability settings onto each request's
/// <see cref="EmbeddingGenerationOptions"/> before delegating to the inner generator.
/// </summary>
/// <remarks>
/// Created by <see cref="AIEmbeddingCapabilityBase{TSettings, TCapabilitySettings}"/> with the resolved,
/// typed capability settings baked in. The caller's <see cref="EmbeddingGenerationOptions"/> instance is
/// never mutated; a per-request copy is used.
/// </remarks>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type.</typeparam>
internal sealed class CapabilitySettingsEmbeddingGenerator<TCapabilitySettings>
    : DelegatingEmbeddingGenerator<string, Embedding<float>>
    where TCapabilitySettings : class
{
    private readonly TCapabilitySettings _capabilitySettings;
    private readonly string? _boundModelId;
    private readonly Action<TCapabilitySettings, string?, EmbeddingGenerationOptions> _apply;

    public CapabilitySettingsEmbeddingGenerator(
        IEmbeddingGenerator<string, Embedding<float>> innerGenerator,
        TCapabilitySettings capabilitySettings,
        string? boundModelId,
        Action<TCapabilitySettings, string?, EmbeddingGenerationOptions> apply)
        : base(innerGenerator)
    {
        _capabilitySettings = capabilitySettings;
        _boundModelId = boundModelId;
        _apply = apply;
    }

    /// <inheritdoc />
    public override Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GenerateAsync(values, Apply(options), cancellationToken);

    private EmbeddingGenerationOptions Apply(EmbeddingGenerationOptions? options)
    {
        // Clone so the caller's options instance is never mutated.
        var effective = options?.Clone() ?? new EmbeddingGenerationOptions();

        // Resolve the model the request will actually run against so the provider can gate settings the
        // model rejects, falling back to the model the generator was created for.
        _apply(_capabilitySettings, effective.ModelId ?? _boundModelId, effective);
        return effective;
    }
}
