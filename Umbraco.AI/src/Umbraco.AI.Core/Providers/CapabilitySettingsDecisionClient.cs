using Umbraco.AI.Core.Decision;

#pragma warning disable UMBRACOAI_DECISION // IAIDecisionClient is experimental

namespace Umbraco.AI.Core.Providers;

/// <summary>
/// Decision client decorator that applies provider-declared capability settings onto each request's
/// <see cref="AIDecisionOptions"/> before delegating to the inner client.
/// </summary>
/// <remarks>
/// Created by <see cref="AIDecisionCapabilityBase{TSettings, TCapabilitySettings}"/> with the resolved,
/// typed capability settings baked in. The caller's <see cref="AIDecisionOptions"/> instance is never
/// mutated; a per-request copy is used.
/// </remarks>
/// <typeparam name="TCapabilitySettings">The provider-declared capability settings type.</typeparam>
internal sealed class CapabilitySettingsDecisionClient<TCapabilitySettings> : IAIDecisionClient
    where TCapabilitySettings : class
{
    private readonly IAIDecisionClient _innerClient;
    private readonly TCapabilitySettings _capabilitySettings;
    private readonly string? _boundModelId;
    private readonly Action<TCapabilitySettings, string?, AIDecisionOptions> _apply;

    public CapabilitySettingsDecisionClient(
        IAIDecisionClient innerClient,
        TCapabilitySettings capabilitySettings,
        string? boundModelId,
        Action<TCapabilitySettings, string?, AIDecisionOptions> apply)
    {
        _innerClient = innerClient;
        _capabilitySettings = capabilitySettings;
        _boundModelId = boundModelId;
        _apply = apply;
    }

    /// <inheritdoc />
    public Task<AIDecisionResponse> AskAsync(
        AIDecisionQuestion question,
        AIDecisionOptions? options = null,
        CancellationToken cancellationToken = default)
        => _innerClient.AskAsync(question, Apply(options), cancellationToken);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => _innerClient.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public void Dispose() => _innerClient.Dispose();

    private AIDecisionOptions Apply(AIDecisionOptions? options)
    {
        // Clone so the caller's options instance is never mutated.
        var effective = options?.Clone() ?? new AIDecisionOptions();

        // Resolve the model the request will actually run against so the provider can gate settings the
        // model rejects, falling back to the model the client was created for.
        _apply(_capabilitySettings, effective.ModelId ?? _boundModelId, effective);
        return effective;
    }
}
