#if MODEL_FACTS_PENDING // Pending: T3 — remove this guard (and the matching #endif) in the commit that makes these specs pass.
// MF-1: Package developers can supply facts about models (AC3-AC14).
// AIModelFactService is assumed to take (AIModelFactProviderCollection, IAppPolicyCache,
// IOptions<AIModelFactOptions>, ILogger<AIModelFactService>). The cache is a real ObjectCacheAppCache.
// Fact providers are hand-rolled, one class per provider, because the service keys its cache on the
// provider *type*; two Moq mocks of IAIModelFactProvider would share one proxy type.
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.ModelFacts;
using Umbraco.Cms.Core.Cache;

namespace Umbraco.AI.Tests.Unit.Services;

public class AIModelFactServiceTests
{
    private static readonly Guid ConnectionId = Guid.NewGuid();

    private static readonly AIModelFactContext Context = new()
    {
        ConnectionId = ConnectionId,
        ProviderId = "openrouter",
        Capability = AICapability.Chat,
    };

    private static AIModelDescriptor Model(string modelId) => new(new AIModelRef("openrouter", modelId), modelId);

    private static AIModelFact MakeFact(string key, AIModelFactTone tone = AIModelFactTone.Neutral, string? url = null)
        => new() { Key = key, Label = key, Value = key, Tone = tone, Url = url };

    private static AIModelFactService CreateService(
        IEnumerable<IAIModelFactProvider> providers,
        TimeSpan? providerTimeout = null,
        ILogger<AIModelFactService>? logger = null)
        => new(
            new AIModelFactProviderCollection(() => providers),
            new ObjectCacheAppCache(),
            Options.Create(new AIModelFactOptions { ProviderTimeout = providerTimeout ?? TimeSpan.FromSeconds(2) }),
            logger ?? NullLogger<AIModelFactService>.Instance);

    private static Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetAsync(
        AIModelFactService service,
        params string[] modelIds)
        => service.GetModelFactsAsync(Context, modelIds.Select(Model).ToList(), CancellationToken.None);

    // ---------------------------------------------------------------- Happy path

    public class GivenTwoProvidersWithFactsForTheSameModel
    {
        private readonly AIModelFactService _service = CreateService(
        [
            new ProviderA { Facts = { ["m1"] = [MakeFact("a1")] } },
            new ProviderB { Facts = { ["m1"] = [MakeFact("b1")] } },
        ]);

        [Fact] // MF-1 AC3
        public async Task TheModelsFactsAreAggregatedInRegistrationOrder()
            => (await GetAsync(_service, "m1"))["m1"].Select(f => f.Key).ShouldBe(new[] { "a1", "b1" });
    }

    public class GivenALaterProviderReturnsAWarningFact
    {
        private readonly AIModelFactService _service = CreateService(
        [
            new ProviderA { Facts = { ["m1"] = [MakeFact("a.neutral")] } },
            new ProviderB { Facts = { ["m1"] = [MakeFact("b.warning", AIModelFactTone.Warning)] } },
        ]);

        [Fact] // MF-1 AC4
        public async Task TheWarningFactComesFirst()
            => (await GetAsync(_service, "m1"))["m1"][0].Key.ShouldBe("b.warning");
    }

    public class GivenProvidersReturnFactsForOnlyOneOfTwoModels
    {
        private readonly AIModelFactService _service = CreateService(
        [
            new ProviderA { Facts = { ["m1"] = [MakeFact("a1")] } },
        ]);

        [Fact] // MF-1 AC5
        public async Task TheModelWithNoFactsIsOmitted()
            => (await GetAsync(_service, "m1", "m2")).ShouldNotContainKey("m2");
    }

    public class GivenACachingProviderAlreadyFetchedOneModel
    {
        private readonly ProviderA _provider = new()
        {
            CacheDuration = TimeSpan.FromHours(1),
            Facts = { ["m1"] = [MakeFact("a1")], ["m2"] = [MakeFact("a2")] },
        };

        private readonly AIModelFactService _service;

        public GivenACachingProviderAlreadyFetchedOneModel()
        {
            _service = CreateService([_provider]);
            GetAsync(_service, "m1").GetAwaiter().GetResult();
            _provider.Calls.Clear();
        }

        [Fact] // MF-1 AC6
        public async Task TheProviderIsCalledOnce()
        {
            await GetAsync(_service, "m1", "m2");

            _provider.Calls.Count.ShouldBe(1);
        }

        [Fact] // MF-1 AC6
        public async Task TheProviderIsAskedOnlyForTheUncachedModel()
        {
            await GetAsync(_service, "m1", "m2");

            _provider.Calls[0].ShouldBe(new[] { "m2" });
        }
    }

    public class GivenACachingProviderPreviouslyReturnedNoFactsForAModel
    {
        private readonly ProviderA _provider = new() { CacheDuration = TimeSpan.FromHours(1) };
        private readonly AIModelFactService _service;

        public GivenACachingProviderPreviouslyReturnedNoFactsForAModel()
        {
            _service = CreateService([_provider]);
            GetAsync(_service, "m1").GetAwaiter().GetResult();
            _provider.Calls.Clear();
        }

        [Fact] // MF-1 AC7
        public async Task TheProviderIsNotCalledAgain()
        {
            await GetAsync(_service, "m1");

            _provider.Calls.ShouldBeEmpty();
        }
    }

    public class GivenAProviderWithZeroCacheDuration
    {
        private readonly ProviderA _provider = new()
        {
            CacheDuration = TimeSpan.Zero,
            Facts = { ["m1"] = [MakeFact("a1")] },
        };

        private readonly AIModelFactService _service;

        public GivenAProviderWithZeroCacheDuration() => _service = CreateService([_provider]);

        [Fact] // MF-1 AC8
        public async Task TheProviderIsCalledOnEveryRequest()
        {
            await GetAsync(_service, "m1");
            await GetAsync(_service, "m1");

            _provider.Calls.Count.ShouldBe(2);
        }
    }

    public class GivenARequestForAConnectionAndCapability
    {
        private readonly ProviderA _provider = new();
        private readonly AIModelFactService _service;

        public GivenARequestForAConnectionAndCapability() => _service = CreateService([_provider]);

        [Fact] // MF-1 AC9
        public async Task TheContextCarriesTheConnectionId()
        {
            await GetAsync(_service, "m1");

            _provider.LastContext!.ConnectionId.ShouldBe(ConnectionId);
        }

        [Fact] // MF-1 AC9
        public async Task TheContextCarriesTheProviderId()
        {
            await GetAsync(_service, "m1");

            _provider.LastContext!.ProviderId.ShouldBe("openrouter");
        }

        [Fact] // MF-1 AC9
        public async Task TheContextCarriesTheCapability()
        {
            await GetAsync(_service, "m1");

            _provider.LastContext!.Capability.ShouldBe(AICapability.Chat);
        }
    }

    // ---------------------------------------------------------------- Sad path

    public class GivenOneProviderThrows
    {
        private readonly Mock<ILogger<AIModelFactService>> _logger = new();
        private readonly AIModelFactService _service;

        public GivenOneProviderThrows()
        {
            _logger.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);
            _service = CreateService(
            [
                new ThrowingProvider(),
                new ProviderB { Facts = { ["m1"] = [MakeFact("b1")] } },
            ],
            logger: _logger.Object);
        }

        [Fact] // MF-1 AC10
        public async Task TheOtherProvidersFactsAreStillReturned()
            => (await GetAsync(_service, "m1"))["m1"].Select(f => f.Key).ShouldBe(new[] { "b1" });

        [Fact] // MF-1 AC11
        public async Task AWarningNamingTheThrowingProviderIsLogged()
        {
            await GetAsync(_service, "m1");

            _logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(nameof(ThrowingProvider))),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }
    }

    public class GivenAProviderSlowerThanTheTimeout
    {
        private readonly AIModelFactService _service = CreateService(
            [new SlowProvider { Facts = { ["m1"] = [MakeFact("slow1")] } }],
            providerTimeout: TimeSpan.FromMilliseconds(50));

        [Fact] // MF-1 AC12
        public async Task TheCallCompletesWellUnderTheProvidersDelay()
        {
            var stopwatch = Stopwatch.StartNew();
            await GetAsync(_service, "m1");
            stopwatch.Stop();

            stopwatch.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(2));
        }

        [Fact] // MF-1 AC12
        public async Task TheSlowProvidersFactsAreNotReturned()
            => (await GetAsync(_service, "m1")).ShouldNotContainKey("m1");
    }

    public class GivenAProviderReturnsFactsForAModelThatWasNotRequested
    {
        private readonly AIModelFactService _service = CreateService(
        [
            new ProviderA { Facts = { ["m1"] = [MakeFact("a1")], ["m9"] = [MakeFact("a9")] }, ReturnAllFacts = true },
        ]);

        [Fact] // MF-1 AC13
        public async Task TheUnrequestedModelIsIgnored()
            => (await GetAsync(_service, "m1")).ShouldNotContainKey("m9");
    }

    public class GivenAFactWithAnUnsafeUrl
    {
        private readonly AIModelFactService _service = CreateService(
        [
            new ProviderA { Facts = { ["m1"] = [MakeFact("a1", url: "javascript:alert(1)")] } },
        ]);

        [Fact] // MF-1 AC14
        public async Task TheUrlIsDropped()
            => (await GetAsync(_service, "m1"))["m1"][0].Url.ShouldBeNull();
    }

    // ---------------------------------------------------------------- Test providers

    /// <summary>
    /// A configurable fact provider that records each call. Returns <see cref="Facts"/> for the
    /// requested models only, unless <see cref="ReturnAllFacts"/> is set.
    /// </summary>
    public abstract class RecordingFactProvider : IAIModelFactProvider
    {
        public TimeSpan CacheDuration { get; init; } = TimeSpan.Zero;

        public Dictionary<string, IReadOnlyList<AIModelFact>> Facts { get; } = new();

        public bool ReturnAllFacts { get; init; }

        public List<IReadOnlyList<string>> Calls { get; } = [];

        public AIModelFactContext? LastContext { get; private set; }

        public virtual Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
            AIModelFactContext context,
            IReadOnlyList<AIModelDescriptor> models,
            CancellationToken cancellationToken)
        {
            LastContext = context;
            Calls.Add(models.Select(m => m.Model.ModelId).ToList());

            IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>> result = ReturnAllFacts
                ? Facts
                : Facts
                    .Where(kv => models.Any(m => m.Model.ModelId == kv.Key))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

            return Task.FromResult(result);
        }
    }

    public sealed class ProviderA : RecordingFactProvider;

    public sealed class ProviderB : RecordingFactProvider;

    public sealed class ThrowingProvider : RecordingFactProvider
    {
        public override Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
            AIModelFactContext context,
            IReadOnlyList<AIModelDescriptor> models,
            CancellationToken cancellationToken)
            => throw new InvalidOperationException("Fact provider failure");
    }

    /// <summary>Takes 5 s, honouring cancellation (the linked-token timeout in ARCHITECTURE decision 7).</summary>
    public sealed class SlowProvider : RecordingFactProvider
    {
        public override async Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
            AIModelFactContext context,
            IReadOnlyList<AIModelDescriptor> models,
            CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return await base.GetModelFactsAsync(context, models, cancellationToken);
        }
    }
}
#endif
