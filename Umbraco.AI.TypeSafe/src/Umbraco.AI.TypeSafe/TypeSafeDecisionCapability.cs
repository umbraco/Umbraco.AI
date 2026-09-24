#pragma warning disable UMBRACOAI_DECISION // Implements the experimental Umbraco.AI decision capability

using System.Diagnostics.CodeAnalysis;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.TypeSafe;

/// <summary>
/// AI decision capability for TypeSafe AI (Jev).
/// </summary>
/// <remarks>
/// Experimental — gated by the <c>Umbraco:AI:Experimental:Decision</c> feature flag and the
/// <c>UMBRACOAI_DECISION</c> diagnostic. Jev exposes a single model (<c>jev-latest</c>) and documents no
/// models endpoint, so the model list is a static array rather than a live lookup.
/// </remarks>
[Experimental(AIDecisionDiagnostics.DiagnosticId)]
public class TypeSafeDecisionCapability(TypeSafeProvider provider)
    : AIDecisionCapabilityBase<TypeSafeProviderSettings>(provider)
{
    private const string DefaultModel = "jev-latest";

    private static readonly IReadOnlyList<AIModelDescriptor> Models =
    [
        new AIModelDescriptor(new AIModelRef("typesafe", DefaultModel), "Jev (latest)"),
    ];

    private new TypeSafeProvider Provider => (TypeSafeProvider)base.Provider;

    /// <inheritdoc />
    protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        TypeSafeProviderSettings settings,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Models);

    /// <inheritdoc />
    /// <remarks>
    /// Validates the API key up front (mirroring <c>OpenAIProvider.ValidateSettings</c>) so a missing key
    /// fails before any network call rather than surfacing as an opaque request failure.
    /// </remarks>
    protected override Task<IAIDecisionClient> CreateClientAsync(
        TypeSafeProviderSettings settings,
        string? modelId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            throw new InvalidOperationException("TypeSafe AI API key is required.");
        }

        var httpClient = Provider.CreateHttpClient();

        IAIDecisionClient client = new TypeSafeDecisionClient(
            httpClient,
            settings,
            modelId ?? DefaultModel,
            static (delay, ct) => Task.Delay(delay, ct));

        return Task.FromResult(client);
    }
}
