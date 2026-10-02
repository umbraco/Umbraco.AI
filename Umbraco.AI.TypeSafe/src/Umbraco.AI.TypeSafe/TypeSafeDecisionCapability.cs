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
    /// <remarks>
    /// Jev documents no models endpoint to probe, so this validates <paramref name="settings"/> by
    /// sending one tiny authenticated question through a real <see cref="TypeSafeDecisionClient"/>
    /// instead (see <see cref="TypeSafeProvider.EnsureConnectionValidAsync"/>) and only returns the
    /// static <see cref="Models"/> list once that succeeds. Any failure (401, 422, network, etc.)
    /// propagates so callers such as <c>AIConnectionService.TestConnectionAsync</c> — which treats any
    /// exception from <c>GetModelsAsync</c> as a failed "Test connection" — see it as one.
    /// </remarks>
    protected override async Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        TypeSafeProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        using var client = await CreateClientAsync(settings, DefaultModel, cancellationToken);
        await Provider.EnsureConnectionValidAsync(settings, client, cancellationToken);

        return Models;
    }

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
