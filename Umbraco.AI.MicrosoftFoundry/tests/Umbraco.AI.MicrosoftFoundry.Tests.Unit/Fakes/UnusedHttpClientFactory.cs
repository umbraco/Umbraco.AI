namespace Umbraco.AI.MicrosoftFoundry.Tests.Unit.Fakes;

/// <summary>
/// Satisfies the provider's constructor for tests that never list models. Throwing rather than returning a
/// real client keeps an accidental network call from turning into a slow or flaky test.
/// </summary>
internal sealed class UnusedHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
        => throw new NotSupportedException("These tests do not call the Foundry APIs.");
}
