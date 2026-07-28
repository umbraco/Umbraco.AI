using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.ImageGeneration;

#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Image generator decorator that applies provider-declared capability settings onto each request's
/// <see cref="ImageGenerationOptions"/> before delegating to the inner generator.
/// </summary>
/// <remarks>
/// Created by <see cref="AIImageGeneratorCapabilityBase{TSettings, TCapabilitySettings}"/> with the
/// resolved, typed capability settings baked in. The caller's <see cref="ImageGenerationOptions"/> instance
/// is never mutated; a per-request copy is used.
/// </remarks>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type.</typeparam>
[Experimental(AIImageGenerationDiagnostics.DiagnosticId)]
internal sealed class CapabilitySettingsImageGenerator<TCapabilitySettings> : DelegatingImageGenerator
    where TCapabilitySettings : class
{
    private readonly TCapabilitySettings _capabilitySettings;
    private readonly string? _boundModelId;
    private readonly Action<TCapabilitySettings, string?, ImageGenerationOptions> _apply;

    public CapabilitySettingsImageGenerator(
        IImageGenerator innerGenerator,
        TCapabilitySettings capabilitySettings,
        string? boundModelId,
        Action<TCapabilitySettings, string?, ImageGenerationOptions> apply)
        : base(innerGenerator)
    {
        _capabilitySettings = capabilitySettings;
        _boundModelId = boundModelId;
        _apply = apply;
    }

    /// <inheritdoc />
    public override Task<ImageGenerationResponse> GenerateAsync(
        ImageGenerationRequest request,
        ImageGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
        => base.GenerateAsync(request, Apply(options), cancellationToken);

    private ImageGenerationOptions Apply(ImageGenerationOptions? options)
    {
        // Clone so the caller's options instance is never mutated.
        var effective = options?.Clone() ?? new ImageGenerationOptions();

        // Resolve the model the request will actually run against so the provider can gate settings the
        // model rejects, falling back to the model the generator was created for.
        _apply(_capabilitySettings, effective.ModelId ?? _boundModelId, effective);
        return effective;
    }
}
