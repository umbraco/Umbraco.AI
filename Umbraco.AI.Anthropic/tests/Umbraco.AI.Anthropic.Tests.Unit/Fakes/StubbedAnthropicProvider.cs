using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit.Fakes;

/// <summary>
/// A real provider with its SDK client pointed at a supplied handler, so the SDK's own serialization and
/// the capability's real wiring are exercised against canned responses.
/// </summary>
/// <remarks>
/// Shared so a test can build a client the way production does — through
/// <c>IAIChatCapability.CreateClientAsync</c>, which is where the base installs the declaration filter —
/// rather than assembling the decorators by hand and proving only that the test knows the wiring.
/// </remarks>
[AIProvider("anthropic", "Anthropic")]
internal sealed class StubbedAnthropicProvider(
    IAIProviderInfrastructure infrastructure,
    IMemoryCache cache,
    HttpMessageHandler handler)
    : AnthropicProvider(infrastructure, cache)
{
    internal override global::Anthropic.AnthropicClient CreateSdkClient(AnthropicProviderSettings settings)
        => new()
        {
            ApiKey = "test-key",
            MaxRetries = 0,
            HttpClient = new HttpClient(handler),
        };
}
