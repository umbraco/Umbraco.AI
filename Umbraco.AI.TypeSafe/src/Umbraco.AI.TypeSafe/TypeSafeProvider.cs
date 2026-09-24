using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.TypeSafe;

/// <summary>
/// AI provider for TypeSafe AI (Jev), an experimental typed-decision model.
/// </summary>
/// <remarks>
/// Exposes only the experimental Decision capability (<see cref="TypeSafeDecisionCapability"/>) — Jev
/// has no chat or embedding models, and answers a single typed yes/no, choice, or score question per
/// call. Calls Jev directly over HTTP (<see cref="IHttpClientFactory"/> + <c>System.Text.Json</c>),
/// like <c>Umbraco.AI.FireworksAI</c>, since no official .NET SDK exists for this vendor.
/// </remarks>
[AIProvider("typesafe", "TypeSafe AI")]
public class TypeSafeProvider : AIProviderBase<TypeSafeProviderSettings>
{
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypeSafeProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="httpClientFactory">Used to create the <see cref="HttpClient"/> the decision client sends requests with.</param>
    public TypeSafeProvider(IAIProviderInfrastructure infrastructure, IHttpClientFactory httpClientFactory)
        : base(infrastructure)
    {
        _httpClientFactory = httpClientFactory;

#pragma warning disable UMBRACOAI_DECISION // Registers the experimental decision capability
        WithCapability<TypeSafeDecisionCapability>();
#pragma warning restore UMBRACOAI_DECISION
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> for calling the TypeSafe AI API.
    /// </summary>
    /// <remarks>
    /// The client carries no preconfigured base address or auth header — <see cref="TypeSafeDecisionClient"/>
    /// builds each request from <see cref="TypeSafeProviderSettings"/> directly, the same way
    /// <c>FireworksAIProvider</c> builds its native-models request per call.
    /// </remarks>
    internal HttpClient CreateHttpClient() => _httpClientFactory.CreateClient(nameof(TypeSafeProvider));
}
