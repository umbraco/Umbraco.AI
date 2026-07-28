using System.Net;
using System.Text;
using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Umbraco.AI.Anthropic.Tests.Unit.Fakes;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Anthropic.Tests.Unit;

/// <summary>
/// Drives the capability's own settings hook end to end — resolved settings in, request body out.
/// </summary>
/// <remarks>
/// <see cref="AnthropicEffortWireTests"/> asserts what the SDK adapter does with a representation built by
/// the test. These tests assert what the representation <em>the capability builds</em> serialises to, which
/// is a different thing and the gap that let a request go out carrying <c>output_config.task_budget: null</c>
/// — rejected by the API as an extra input, because assigning a sibling property marks it present even when
/// its value is null.
/// </remarks>
public class AnthropicApplyEffortWireTests
{
    private const string ModelsResponse = """
        { "data": [ { "type": "model", "id": "claude-opus-5", "display_name": "Claude Opus 5",
                      "created_at": "2026-06-09T00:00:00Z", "max_input_tokens": 1, "max_tokens": 1,
                      "capabilities": { "effort": { "supported": true } } } ],
          "has_more": false }
        """;

    [Fact]
    public async Task ApplyCapabilitySettings_SendsEffortAndNothingElseInOutputConfig()
    {
        // Arrange
        var handler = new AnthropicApiHandler(ModelsResponse);
        var chatClient = await CreateConfiguredClientAsync(handler, effort: "medium");

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert — exactly one key inside output_config
        var body = handler.ChatRequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"output_config\":{\"effort\":\"medium\"}");
        body.ShouldNotContain("task_budget");
        body.ShouldNotContain("format");
    }

    [Fact]
    public async Task ApplyCapabilitySettings_NoEffortConfigured_SendsNoOutputConfig()
    {
        // Arrange
        var handler = new AnthropicApiHandler(ModelsResponse);
        var chatClient = await CreateConfiguredClientAsync(handler, effort: null);

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert
        var body = handler.ChatRequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("output_config");
    }

    [Fact]
    public async Task ApplyCapabilitySettings_LevelTheModelDoesNotAccept_SendsNoOutputConfig()
    {
        // Arrange — xhigh is not offered, so a value stored by an API caller must not be forwarded
        var handler = new AnthropicApiHandler(ModelsResponse);
        var chatClient = await CreateConfiguredClientAsync(handler, effort: "xhigh");

        // Act
        await SendAndIgnoreFailureAsync(chatClient);

        // Assert
        var body = handler.ChatRequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("output_config");
    }

    [Fact]
    public async Task ModelListUnavailable_StillSendsEffortByInferringFromTheModelId()
    {
        // Arrange — the models call fails, so there are no reported capabilities to consult
        var handler = new AnthropicApiHandler(modelsResponse: null);
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            handler);

        IAIChatCapability capability = new AnthropicChatCapability(provider, logger: null);

        // Act — client creation must not fail, and the ID predicate says Opus 5 accepts effort
        var chatClient = await capability.CreateClientAsync(
            new AnthropicProviderSettings { ApiKey = "test-key" },
            new AnthropicChatCapabilitySettings { Effort = "medium" },
            "claude-opus-5",
            default);

        await SendAndIgnoreFailureAsync(chatClient);

        // Assert
        var body = handler.ChatRequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"output_config\":{\"effort\":\"medium\"}");
    }

    [Fact]
    public async Task ModelListUnavailable_ModelTheIdPredicateRejects_SendsNoOutputConfig()
    {
        // Arrange — the fallback must still refuse a model known not to accept effort
        var handler = new AnthropicApiHandler(modelsResponse: null);
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            handler);

        IAIChatCapability capability = new AnthropicChatCapability(provider, logger: null);

        // Act
        var chatClient = await capability.CreateClientAsync(
            new AnthropicProviderSettings { ApiKey = "test-key" },
            new AnthropicChatCapabilitySettings { Effort = "medium" },
            "claude-haiku-4-5-20251001",
            default);

        await SendAndIgnoreFailureAsync(chatClient);

        // Assert
        var body = handler.ChatRequestBodies.ShouldHaveSingleItem();
        body.ShouldNotContain("output_config");
    }

    private static async Task<IChatClient> CreateConfiguredClientAsync(
        AnthropicApiHandler handler,
        string? effort)
    {
        var provider = new StubbedAnthropicProvider(
            new FakeProviderInfrastructure(),
            new MemoryCache(new MemoryCacheOptions()),
            handler);

        IAIChatCapability capability = new AnthropicChatCapability(provider, logger: null);
        var settings = new AnthropicProviderSettings { ApiKey = "test-key" };
        var capabilitySettings = effort is null ? new AnthropicChatCapabilitySettings() : new() { Effort = effort };

        return await capability.CreateClientAsync(settings, capabilitySettings, "claude-opus-5", default);
    }

    private static async Task SendAndIgnoreFailureAsync(IChatClient chatClient)
    {
        try
        {
            await chatClient.GetResponseAsync("hello", new ChatOptions { MaxOutputTokens = 64 });
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only the captured body matters here.
        }
    }

    [AIProvider("anthropic", "Anthropic")]
    private sealed class StubbedAnthropicProvider(
        IAIProviderInfrastructure infrastructure,
        IMemoryCache cache,
        HttpMessageHandler handler)
        : AnthropicProvider(infrastructure, cache)
    {
        internal override AnthropicClient CreateSdkClient(AnthropicProviderSettings settings)
            => new()
            {
                ApiKey = "test-key",
                MaxRetries = 0,
                HttpClient = new HttpClient(handler),
            };
    }

    /// <summary>
    /// Stands in for the Anthropic API: serves the models call from a canned response (or fails it, when
    /// none is given) and captures chat request bodies.
    /// </summary>
    private sealed class AnthropicApiHandler(string? modelsResponse) : HttpMessageHandler
    {
        public List<string> ChatRequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri?.AbsolutePath.Contains("/models", StringComparison.Ordinal) == true)
            {
                return modelsResponse is null
                    ? throw new HttpRequestException("models endpoint unreachable")
                    : new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(modelsResponse, Encoding.UTF8, "application/json"),
                    };
            }

            if (request.Content is not null)
            {
                ChatRequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"type":"error","error":{"type":"invalid_request_error","message":"captured"}}"""),
            };
        }
    }
}
