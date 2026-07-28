using System.ClientModel;
using System.ClientModel.Primitives;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using Umbraco.AI.OpenAI.Tests.Unit.Fakes;

namespace Umbraco.AI.OpenAI.Tests.Unit;

/// <summary>
/// Pins down how the reasoning effort reaches the OpenAI wire, asserted against a captured request body
/// rather than a live call.
/// </summary>
/// <remarks>
/// The capability sets it through <see cref="ChatOptions.RawRepresentationFactory"/> on an otherwise empty
/// <see cref="CreateResponseOptions"/>, which only works because the Microsoft.Extensions.AI adapter fills
/// the rest of the request (model, input, token limits) around it. If that changes, this fails rather than
/// the setting silently going missing — or worse, a request going out without a model.
/// </remarks>
public class OpenAIReasoningEffortWireTests
{
    [Fact]
    public async Task RawRepresentationFactory_SetsReasoningEffort_ReachesTheRequestBodyWithTheModel()
    {
        // Arrange
        var handler = new CapturingHttpMessageHandler();
        var chatClient = CreateChatClient(handler, "gpt-5.6");

        var options = new ChatOptions
        {
            MaxOutputTokens = 64,
            RawRepresentationFactory = _ =>
            {
                var raw = new CreateResponseOptions();
                raw.ReasoningOptions ??= new ResponseReasoningOptions();
                raw.ReasoningOptions.ReasoningEffortLevel = ResponseReasoningEffortLevel.Low;
                return raw;
            },
        };

        // Act
        await SendAndIgnoreFailureAsync(chatClient, options);

        // Assert
        var body = handler.RequestBodies.ShouldHaveSingleItem();
        body.ShouldContain("\"effort\":\"low\"");
        // The adapter must still supply everything the representation left empty.
        body.ShouldContain("\"model\":\"gpt-5.6\"");
        body.ShouldContain("hello");
    }

    private static IChatClient CreateChatClient(CapturingHttpMessageHandler handler, string modelId)
        => new OpenAIClient(
                new ApiKeyCredential("test-key"),
                new OpenAIClientOptions
                {
                    Transport = new HttpClientPipelineTransport(new HttpClient(handler)),
                    RetryPolicy = new ClientRetryPolicy(0),
                })
            .GetResponsesClient()
            .AsIChatClient(modelId);

    private static async Task SendAndIgnoreFailureAsync(IChatClient chatClient, ChatOptions options)
    {
        try
        {
            await chatClient.GetResponseAsync("hello", options);
        }
        catch (Exception)
        {
            // The capturing handler always fails the request; only the captured body matters here.
        }
    }
}
