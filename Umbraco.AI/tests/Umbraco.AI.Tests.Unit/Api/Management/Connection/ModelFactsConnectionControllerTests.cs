// MF-5: The backoffice can fetch a connection's model facts (AC1-AC12).
// Real entry point: the ModelFactsConnectionController.GetModelFacts action, with a real UmbracoMapper
// (CommonMapDefinition + ConnectionMapDefinition + the new ModelFactsMapDefinition). The connection
// service and IAIModelFactService are mocked; the fact service mock honours its contract by returning
// facts only for the models it is asked about.
// Assumed: ctor (IAIConnectionService, IAIModelFactService, IAIExperimentalFeatures, IUmbracoMapper, ILogger<ModelFactsConnectionController>);
// action GetModelFacts(IdOrAlias, string? capability, string? modelId, CancellationToken);
// ModelFactsResponseModel.Items -> ModelFactsItemResponseModel { Model (ModelRefModel), Facts }
// -> ModelFactResponseModel { Key, Label, ShortLabel, Value, SortValue, Detail, Tone, Url }.
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.ModelFacts;
using Umbraco.AI.Core.Providers;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Tests.Common.Builders;
using Umbraco.AI.Tests.Common.Fakes;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.AI.Web.Api.Management.Common.Mapping;
using Umbraco.AI.Web.Api.Management.Connection.Controllers;
using Umbraco.AI.Web.Api.Management.Connection.Mapping;
using Umbraco.AI.Web.Api.Management.Connection.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;

namespace Umbraco.AI.Tests.Unit.Api.Management.Connection;

public class ModelFactsConnectionControllerTests
{
    private const string ProviderId = "openrouter";
    private const string Alias = "my-openrouter";

    /// <summary>
    /// Shared arrange: connection c1 (alias "my-openrouter", provider "openrouter") with a Chat
    /// capability listing m1 and m2, and a fact service that knows facts for m1 and m2.
    /// </summary>
    public abstract class ConnectionScenario
    {
        protected readonly Guid ConnectionId = Guid.NewGuid();
        protected readonly Mock<IAIConnectionService> ConnectionService = new();
        protected readonly Mock<IAIModelFactService> FactService = new();
        protected readonly Mock<IAIExperimentalFeatures> ExperimentalFeatures = new();
        protected readonly Mock<ILogger<ModelFactsConnectionController>> Logger = new();
        protected readonly Mock<IAIConfiguredCapability> ChatCapability = new();
        protected readonly Dictionary<string, IReadOnlyList<AIModelFact>> KnownFacts = new();

        protected ConnectionScenario()
        {
            var connection = new AIConnectionBuilder()
                .WithId(ConnectionId)
                .WithAlias(Alias)
                .WithProviderId(ProviderId)
                .Build();

            ChatCapability.Setup(x => x.Kind).Returns(AICapability.Chat);
            ChatCapability
                .Setup(x => x.GetModelsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AIModelDescriptor>
                {
                    new(new AIModelRef(ProviderId, "m1"), "Model 1"),
                    new(new AIModelRef(ProviderId, "m2"), "Model 2"),
                });

            var configured = new Mock<IAIConfiguredProvider>();
            configured.Setup(x => x.Provider).Returns(new FakeAIProvider(ProviderId, "OpenRouter"));
            configured.Setup(x => x.GetCapabilities()).Returns(new[] { ChatCapability.Object });

            ConnectionService
                .Setup(x => x.GetConfiguredProviderAsync(ConnectionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(configured.Object);
            ConnectionService
                .Setup(x => x.GetConnectionByAliasAsync(Alias, It.IsAny<CancellationToken>()))
                .ReturnsAsync(connection);

            FactService
                .Setup(x => x.GetModelFactsAsync(
                    It.IsAny<AIModelFactContext>(),
                    It.IsAny<IReadOnlyList<AIModelDescriptor>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((AIModelFactContext _, IReadOnlyList<AIModelDescriptor> models, CancellationToken _) =>
                    (IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>)KnownFacts
                        .Where(kv => models.Any(m => m.Model.ModelId == kv.Key))
                        .ToDictionary(kv => kv.Key, kv => kv.Value));

            ExperimentalFeatures.Setup(x => x.IsCapabilityEnabled(It.IsAny<AICapability>())).Returns(true);
            Logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
        }

        protected ModelFactsConnectionController CreateController()
            => new(
                ConnectionService.Object,
                FactService.Object,
                ExperimentalFeatures.Object,
                new UmbracoMapper(
                    new MapDefinitionCollection(() => new IMapDefinition[]
                    {
                        new CommonMapDefinition(),
                        new ConnectionMapDefinition(),
                        new ModelFactsMapDefinition(),
                    }),
                    Mock.Of<ICoreScopeProvider>(),
                    NullLogger<UmbracoMapper>.Instance),
                Logger.Object);

        protected Task<IActionResult> GetAsync(IdOrAlias idOrAlias, string? capability, string? modelId = null)
            => CreateController().GetModelFacts(idOrAlias, capability, modelId, CancellationToken.None);

        protected Task<IActionResult> GetByIdAsync(string? capability, string? modelId = null)
            => GetAsync(new IdOrAlias(ConnectionId), capability, modelId);

        /// <summary>Unwraps a 200 response (a plain cast, so a non-200 fails the spec here).</summary>
        protected static List<ModelFactsItemResponseModel> ItemsOf(IActionResult result)
            => ((ModelFactsResponseModel)((OkObjectResult)result).Value!).Items.ToList();

        protected static AIModelFact MakeFact(string key) => new() { Key = key, Label = key, Value = key };
    }

    // ---------------------------------------------------------------- Happy path

    public class GivenOnlyM1HasFacts : ConnectionScenario
    {
        public GivenOnlyM1HasFacts() => KnownFacts["m1"] = [MakeFact("f1")];

        [Fact] // MF-5 AC1
        public async Task RequestingM1_Returns200()
            => (await GetByIdAsync("Chat", "m1")).ShouldBeOfType<OkObjectResult>();

        [Fact] // MF-5 AC1
        public async Task RequestingM1_ReturnsOneItem()
            => ItemsOf(await GetByIdAsync("Chat", "m1")).Count.ShouldBe(1);

        [Fact] // MF-5 AC1
        public async Task RequestingM1_TheItemIsForM1()
            => ItemsOf(await GetByIdAsync("Chat", "m1"))[0].Model.ModelId.ShouldBe("m1");
    }

    public class GivenBothModelsHaveFacts : ConnectionScenario
    {
        public GivenBothModelsHaveFacts()
        {
            KnownFacts["m1"] = [MakeFact("f1")];
            KnownFacts["m2"] = [MakeFact("f2")];
        }

        [Fact] // MF-5 AC2
        public async Task RequestingWithoutModelId_ReturnsItemsForM1AndM2()
            => ItemsOf(await GetByIdAsync("Chat"))
                .Select(i => i.Model.ModelId)
                .ShouldBe(new[] { "m1", "m2" }, ignoreOrder: true);
    }

    public class GivenM1HasAFactWithEveryFieldSet : ConnectionScenario
    {
        public GivenM1HasAFactWithEveryFieldSet() => KnownFacts["m1"] =
        [
            new AIModelFact
            {
                Key = "core.contextWindow",
                Label = "#uaiModelFacts_contextWindow",
                ShortLabel = "#uaiModelFacts_contextWindowShort",
                Value = "200,000",
                SortValue = 200000,
                Detail = "#uaiModelFacts_priceDetail",
                Tone = AIModelFactTone.Warning,
                Url = "https://example.com/model",
            },
        ];

        private async Task<ModelFactResponseModel> GetFactAsync()
            => ItemsOf(await GetByIdAsync("Chat", "m1"))[0].Facts.First();

        [Fact] // MF-5 AC3
        public async Task KeyIsMapped() => (await GetFactAsync()).Key.ShouldBe("core.contextWindow");

        [Fact] // MF-5 AC3
        public async Task LabelIsMapped() => (await GetFactAsync()).Label.ShouldBe("#uaiModelFacts_contextWindow");

        [Fact] // MF-5 AC3
        public async Task ShortLabelIsMapped()
            => (await GetFactAsync()).ShortLabel.ShouldBe("#uaiModelFacts_contextWindowShort");

        [Fact] // MF-5 AC3
        public async Task ValueIsMapped() => (await GetFactAsync()).Value.ShouldBe("200,000");

        [Fact] // MF-5 AC3
        public async Task SortValueIsMapped() => (await GetFactAsync()).SortValue.ShouldBe(200000d);

        [Fact] // MF-5 AC3
        public async Task DetailIsMapped() => (await GetFactAsync()).Detail.ShouldBe("#uaiModelFacts_priceDetail");

        [Fact] // MF-5 AC3 — tone serializes by name ("Warning"), whether the model holds a string or the enum
        public async Task ToneIsMapped() => (await GetFactAsync()).Tone.ToString().ShouldBe("Warning");

        [Fact] // MF-5 AC3
        public async Task UrlIsMapped() => (await GetFactAsync()).Url.ShouldBe("https://example.com/model");
    }

    public class GivenTheConnectionIsAddressedByAlias : ConnectionScenario
    {
        [Fact] // MF-5 AC4
        public async Task RequestingByAlias_Returns200()
            => (await GetAsync(new IdOrAlias(Alias), "Chat")).ShouldBeOfType<OkObjectResult>();
    }

    // ---------------------------------------------------------------- Sad path

    public class GivenNoConnectionMatches : ConnectionScenario
    {
        [Fact] // MF-5 AC5
        public async Task RequestingAnUnknownConnection_Returns404()
            => (await GetAsync(new IdOrAlias("nope"), "Chat")).ShouldBeOfType<NotFoundObjectResult>();
    }

    public class GivenAnExistingConnection : ConnectionScenario
    {
        [Fact] // MF-5 AC6
        public async Task RequestingWithoutACapability_Returns400()
            => (await GetByIdAsync(capability: null)).ShouldBeOfType<BadRequestObjectResult>();

        [Fact] // MF-5 AC7
        public async Task RequestingAnUnparseableCapability_Returns400()
            => (await GetByIdAsync("Banana")).ShouldBeOfType<BadRequestObjectResult>();

        [Fact]
        public async Task RequestingANumericCapability_Returns400()
            => (await GetByIdAsync("1")).ShouldBeOfType<BadRequestObjectResult>();
    }

    public class GivenTheConnectionHasNoEmbeddingCapability : ConnectionScenario
    {
        public GivenTheConnectionHasNoEmbeddingCapability()
        {
            KnownFacts["m1"] = [MakeFact("f1")];
            KnownFacts["m2"] = [MakeFact("f2")];
        }

        [Fact] // MF-5 AC8
        public async Task RequestingEmbedding_ReturnsNoItems()
            => ItemsOf(await GetByIdAsync("Embedding")).ShouldBeEmpty();
    }

    public class GivenTheConnectionDoesNotListTheModel : ConnectionScenario
    {
        public GivenTheConnectionDoesNotListTheModel()
        {
            KnownFacts["m1"] = [MakeFact("f1")];
            KnownFacts["ghost"] = [MakeFact("g1")];
        }

        [Fact] // MF-5 AC9
        public async Task RequestingAnUnlistedModel_ReturnsNoItems()
            => ItemsOf(await GetByIdAsync("Chat", "ghost")).ShouldBeEmpty();
    }

    public class GivenModelListingThrows : ConnectionScenario
    {
        public GivenModelListingThrows()
        {
            KnownFacts["m1"] = [MakeFact("f1")];
            ChatCapability
                .Setup(x => x.GetModelsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Provider listing failed"));
        }

        [Fact] // MF-5 AC10
        public async Task Requesting_ReturnsNoItems()
            => ItemsOf(await GetByIdAsync("Chat")).ShouldBeEmpty();

        [Fact] // MF-5 AC11
        public async Task Requesting_LogsAWarning()
        {
            await GetByIdAsync("Chat");

            Logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }

    public class GivenNoConnectionMatchesAndNoCapabilityIsGiven : ConnectionScenario
    {
        [Fact]
        public async Task Requesting_Returns400()
            => (await GetAsync(new IdOrAlias("nope"), capability: null)).ShouldBeOfType<BadRequestObjectResult>();
    }

    public class GivenTheCapabilityIsDisabled : ConnectionScenario
    {
        public GivenTheCapabilityIsDisabled()
        {
            KnownFacts["m1"] = [MakeFact("f1")];
            ExperimentalFeatures.Setup(x => x.IsCapabilityEnabled(AICapability.Chat)).Returns(false);
        }

        [Fact]
        public async Task Requesting_Returns200()
            => (await GetByIdAsync("Chat")).ShouldBeOfType<OkObjectResult>();

        [Fact]
        public async Task Requesting_ReturnsNoItems()
            => ItemsOf(await GetByIdAsync("Chat")).ShouldBeEmpty();
    }

    public class GivenModelListingTimesOutWithoutTheCallerCancelling : ConnectionScenario
    {
        public GivenModelListingTimesOutWithoutTheCallerCancelling()
        {
            KnownFacts["m1"] = [MakeFact("f1")];
            ChatCapability
                .Setup(x => x.GetModelsAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new TaskCanceledException("HttpClient timeout"));
        }

        [Fact]
        public async Task Requesting_Returns200()
            => (await GetByIdAsync("Chat")).ShouldBeOfType<OkObjectResult>();

        [Fact]
        public async Task Requesting_ReturnsNoItems()
            => ItemsOf(await GetByIdAsync("Chat")).ShouldBeEmpty();

        [Fact]
        public async Task Requesting_LogsAWarning()
        {
            await GetByIdAsync("Chat");

            Logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }

    public class GivenTheProviderListsAModelTwice : ConnectionScenario
    {
        public GivenTheProviderListsAModelTwice()
        {
            KnownFacts["m1"] = [MakeFact("f1")];
            ChatCapability
                .Setup(x => x.GetModelsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<AIModelDescriptor>
                {
                    new(new AIModelRef(ProviderId, "m1"), "Model 1"),
                    new(new AIModelRef(ProviderId, "m1"), "Model 1 again"),
                });
        }

        [Fact]
        public async Task Requesting_ReturnsOneItem()
            => ItemsOf(await GetByIdAsync("Chat")).Count.ShouldBe(1);
    }

    public class GivenTheControllerType
    {
        [Fact] // MF-5 AC12
        public void RequiresAISectionAccess()
            => typeof(ModelFactsConnectionController)
                .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                .Select(a => a.Policy)
                .ShouldContain(AIAuthorizationPolicies.SectionAccessAI);
    }
}
