#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Tests.Common.Decision.Spike;

namespace Umbraco.AI.Tests.Unit.Decision;

/// <summary>
/// DC-4 (AC1) — real entry point. Exercises the real <see cref="HttpClient"/> pipeline through the
/// real, reusable <see cref="JevSpikeDecisionClient"/> (<c>Umbraco.AI.Tests.Common</c>); only the
/// outermost transport (<see cref="HttpMessageHandler"/>) is faked, so this is the closest
/// automatable proxy for "a real HTTP round trip" — the genuinely live call against Jev with real
/// credentials (T11's acceptance criterion) is a separate, manual verification step, same as this
/// repo's other real-provider-key checks.
/// </summary>
/// <remarks>
/// The endpoint path and JSON field names below mirror Jev's real <c>/v1/systemone</c> wire contract,
/// confirmed against <see href="https://docs.typesafe.ai/api"/> and a live call during T11 — this
/// spec proves the plumbing (HttpClient → JSON → typed <see cref="Umbraco.AI.Core.Decision.AIDecisionResponse"/>)
/// against a wire shape that has actually been exercised for real, not a placeholder guess.
/// </remarks>
public class JevSpikeProviderTests
{
    public class GivenABinaryQuestionAndAWorkingEndpoint
    {
        private readonly JevSpikeDecisionClient _sut;

        public GivenABinaryQuestionAndAWorkingEndpoint()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(AnswerResponse(new JevAnswerDto("noul", 0.92, null, null, null))),
            });
            _sut = new JevSpikeDecisionClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.typesafe.ai") }, "test-api-key");
        }

        [Fact]
        public async Task ReturnsABinaryDecisionResponse()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" };

            var response = await _sut.AskAsync(question);

            response.Kind.ShouldBe(AIDecisionKind.Binary);
        }

        [Fact]
        public async Task MapsTheProbabilityToATrueAnswerAboveTheHalfwayThreshold()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" };

            var response = await _sut.AskAsync(question);

            response.BinaryAnswer.ShouldBe(true);
        }

        [Fact]
        public async Task ReportsConfidenceAsTheWinningProbability()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" };

            var response = await _sut.AskAsync(question);

            response.Confidence.ShouldBe(0.92);
        }
    }

    public class GivenABinaryQuestionAnsweredBelowTheHalfwayThreshold
    {
        private readonly JevSpikeDecisionClient _sut;

        public GivenABinaryQuestionAnsweredBelowTheHalfwayThreshold()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(AnswerResponse(new JevAnswerDto("noul", 0.08, null, null, null))),
            });
            _sut = new JevSpikeDecisionClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.typesafe.ai") }, "test-api-key");
        }

        [Fact]
        public async Task MapsTheProbabilityToAFalseAnswer()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" };

            var response = await _sut.AskAsync(question);

            response.BinaryAnswer.ShouldBe(false);
        }

        [Fact]
        public async Task ReportsConfidenceAsTheComplementOfTheProbability()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" };

            var response = await _sut.AskAsync(question);

            response.Confidence.ShouldBe(0.92);
        }
    }

    public class GivenAChoiceQuestionAndAWorkingEndpoint
    {
        private readonly JevSpikeDecisionClient _sut;

        public GivenAChoiceQuestionAndAWorkingEndpoint()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(AnswerResponse(new JevAnswerDto("choice", null, "positive", null, 0.81))),
            });
            _sut = new JevSpikeDecisionClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.typesafe.ai") }, "test-api-key");
        }

        [Fact]
        public async Task ReturnsTheSelectedChoice()
        {
            var question = new AIDecisionQuestion
            {
                Kind = AIDecisionKind.Choice,
                Prompt = "what is the sentiment?",
                Choices = ["positive", "negative", "neutral"],
            };

            var response = await _sut.AskAsync(question);

            response.SelectedChoice.ShouldBe("positive");
        }
    }

    public class GivenAScoreQuestionAndAWorkingEndpoint
    {
        private readonly JevSpikeDecisionClient _sut;

        public GivenAScoreQuestionAndAWorkingEndpoint()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(AnswerResponse(new JevAnswerDto("score", null, null, 7.5, 0.63))),
            });
            _sut = new JevSpikeDecisionClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.typesafe.ai") }, "test-api-key");
        }

        [Fact]
        public async Task ReturnsTheNumericScore()
        {
            var question = new AIDecisionQuestion
            {
                Kind = AIDecisionKind.Score,
                Prompt = "how confident is this claim, from 0 to 10?",
                ScoreRange = (0, 10),
            };

            var response = await _sut.AskAsync(question);

            response.Score.ShouldBe(7.5);
        }
    }

    public class GivenAServerErrorResponse
    {
        private readonly JevSpikeDecisionClient _sut;

        public GivenAServerErrorResponse()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
            _sut = new JevSpikeDecisionClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.typesafe.ai") }, "test-api-key");
        }

        [Fact]
        public async Task ThrowsHttpRequestException()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" };

            await Should.ThrowAsync<HttpRequestException>(() => _sut.AskAsync(question));
        }
    }

    public class GivenANoulAnswerWithNoProbabilityValue
    {
        private readonly JevSpikeDecisionClient _sut;

        public GivenANoulAnswerWithNoProbabilityValue()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(AnswerResponse(new JevAnswerDto("noul", null, null, null, null))),
            });
            _sut = new JevSpikeDecisionClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.typesafe.ai") }, "test-api-key");
        }

        [Fact]
        public async Task ThrowsJsonException()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" };

            await Should.ThrowAsync<JsonException>(() => _sut.AskAsync(question));
        }
    }

    public class GivenAnUnrecognizedAnswerKind
    {
        private readonly JevSpikeDecisionClient _sut;

        public GivenAnUnrecognizedAnswerKind()
        {
            var handler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(AnswerResponse(new JevAnswerDto("mystery", null, null, null, 0.5))),
            });
            _sut = new JevSpikeDecisionClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.typesafe.ai") }, "test-api-key");
        }

        [Fact]
        public async Task ThrowsJsonException()
        {
            var question = new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" };

            await Should.ThrowAsync<JsonException>(() => _sut.AskAsync(question));
        }
    }

    public class GivenAQuestionAndACustomAnswerPath
    {
        [Fact]
        public async Task PostsToTheConfiguredPathWithTheBearerToken()
        {
            HttpRequestMessage? capturedRequest = null;
            var handler = new FakeHttpMessageHandler(request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(AnswerResponse(new JevAnswerDto("noul", 0.92, null, null, null))),
                };
            });
            var sut = new JevSpikeDecisionClient(
                new HttpClient(handler) { BaseAddress = new Uri("https://api.typesafe.ai") },
                "test-api-key",
                "/v2/custom-answer");

            await sut.AskAsync(new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" });

            capturedRequest.ShouldNotBeNull();
            capturedRequest!.RequestUri.ShouldBe(new Uri("https://api.typesafe.ai/v2/custom-answer"));
            capturedRequest.Headers.Authorization.ShouldNotBeNull();
            capturedRequest.Headers.Authorization!.Scheme.ShouldBe("Bearer");
            capturedRequest.Headers.Authorization!.Parameter.ShouldBe("test-api-key");
        }
    }

    public class GivenABinaryQuestion
    {
        [Fact]
        public async Task SendsJevsNoulTypeDiscriminatorNotTheEnumMemberName()
        {
            string? sentBody = null;
            var handler = new FakeHttpMessageHandler(request =>
            {
                // Read the body here, before AskAsync's `using` disposes the request/content.
                sentBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(AnswerResponse(new JevAnswerDto("noul", 0.92, null, null, null))),
                };
            });
            var sut = new JevSpikeDecisionClient(new HttpClient(handler) { BaseAddress = new Uri("https://api.typesafe.ai") }, "test-api-key");

            await sut.AskAsync(new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" });

            sentBody.ShouldNotBeNull();
            sentBody.ShouldContain("\"type\":\"noul\"");
            sentBody.ShouldNotContain("\"type\":\"binary\"");
        }
    }

    /// <summary>Matches Jev's real batch response shape — one named answer, keyed <c>"q"</c>.</summary>
    private static JevSystemOneResponse AnswerResponse(JevAnswerDto answer)
        => new("jev-1.13.0", new Dictionary<string, JevAnswerDto> { ["q"] = answer });

    /// <summary>DC-4 (AC2) — the experimental flag, not the client itself, gates reachability; see DC-1's AC4-AC6 specs for that behavior.</summary>
    [Fact]
    public void Kind_IsDecision()
    {
        var provider = BuildProvider();

        provider.GetCapability<JevSpikeDecisionCapability>().Kind.ShouldBe(AICapability.Decision);
    }

    /// <summary>DC-4 error path — a missing API key fails fast rather than reaching the network.</summary>
    [Fact]
    public async Task CreateClient_WithMissingApiKey_ThrowsInvalidOperationException()
    {
        var provider = BuildProvider();
        var capability = (IAIDecisionCapability)provider.GetCapability<JevSpikeDecisionCapability>();
        var settings = new JevSpikeProviderSettings { ApiKey = null };

        await Should.ThrowAsync<InvalidOperationException>(
            () => capability.CreateClientAsync(settings, null, default));
    }

    /// <summary>
    /// Guards against the exact bug T11 fixed everywhere else in this file: a connection with a
    /// blank <see cref="JevSpikeProviderSettings.AnswerPath"/> (e.g. one saved before the real
    /// endpoint was known) must fall back to the real Jev endpoint
    /// (<see cref="JevSpikeProviderSettings.DefaultAnswerPath"/>), not the old, wrong placeholder.
    /// </summary>
    [Fact]
    public async Task CreateClient_WithBlankAnswerPath_FallsBackToTheRealJevEndpoint()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(AnswerResponse(new JevAnswerDto("noul", 0.92, null, null, null))),
            };
        });
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient(handler));
        var provider = BuildProvider(httpClientFactoryMock.Object);
        var capability = (IAIDecisionCapability)provider.GetCapability<JevSpikeDecisionCapability>();
        var settings = new JevSpikeProviderSettings { ApiKey = "test-api-key", AnswerPath = "   " };

        var client = await capability.CreateClientAsync(settings, null, default);
        await client.AskAsync(new AIDecisionQuestion { Kind = AIDecisionKind.Binary, Prompt = "is this text spam?" });

        capturedRequest.ShouldNotBeNull();
        capturedRequest!.RequestUri!.AbsolutePath.ShouldBe(JevSpikeProviderSettings.DefaultAnswerPath);
    }

    private static JevSpikeProvider BuildProvider(IHttpClientFactory? httpClientFactory = null)
        => JevSpikeProviderFactory.Create(httpClientFactory);

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request));
    }
}
