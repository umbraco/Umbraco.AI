// EX-6 — Extract over the Management API (AC1, AC2, AC5-AC7, AC9-AC11; also EX-3 AC9, flag off)
#pragma warning disable UMBRACOAI_EXTRACTION // Tests the experimental Extraction controller

using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Extraction;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Providers.Errors;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Web.Api.Management.Extraction.Controllers;
using Umbraco.AI.Web.Api.Management.Extraction.Models;

namespace Umbraco.AI.Tests.Unit.Api.Management.Extraction;

// Controller is the real entry point, constructed directly as AskDecisionControllerTests does; the
// Extraction service is the mocked collaborator. Expected service signature (copies Decision):
// Task<AIExtractionResponse> GetExtractionResponseAsync(Action<AIExtractionBuilder>, AIExtractionRequest,
// CancellationToken). Unlike Decision's single-question ask, the request is a batch and the response
// keeps every answer keyed by field id (ARCHITECTURE.md decision 7).
public class ExtractControllerTests
{
    private const string State = "Order A-88412 was delivered on 3 October. I paid 701,10 kr. The mug and teapot were broken.";

    private static ExtractController CreateController(
        Mock<IAIExtractionService> extractionService,
        Mock<IAIProfileService> profileService,
        bool enabled)
    {
        var experimental = new Mock<IAIExperimentalFeatures>();
        experimental.Setup(x => x.IsCapabilityEnabled(AICapability.Extraction)).Returns(enabled);
        return new ExtractController(extractionService.Object, profileService.Object, experimental.Object);
    }

    private static AIProfile ExtractionProfile(Guid id, string alias) => new()
    {
        Id = id, Alias = alias, Name = alias, ConnectionId = Guid.NewGuid(), Capability = AICapability.Extraction,
    };

    /// <summary>Answers every field in whatever request the controller sends, by the field's type.</summary>
    private static Mock<IAIExtractionService> ServiceAnsweringEveryField()
    {
        var service = new Mock<IAIExtractionService>();
        service
            .Setup(x => x.GetExtractionResponseAsync(
                It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Action<AIExtractionBuilder> _, AIExtractionRequest r, CancellationToken _) => new AIExtractionResponse
            {
                Answers = r.Fields.ToDictionary(f => f.Id!, AnswerFor),
                ModelId = "@cf/cloudflare/clairic",
                Usage = new Microsoft.Extensions.AI.UsageDetails { InputTokenCount = 612, OutputTokenCount = 0, TotalTokenCount = 612 },
            });
        return service;
    }

    private static AIExtractionAnswer AnswerFor(AIExtractionField field) => field switch
    {
        AITextExtractionField => new AITextExtractionAnswer { Value = "A-88412", Span = new AIExtractionSpan(6, 13), Confidence = 0.9999 },
        AINumberExtractionField => new AINumberExtractionAnswer { Value = 701.10m, Confidence = 0.9999 },
        AIDateExtractionField => new AIDateExtractionAnswer { Value = new DateOnly(2026, 10, 3), Confidence = 0.998 },
        AITimeExtractionField => new AITimeExtractionAnswer { Value = new TimeOnly(14, 30), Confidence = 0.97 },
        AIDateTimeExtractionField => new AIDateTimeExtractionAnswer { Value = new DateTime(2026, 10, 3, 14, 30, 0), Confidence = 0.97 },
        AIDurationExtractionField duration => new AIDurationExtractionAnswer { Value = 2m, Unit = duration.Unit, Confidence = 0.96 },
        AIListExtractionField => new AIListExtractionAnswer
        {
            Values = ["mug", "teapot"],
            Spans = [new AIExtractionSpan(64, 67), new AIExtractionSpan(72, 78)],
            Confidence = 0.99,
        },
        _ => throw new NotSupportedException(field.GetType().Name),
    };

    /// <summary>The request in SPEC.md "Management API surface".</summary>
    private static ExtractRequestModel SpecRequest(string? profile = "cloudflare-clairic") => new()
    {
        ProfileIdOrAlias = profile,
        State = State,
        ReferenceDate = new DateOnly(2026, 10, 20),
        Fields =
        [
            new TextExtractionFieldModel { Id = "orderId", Instructions = "The order number" },
            new NumberExtractionFieldModel { Id = "amount", Instructions = "Amount paid", Decimals = 2, Unit = "DKK" },
            new DateExtractionFieldModel { Id = "deliveredOn", Instructions = "Delivery date" },
            new ListExtractionFieldModel { Id = "damaged", Instructions = "Damaged products", ItemType = "text" },
        ],
    };

    private static ExtractRequestModel WithFields(params ExtractionFieldModel[] fields) => new()
    {
        State = State,
        Fields = fields,
    };

    private static Mock<IAIProfileService> ProfileServiceResolving(string alias, Guid id)
    {
        var profileService = new Mock<IAIProfileService>();
        profileService
            .Setup(x => x.GetProfileByAliasAsync(alias, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExtractionProfile(id, alias));
        profileService
            .Setup(x => x.GetProfileAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ExtractionProfile(id, alias));
        return profileService;
    }

    #region Happy path

    public class GivenTheSpecRequest
    {
        private readonly IActionResult _result;

        public GivenTheSpecRequest()
        {
            _result = CreateController(ServiceAnsweringEveryField(), ProfileServiceResolving("cloudflare-clairic", Guid.NewGuid()), enabled: true)
                .Extract(SpecRequest()).GetAwaiter().GetResult();
        }

        private ExtractResponseModel Response => (ExtractResponseModel)((OkObjectResult)_result).Value!;

        [Fact]
        public void ReturnsOk() => _result.ShouldBeOfType<OkObjectResult>();

        [Fact]
        public void DeclaresTheBaseResponseTypeSoTheDiscriminatorsSurvive()
            => ((OkObjectResult)_result).DeclaredType.ShouldBe(typeof(ExtractResponseModel));

        [Fact]
        public void ReturnsOneAnswerPerField() => Response.Answers.Count.ShouldBe(4);

        [Fact]
        public void ReturnsATextAnswerForTheTextField()
            => Response.Answers["orderId"].ShouldBeOfType<TextExtractionAnswerModel>();

        [Fact]
        public void ReturnsANumberAnswerForTheNumberField()
            => Response.Answers["amount"].ShouldBeOfType<NumberExtractionAnswerModel>();

        [Fact]
        public void ReturnsADateAnswerForTheDateField()
            => Response.Answers["deliveredOn"].ShouldBeOfType<DateExtractionAnswerModel>();

        [Fact]
        public void ReturnsAListAnswerForTheListField()
            => Response.Answers["damaged"].ShouldBeOfType<ListExtractionAnswerModel>();

        [Fact]
        public void MapsTheTextValue()
            => ((TextExtractionAnswerModel)Response.Answers["orderId"]).Value.ShouldBe("A-88412");

        [Fact]
        public void MapsTheNumberValue()
            => ((NumberExtractionAnswerModel)Response.Answers["amount"]).Value.ShouldBe(701.10m);

        [Fact]
        public void MapsTheListValues()
            => ((ListExtractionAnswerModel)Response.Answers["damaged"]).Values.ShouldBe(["mug", "teapot"]);

        [Fact]
        public void ReturnsTheModelId() => Response.ModelId.ShouldBe("@cf/cloudflare/clairic");

        [Fact]
        public void ReturnsTheUsage() => Response.Usage!.InputTokens.ShouldBe(612);
    }

    public class GivenTheSpecRequestIsSentToTheService
    {
        private AIExtractionRequest? _sent;

        public GivenTheSpecRequestIsSentToTheService()
        {
            var service = ServiceAnsweringEveryField();
            service
                .Setup(x => x.GetExtractionResponseAsync(
                    It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<Action<AIExtractionBuilder>, AIExtractionRequest, CancellationToken>((_, r, _) => _sent = r)
                .ReturnsAsync((Action<AIExtractionBuilder> _, AIExtractionRequest r, CancellationToken _) => new AIExtractionResponse
                {
                    Answers = r.Fields.ToDictionary(f => f.Id!, AnswerFor),
                });

            CreateController(service, ProfileServiceResolving("cloudflare-clairic", Guid.NewGuid()), enabled: true)
                .Extract(SpecRequest()).GetAwaiter().GetResult();
        }

        [Fact]
        public void SendsEveryFieldInOneBatch() => _sent!.Fields.Count.ShouldBe(4);

        [Fact]
        public void SendsTheState() => _sent!.State.ShouldBe(State);

        [Fact]
        public void SendsTheReferenceDate() => _sent!.ReferenceDate.ShouldBe(new DateOnly(2026, 10, 20));

        [Fact]
        public void MapsTheNumberFieldOptions()
            => ((AINumberExtractionField)_sent!.Fields[1]).Decimals.ShouldBe(2);
    }

    public class GivenAProfileAlias
    {
        private readonly Mock<IAIProfileService> _profileService = ProfileServiceResolving("cloudflare-clairic", Guid.NewGuid());

        public GivenAProfileAlias()
            => CreateController(ServiceAnsweringEveryField(), _profileService, enabled: true)
                .Extract(SpecRequest(profile: "cloudflare-clairic")).GetAwaiter().GetResult();

        [Fact]
        public void ResolvesTheAlias()
            => _profileService.Verify(
                x => x.GetProfileByAliasAsync("cloudflare-clairic", It.IsAny<CancellationToken>()),
                Times.Once);
    }

    public class GivenAProfileId
    {
        private readonly Guid _profileId = Guid.NewGuid();
        private readonly Mock<IAIProfileService> _profileService;

        public GivenAProfileId()
        {
            _profileService = ProfileServiceResolving("cloudflare-clairic", _profileId);
            CreateController(ServiceAnsweringEveryField(), _profileService, enabled: true)
                .Extract(SpecRequest(profile: _profileId.ToString())).GetAwaiter().GetResult();
        }

        [Fact]
        public void ResolvesTheId()
            => _profileService.Verify(
                x => x.GetProfileAsync(_profileId, It.IsAny<CancellationToken>()),
                Times.Once);
    }

    public class GivenNoProfileAndADefaultExtractionProfile
    {
        private readonly Mock<IAIProfileService> _profileService = new();
        private readonly IActionResult _result;

        public GivenNoProfileAndADefaultExtractionProfile()
        {
            // The default is resolved inside IAIExtractionService (as for Decision), so the controller
            // passes no profile through and never looks one up itself.
            _result = CreateController(ServiceAnsweringEveryField(), _profileService, enabled: true)
                .Extract(SpecRequest(profile: null)).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsOk() => _result.ShouldBeOfType<OkObjectResult>();

        [Fact]
        public void DoesNotLookUpAProfile() => _profileService.Invocations.ShouldBeEmpty();
    }

    public class GivenTimeDateTimeAndDurationFields
    {
        private readonly AIExtractionRequest? _sent;
        private readonly ExtractResponseModel _response;

        public GivenTimeDateTimeAndDurationFields()
        {
            AIExtractionRequest? sent = null;
            var service = ServiceAnsweringEveryField();
            service
                .Setup(x => x.GetExtractionResponseAsync(
                    It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
                .Callback<Action<AIExtractionBuilder>, AIExtractionRequest, CancellationToken>((_, r, _) => sent = r)
                .ReturnsAsync((Action<AIExtractionBuilder> _, AIExtractionRequest r, CancellationToken _) => new AIExtractionResponse
                {
                    Answers = r.Fields.ToDictionary(f => f.Id!, AnswerFor),
                });

            var result = CreateController(service, new Mock<IAIProfileService>(), enabled: true)
                .Extract(WithFields(
                    new TimeExtractionFieldModel { Id = "at", Instructions = "Time of delivery" },
                    new DateTimeExtractionFieldModel { Id = "when", Instructions = "Delivery moment" },
                    new DurationExtractionFieldModel { Id = "wait", Instructions = "How long it took", Unit = "Hours" }))
                .GetAwaiter().GetResult();

            _sent = sent;
            _response = (ExtractResponseModel)((OkObjectResult)result).Value!;
        }

        [Fact]
        public void MapsTheTimeField() => _sent!.Fields[0].ShouldBeOfType<AITimeExtractionField>();

        [Fact]
        public void MapsTheDateTimeField() => _sent!.Fields[1].ShouldBeOfType<AIDateTimeExtractionField>();

        [Fact]
        public void MapsAMixedCaseDurationUnit() => ((AIDurationExtractionField)_sent!.Fields[2]).Unit.ShouldBe(AIDurationUnit.Hours);

        [Fact]
        public void ReturnsTheDurationUnitInLowerCase() => ((DurationExtractionAnswerModel)_response.Answers["wait"]).Unit.ShouldBe("hours");

        [Fact]
        public void ReturnsTheTimeAnswer() => ((TimeExtractionAnswerModel)_response.Answers["at"]).Value.ShouldBe(new TimeOnly(14, 30));

        [Fact]
        public void ReturnsTheDateTimeAnswer()
            => ((DateTimeExtractionAnswerModel)_response.Answers["when"]).Value.ShouldBe(new DateTime(2026, 10, 3, 14, 30, 0));
    }

    public class GivenAListOfNumbers
    {
        [Fact]
        public void MapsTheNumberItemType() => ItemTypeSent("number").ShouldBe(AIExtractionListItemType.Number);
    }

    public class GivenAListOfDates
    {
        [Fact]
        public void MapsTheDateItemType() => ItemTypeSent("date").ShouldBe(AIExtractionListItemType.Date);
    }

    private static AIExtractionListItemType ItemTypeSent(string itemType)
    {
        AIExtractionRequest? sent = null;
        var service = ServiceAnsweringEveryField();
        service
            .Setup(x => x.GetExtractionResponseAsync(
                It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Action<AIExtractionBuilder>, AIExtractionRequest, CancellationToken>((_, r, _) => sent = r)
            .ReturnsAsync(new AIExtractionResponse { Answers = new Dictionary<string, AIExtractionAnswer>() });

        CreateController(service, new Mock<IAIProfileService>(), enabled: true)
            .Extract(WithFields(new ListExtractionFieldModel { Id = "items", Instructions = "x", ItemType = itemType }))
            .GetAwaiter().GetResult();

        return ((AIListExtractionField)sent!.Fields[0]).ItemType;
    }

    #endregion

    #region Sad path

    public class GivenTheFlagIsOff
    {
        private readonly Mock<IAIExtractionService> _service = new();
        private readonly IActionResult _result;

        public GivenTheFlagIsOff()
        {
            // Invalid body on purpose: the flag check must come before validation.
            _result = CreateController(_service, new Mock<IAIProfileService>(), enabled: false)
                .Extract(WithFields()).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsAnEmptyNotFound() => _result.ShouldBeOfType<NotFoundResult>();

        [Fact]
        public void DoesNotCallTheService() => _service.Invocations.ShouldBeEmpty();

        [Fact]
        public void TheControllerBaseIsGatedOnExtractionForRealHttpTraffic()
            // The shared AICapabilityGateFilter (tested in AICapabilityGateFilterTests) returns the 404
            // before body binding; this pins that ExtractionControllerBase carries it for Extraction.
            => typeof(ExtractionControllerBase)
                .GetCustomAttributes(typeof(Umbraco.AI.Web.Api.Management.Common.Filters.AICapabilityGateAttribute), inherit: false)
                .Cast<Umbraco.AI.Web.Api.Management.Common.Filters.AICapabilityGateAttribute>()
                .SelectMany(a => a.Arguments!.OfType<AICapability>())
                .ShouldBe([AICapability.Extraction]);
    }

    public class GivenAnInvalidRequest
    {
        public static TheoryData<string, ExtractRequestModel> InvalidRequests => new()
        {
            {
                "duplicate field id",
                WithFields(
                    new TextExtractionFieldModel { Id = "orderId", Instructions = "The order number" },
                    new TextExtractionFieldModel { Id = "orderId", Instructions = "The order number again" })
            },
            { "no fields", WithFields() },
            { "invalid field id", WithFields(new TextExtractionFieldModel { Id = "1order", Instructions = "x" }) },
            { "blank instructions", WithFields(new TextExtractionFieldModel { Id = "orderId", Instructions = "  " }) },
            { "decimals above 6", WithFields(new NumberExtractionFieldModel { Id = "amount", Instructions = "x", Decimals = 7 }) },
            { "unknown duration unit", WithFields(new DurationExtractionFieldModel { Id = "wait", Instructions = "x", Unit = "fortnights" }) },
            { "numeric duration unit", WithFields(new DurationExtractionFieldModel { Id = "wait", Instructions = "x", Unit = "3" }) },
            { "flag-combined duration unit", WithFields(new DurationExtractionFieldModel { Id = "wait", Instructions = "x", Unit = "seconds,minutes" }) },
            { "numeric list item type", WithFields(new ListExtractionFieldModel { Id = "damaged", Instructions = "x", ItemType = "3" }) },
            { "unknown list item type", WithFields(new ListExtractionFieldModel { Id = "damaged", Instructions = "x", ItemType = "widgets" }) },
        };

        [Theory]
        [MemberData(nameof(InvalidRequests))]
        public async Task ReturnsBadRequestProblemDetails(string _, ExtractRequestModel request)
            => ((BadRequestObjectResult)await CreateController(new Mock<IAIExtractionService>(), new Mock<IAIProfileService>(), enabled: true)
                .Extract(request)).Value.ShouldBeOfType<ProblemDetails>();

        [Theory]
        [MemberData(nameof(InvalidRequests))]
        public async Task DoesNotCallTheService(string _, ExtractRequestModel request)
        {
            var service = new Mock<IAIExtractionService>();
            await CreateController(service, new Mock<IAIProfileService>(), enabled: true).Extract(request);
            service.Invocations.ShouldBeEmpty();
        }
    }

    public class GivenNoProfileAndNoDefault
    {
        private readonly IActionResult _result;

        public GivenNoProfileAndNoDefault()
        {
            var service = new Mock<IAIExtractionService>();
            service
                .Setup(x => x.GetExtractionResponseAsync(
                    It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
                // Mirrors AIProfileService.GetDefaultProfileAsync's message for an unconfigured default.
                .ThrowsAsync(new InvalidOperationException("Default Extraction profile is not configured."));

            _result = CreateController(service, new Mock<IAIProfileService>(), enabled: true)
                .Extract(SpecRequest(profile: null)).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsBadRequestProblemDetails()
            => ((BadRequestObjectResult)_result).Value.ShouldBeOfType<ProblemDetails>();

        [Fact]
        public void NamesTheMissingDefault()
            => ((ProblemDetails)((BadRequestObjectResult)_result).Value!).Detail!
                .ShouldContain("Default Extraction profile", Case.Insensitive);
    }

    public class GivenANonExtractionProfile
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
            var service = new Mock<IAIExtractionService>();
            service
                .Setup(x => x.GetExtractionResponseAsync(
                    It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("The profile 'Chat' does not support extraction capability."));

            var result = await CreateController(service, profileService, enabled: true).Extract(SpecRequest(profile: "chat"));

            result.ShouldBeOfType<BadRequestObjectResult>();
        }
    }

    public class GivenAProfileThatDoesNotExist
    {
        [Fact]
        public async Task ReturnsNotFoundProblemDetails()
        {
            // Same as Decision: a named-but-missing profile is a 404, not a 400.
            var profileService = new Mock<IAIProfileService>();
            profileService
                .Setup(x => x.GetProfileByAliasAsync("missing", It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIProfile?)null);

            var result = await CreateController(new Mock<IAIExtractionService>(), profileService, enabled: true)
                .Extract(SpecRequest(profile: "missing"));

            result.ShouldBeOfType<NotFoundObjectResult>();
        }
    }

    public class GivenTheProviderFails
    {
        private readonly IActionResult _result;

        public GivenTheProviderFails()
        {
            var service = new Mock<IAIExtractionService>();
            service
                .Setup(x => x.GetExtractionResponseAsync(
                    It.IsAny<Action<AIExtractionBuilder>>(), It.IsAny<AIExtractionRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new AIProviderException(new AIProviderErrorInfo(
                    AIProviderErrorCategory.RateLimited, "Capacity temporarily exceeded", "3040", "429")));

            _result = CreateController(service, ProfileServiceResolving("cloudflare-clairic", Guid.NewGuid()), enabled: true)
                .Extract(SpecRequest()).GetAwaiter().GetResult();
        }

        [Fact]
        public void ReturnsBadRequestProblemDetails()
            => ((BadRequestObjectResult)_result).Value.ShouldBeOfType<ProblemDetails>();
    }

    #endregion
}
