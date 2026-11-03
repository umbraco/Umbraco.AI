using Umbraco.AI.Core.Extraction;

#pragma warning disable UMBRACOAI_EXTRACTION // IAIExtractionClient is experimental

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Extraction client decorator that applies provider-declared capability settings onto each request's
/// <see cref="AIExtractionOptions"/> before delegating to the inner client.
/// </summary>
/// <remarks>
/// Created by <see cref="AIExtractionCapabilityBase{TSettings, TCapabilitySettings}"/> with the resolved,
/// typed capability settings baked in. The caller's <see cref="AIExtractionOptions"/> instance is never
/// mutated; a per-request copy is used.
/// </remarks>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type.</typeparam>
internal sealed class CapabilitySettingsExtractionClient<TCapabilitySettings> : IAIExtractionClient
    where TCapabilitySettings : class
{
    private readonly IAIExtractionClient _innerClient;
    private readonly TCapabilitySettings _capabilitySettings;
    private readonly string? _boundModelId;
    private readonly Action<TCapabilitySettings, string?, AIExtractionOptions> _apply;

    public CapabilitySettingsExtractionClient(
        IAIExtractionClient innerClient,
        TCapabilitySettings capabilitySettings,
        string? boundModelId,
        Action<TCapabilitySettings, string?, AIExtractionOptions> apply)
    {
        _innerClient = innerClient;
        _capabilitySettings = capabilitySettings;
        _boundModelId = boundModelId;
        _apply = apply;
    }

    /// <inheritdoc />
    public Task<AIExtractionResponse> GetResponseAsync(
        AIExtractionRequest request,
        AIExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
        => _innerClient.GetResponseAsync(request, Apply(options), cancellationToken);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => _innerClient.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public void Dispose() => _innerClient.Dispose();

    private AIExtractionOptions Apply(AIExtractionOptions? options)
    {
        // Clone so the caller's options instance is never mutated.
        var effective = options?.Clone() ?? new AIExtractionOptions();

        // Resolve the model the request will actually run against so the provider can gate settings the
        // model rejects, falling back to the model the client was created for.
        _apply(_capabilitySettings, effective.ModelId ?? _boundModelId, effective);
        return effective;
    }
}
