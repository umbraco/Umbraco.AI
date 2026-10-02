// DR-4 — Ask decisions over the Management API (AC1-AC4, AC6-AC11)
#pragma warning disable UMBRACOAI_DECISION // Tests the experimental Decision controller

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Decision.Controllers;
using Umbraco.AI.Web.Api.Management.Decision.Models;

namespace Umbraco.AI.Tests.Unit.Api.Management.Decision;

// Controller is the real entry point, constructed directly as every other Management API
// controller test in this project does; the Decision service is the mocked collaborator.
// Assumed service signature (T5): Task<TResponse> AskAsync<TResponse>(
//     Action<AIDecisionBuilder>, AIDecisionQuestion<TResponse>, CancellationToken).
public class AskDecisionControllerTests
{
    private static AskDecisionController CreateController(
        Mock<IAIDecisionService> decisionService,
        Mock<IAIProfileService> profileService,
        bool enabled)
    {
        var experimental = new Mock<IAIExperimentalFeatures>();
        experimental.Setup(x => x.IsCapabilityEnabled(AICapability.Decision)).Returns(enabled);
        return new AskDecisionController(decisionService.Object, profileService.Object, experimental.Object);
    }

    private static AIProfile DecisionProfile(Guid id, string alias) => new()
    {
        Id = id, Alias = alias, Name = alias, ConnectionId = Guid.NewGuid(), Capability = AICapability.Decision,
    };

    private static AskDecisionRequestModel Binary(string instructions = "Is this spam?", string? profile = null) => new()
    {
        ProfileIdOrAlias = profile,
        Question = new BinaryDecisionQuestionModel { Instructions = instructions, Context = "Buy cheap watches" },
    };

    private static AskDecisionRequestModel Choice(params string[] keys) => new()
    {
        Question = new ChoiceDecisionQuestionModel
        {
            Instructions = "Which topic?",
            Options = keys.Select(k => new DecisionOptionModel { Key = k }).ToList(),
        },
    };

    private static AskDecisionRequestModel Score(params string[] levels) => new()
    {
        Question = new ScoreDecisionQuestionModel { Instructions = "How good?", Levels = levels.ToList() },
    };

    // These two go around the params-array helpers above to represent shapes a well-typed caller
    // couldn't produce but a raw JSON payload still can — see AskDecisionRequestModel's remarks on
    // `required` only enforcing property presence, not a non-null value.
    private static AskDecisionRequestModel ChoiceWithOptions(IReadOnlyList<DecisionOptionModel>? options) => new()
    {
        Question = new ChoiceDecisionQuestionModel { Instructions = "Which topic?", Options = options! },
    };

    private static AskDecisionRequestModel ScoreWithLevels(IReadOnlyList<string>? levels) => new()
    {
        Question = new ScoreDecisionQuestionModel { Instructions = "How good?", Levels = levels! },
    };

    #region Happy path

    public class GivenABinaryQuestion
    {
        private readonly IActionResult _result;

        public GivenABinaryQuestion()
        {
            var service = new Mock<IAIDecisionService>();
            service
                .Setup(x => x.AskAsync(
                    It.IsAny<Action<AIDecisionBuilder>>(),
                    It.IsAny<AIDecisionQuestion<AIBinaryDecisionResponse>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIBinaryDecisionResponse { Probability = 0.97, ModelId = "jev-latest" });

            _result = CreateController(service, new Mock<IAIProfileService>(), enabled: true)
                .Ask(Binary()).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsOk() => _result.ShouldBeOfType<OkObjectResult>();

        [Fact]
        public void ReturnsABinaryResponseModel()
            => ((OkObjectResult)_result).Value.ShouldBeOfType<BinaryDecisionResponseModel>();

        [Fact]
        public void MapsTheAnswer()
            => ((BinaryDecisionResponseModel)((OkObjectResult)_result).Value!).Answer.ShouldBeTrue();

        [Fact]
        public void MapsTheProbability()
            => ((BinaryDecisionResponseModel)((OkObjectResult)_result).Value!).Probability.ShouldBe(0.97);

        [Fact]
        public void MapsTheConfidence()
            => ((BinaryDecisionResponseModel)((OkObjectResult)_result).Value!).Confidence.ShouldBe(0.97);

        [Fact]
        public void SerializesWithTheBinaryDiscriminator()
            => JsonSerializer.Serialize<DecisionResponseModel>((DecisionResponseModel)((OkObjectResult)_result).Value!)
                .ShouldContain("\"$type\":\"binary\"");
    }

    public class GivenAChoiceQuestion
    {
        private readonly IActionResult _result;

        public GivenAChoiceQuestion()
        {
            var service = new Mock<IAIDecisionService>();
            service
                .Setup(x => x.AskAsync(
                    It.IsAny<Action<AIDecisionBuilder>>(),
                    It.IsAny<AIDecisionQuestion<AIChoiceDecisionResponse>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIChoiceDecisionResponse
                {
                    Choice = "seo",
                    ChoiceConfidence = 0.91,
                    Probabilities = new Dictionary<string, double> { ["seo"] = 0.91, ["other"] = 0.09 },
                });

            _result = CreateController(service, new Mock<IAIProfileService>(), enabled: true)
                .Ask(Choice("seo", "other")).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsAChoiceResponseModel()
            => ((OkObjectResult)_result).Value.ShouldBeOfType<ChoiceDecisionResponseModel>();

        [Fact]
        public void MapsTheChoice()
            => ((ChoiceDecisionResponseModel)((OkObjectResult)_result).Value!).Choice.ShouldBe("seo");

        [Fact]
        public void MapsTheConfidence()
            => ((ChoiceDecisionResponseModel)((OkObjectResult)_result).Value!).Confidence.ShouldBe(0.91);

        [Fact]
        public void MapsTheProbabilities()
            => ((ChoiceDecisionResponseModel)((OkObjectResult)_result).Value!).Probabilities.Count.ShouldBe(2);
    }

    public class GivenAScoreQuestion
    {
        private readonly IActionResult _result;

        public GivenAScoreQuestion()
        {
            var service = new Mock<IAIDecisionService>();
            service
                .Setup(x => x.AskAsync(
                    It.IsAny<Action<AIDecisionBuilder>>(),
                    It.IsAny<AIDecisionQuestion<AIScoreDecisionResponse>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIScoreDecisionResponse
                {
                    Score = 1.8,
                    Level = "good",
                    ScoreConfidence = 0.8,
                    Probabilities = new Dictionary<string, double> { ["poor"] = 0.05, ["ok"] = 0.15, ["good"] = 0.8 },
                });

            _result = CreateController(service, new Mock<IAIProfileService>(), enabled: true)
                .Ask(Score("poor", "ok", "good")).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsAScoreResponseModel()
            => ((OkObjectResult)_result).Value.ShouldBeOfType<ScoreDecisionResponseModel>();

        [Fact]
        public void MapsTheScore()
            => ((ScoreDecisionResponseModel)((OkObjectResult)_result).Value!).Score.ShouldBe(1.8);

        [Fact]
        public void MapsTheLevel()
            => ((ScoreDecisionResponseModel)((OkObjectResult)_result).Value!).Level.ShouldBe("good");

        [Fact]
        public void MapsTheConfidence()
            => ((ScoreDecisionResponseModel)((OkObjectResult)_result).Value!).Confidence.ShouldBe(0.8);

        [Fact]
        public void MapsTheProbabilitiesByLabel()
            => ((ScoreDecisionResponseModel)((OkObjectResult)_result).Value!).Probabilities["good"].ShouldBe(0.8);
    }

    public class GivenAProfileAlias
    {
        private readonly Mock<IAIProfileService> _profileService = new();
        private readonly Guid _profileId = Guid.NewGuid();

        public GivenAProfileAlias()
        {
            _profileService
                .Setup(x => x.GetProfileByAliasAsync("spam-check", It.IsAny<CancellationToken>()))
                .ReturnsAsync(DecisionProfile(_profileId, "spam-check"));

            var service = new Mock<IAIDecisionService>();
            service
                .Setup(x => x.AskAsync(
                    It.IsAny<Action<AIDecisionBuilder>>(),
                    It.IsAny<AIDecisionQuestion<AIBinaryDecisionResponse>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIBinaryDecisionResponse { Probability = 0.5 });

            CreateController(service, _profileService, enabled: true)
                .Ask(Binary(profile: "spam-check")).GetAwaiter().GetResult();
        }

        [Fact]
        public void ResolvesTheAlias()
            => _profileService.Verify(
                x => x.GetProfileByAliasAsync("spam-check", It.IsAny<CancellationToken>()),
                Times.Once);
    }

    #endregion

    #region Sad path

    public class GivenTheFlagIsOff
    {
        private readonly Mock<IAIDecisionService> _service = new();
        private readonly IActionResult _result;

        public GivenTheFlagIsOff()
        {
            // Invalid body on purpose: the flag check must come before validation.
            _result = CreateController(_service, new Mock<IAIProfileService>(), enabled: false)
                .Ask(Binary(instructions: "  ")).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsAnEmptyNotFound() => _result.ShouldBeOfType<NotFoundResult>();

        [Fact]
        public void DoesNotCallTheService()
            => _service.Invocations.ShouldBeEmpty();
    }

    public class GivenAnInvalidQuestion
    {
        public static TheoryData<string, AskDecisionRequestModel> InvalidRequests => new()
        {
            { "blank instructions", Binary(instructions: "  ") },
            { "one choice option", Choice("a") },
            { "256 choice options", Choice(Enumerable.Range(0, 256).Select(i => $"o{i}").ToArray()) },
            { "duplicate choice keys", Choice("a", "a") },
            { "blank choice key", Choice("a", " ") },
            { "one score level", Score("only") },
            { "eleven score levels", Score(Enumerable.Range(0, 11).Select(i => $"l{i}").ToArray()) },
            { "blank score level", Score("low", " ") },
            { "null options", ChoiceWithOptions(null) },
            { "a null option entry", ChoiceWithOptions([new DecisionOptionModel { Key = "a" }, null!]) },
            { "null levels", ScoreWithLevels(null) },
        };

        [Theory]
        [MemberData(nameof(InvalidRequests))]
        public async Task ReturnsBadRequest(string _, AskDecisionRequestModel request)
            => (await CreateController(new Mock<IAIDecisionService>(), new Mock<IAIProfileService>(), enabled: true)
                .Ask(request)).ShouldBeOfType<BadRequestObjectResult>();

        [Theory]
        [MemberData(nameof(InvalidRequests))]
        public async Task DoesNotCallTheService(string _, AskDecisionRequestModel request)
        {
            var service = new Mock<IAIDecisionService>();
            await CreateController(service, new Mock<IAIProfileService>(), enabled: true).Ask(request);
            service.Invocations.ShouldBeEmpty();
        }
    }

    // These two pin the exact exception System.Text.Json raises for each malformed discriminator shape.
    // Both are format errors an input formatter must translate to a ModelState error (400), never let
    // escape uncaught (500) — see SPEC.md's guarantees for POST decision/ask. Umbraco CMS's own
    // NamedSystemTextJsonInputFormatter (wired up for this API via UmbracoAIUmbracoBuilderExtensions.
    // AddJsonOptions + the [JsonOptionsName] on UmbracoAICoreManagementControllerBase) already handles
    // both — it catches JsonException (the .NET default input formatter's own behaviour) and, unlike
    // the stock formatter, also catches NotSupportedException, which is what a *missing* discriminator
    // raises. Do not "fix" this with a JsonConverter on DecisionQuestionModel: combining a custom
    // converter with [JsonPolymorphic]/[JsonDerivedType] on the same type throws
    // "The converter for derived type ... does not support metadata writes or reads" the moment it's
    // exercised, regardless of what the converter's Read/Write actually do.
    public class GivenAnUnknownQuestionType
    {
        [Fact]
        public void FailsToDeserialize() // ASP.NET turns this into a 400 before the action runs
            => Should.Throw<JsonException>(() => JsonSerializer.Deserialize<AskDecisionRequestModel>(
                """{ "question": { "$type": "bogus", "instructions": "x" } }""",
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    public class GivenAMissingQuestionType
    {
        [Fact]
        public void FailsToDeserialize() // NamedSystemTextJsonInputFormatter turns this into a 400, not a 500
            => Should.Throw<NotSupportedException>(() => JsonSerializer.Deserialize<AskDecisionRequestModel>(
                """{ "question": { "instructions": "x" } }""",
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    public class GivenAProfileThatDoesNotExist
    {
        [Fact]
        public async Task ReturnsNotFoundProblemDetails()
        {
            var profileService = new Mock<IAIProfileService>();
            profileService
                .Setup(x => x.GetProfileByAliasAsync("missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProfile?)null);

            var result = await CreateController(new Mock<IAIDecisionService>(), profileService, enabled: true)
                .Ask(Binary(profile: "missing"));

            result.ShouldBeOfType<NotFoundObjectResult>();
        }
    }

    public class GivenANonDecisionProfile
    {
        [Fact]
        public async Task ReturnsBadRequest()
        {
            var profileService = new Mock<IAIProfileService>();
            profileService
                .Setup(x => x.GetProfileByAliasAsync("chat", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AIProfile
                {
                    Id = Guid.NewGuid(), Alias = "chat", Name = "Chat", ConnectionId = Guid.NewGuid(),
                    Capability = AICapability.Chat,
                });
            var service = new Mock<IAIDecisionService>();
            service
                .Setup(x => x.AskAsync(
                    It.IsAny<Action<AIDecisionBuilder>>(),
                    It.IsAny<AIDecisionQuestion<AIBinaryDecisionResponse>>(),
                    It.IsAny<CancellationToken>()))
                // Real AIDecisionService.EnsureProfileSupportsDecision message (uses profile.Name, not alias).
                .ThrowsAsync(new InvalidOperationException("The profile 'Chat' does not support decision capability."));

            var result = await CreateController(service, profileService, enabled: true).Ask(Binary(profile: "chat"));

            result.ShouldBeOfType<BadRequestObjectResult>();
        }
    }

    public class GivenNoDefaultDecisionProfile
    {
        private readonly IActionResult _result;

        public GivenNoDefaultDecisionProfile()
        {
            var service = new Mock<IAIDecisionService>();
            service
                .Setup(x => x.AskAsync(
                    It.IsAny<Action<AIDecisionBuilder>>(),
                    It.IsAny<AIDecisionQuestion<AIBinaryDecisionResponse>>(),
                    It.IsAny<CancellationToken>()))
                // Real AIProfileService.GetDefaultProfileAsync message for an unconfigured Decision default.
                .ThrowsAsync(new InvalidOperationException("Default Decision profile is not configured."));

            _result = CreateController(service, new Mock<IAIProfileService>(), enabled: true)
                .Ask(Binary()).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsBadRequest() => _result.ShouldBeOfType<BadRequestObjectResult>();

        [Fact]
        public void NamesTheMissingDefault()
            => ((ProblemDetails)((BadRequestObjectResult)_result).Value!).Detail!
                .ShouldContain("Default Decision profile", Case.Insensitive);
    }

    public class GivenTheProviderRejectsTheQuestion
    {
        [Fact]
        public async Task ReturnsBadRequest()
        {
            var service = new Mock<IAIDecisionService>();
            service
                .Setup(x => x.AskAsync(
                    It.IsAny<Action<AIDecisionBuilder>>(),
                    It.IsAny<AIDecisionQuestion<AIBinaryDecisionResponse>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AIProviderException(new AIProviderErrorInfo(
                    AIProviderErrorCategory.InvalidRequest, "The question was rejected.", "422", "validation error")));

            var result = await CreateController(service, new Mock<IAIProfileService>(), enabled: true).Ask(Binary());

            result.ShouldBeOfType<BadRequestObjectResult>();
        }
    }

    #endregion
}
