#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Decision.Spike;

/// <summary>
/// A disposable spike provider proving the experimental <c>AICapability.Decision</c> capability
/// against Jev's real HTTP API — decision-capability plan T10.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is not a real, shippable provider.</b> Per the decision-capability plan's brief, the
/// Decision capability itself is the thing under test, not Jev integration as a product. Real
/// providers live in their own <c>src/</c> project and ship on NuGet with the rest of Umbraco.AI;
/// putting an <c>[AIProvider]</c>-attributed class in <c>Umbraco.AI.Core</c> (or any other <c>src/</c>
/// project) would make it auto-discovered — via <see cref="Umbraco.Cms.Core.Composing.IDiscoverable"/>
/// assembly scanning — in every real Umbraco.AI install, which is the opposite of disposable.
/// </para>
/// <para>
/// Living in <c>Umbraco.AI.Tests.Common</c> instead means this never ships: the assembly is a
/// test-support library, referenced only by <c>Umbraco.AI.Tests.Unit</c>/<c>Umbraco.AI.Tests.Integration</c>,
/// never packed or referenced by a real site. T11's manual, real-network verification step picks up
/// this class the same way <c>Umbraco.AI.Tests.Common</c>'s existing <c>Fake*</c> types are already
/// referenced from test/integration hosts — via an explicit <c>builder.AIProviders().Add&lt;JevSpikeProvider&gt;()</c>
/// call, not auto-discovery. TypeLoader assembly scanning would still pick this class up as an
/// <c>[AIProvider]</c> if a real site ever took a direct <c>ProjectReference</c> on
/// <c>Umbraco.AI.Tests.Common</c> — that reference is what T11 needs to avoid, not something this
/// attribute placement prevents on its own. Doing so would also pull Moq/Shouldly/Microsoft.Data.Sqlite
/// into that site, so T11 should wire this in via a small separate spike host, or explicitly accept the
/// extra test-dependency baggage, rather than referencing this assembly from a demo/production site.
/// </para>
/// </remarks>
[AIProvider("typesafe-jev-spike", "TypeSafe Jev (Spike)")]
public sealed class JevSpikeProvider : AIProviderBase<JevSpikeProviderSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="JevSpikeProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="httpClientFactory">The HTTP client factory, mirroring the sibling providers'
    /// (e.g. <c>FireworksAIProvider</c>, <c>OpenRouterProvider</c>) factory-managed HttpClient pattern.</param>
    public JevSpikeProvider(IAIProviderInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;

        WithCapability<JevSpikeDecisionCapability>();
    }

    /// <summary>
    /// Creates a pooled, factory-managed <see cref="HttpClient"/> for the Jev API. The returned instance
    /// must not be disposed by its caller — its underlying handler is owned and recycled by
    /// <see cref="IHttpClientFactory"/>, not by this provider or its capability.
    /// </summary>
    internal HttpClient CreateHttpClient() => _httpClientFactory.CreateClient();
}
