#if MODEL_FACTS_PENDING // Pending: T2 — remove this guard (and the matching #endif) in the commit that makes these specs pass.
// MF-1: Package developers can supply facts about models (AC1).
// Real entry point: providers appended through builder.AIModelFactProviders() on a real UmbracoBuilder,
// registered with UmbracoBuilder.Build() and resolved from the built service provider, as a Composer would.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.ModelFacts;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Tests.Unit.ModelFacts;

public class AIModelFactProviderCollectionTests
{
    // ---------------------------------------------------------------- Happy path

    public class GivenTwoProvidersAppendedViaAIModelFactProviders : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public GivenTwoProvidersAppendedViaAIModelFactProviders()
        {
            var services = new ServiceCollection();
            var builder = new UmbracoBuilder(
                services,
                new ConfigurationBuilder().Build(),
                new TypeLoader(Mock.Of<ITypeFinder>(), NullLogger<TypeLoader>.Instance));

            builder.AIModelFactProviders()
                .Append<FirstFactProvider>()
                .Append<SecondFactProvider>();

            builder.Build();
            _serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose() => _serviceProvider.Dispose();

        [Fact] // MF-1 AC1
        public void TheCollectionContainsBothProvidersInAppendOrder()
            => _serviceProvider.GetRequiredService<AIModelFactProviderCollection>()
                .Select(p => p.GetType())
                .ShouldBe(new[] { typeof(FirstFactProvider), typeof(SecondFactProvider) });
    }

    // No sad path: AC1 is the only criterion for the collection itself.

    public sealed class FirstFactProvider : IAIModelFactProvider
    {
        public TimeSpan CacheDuration => TimeSpan.Zero;

        public Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
            AIModelFactContext context,
            IReadOnlyList<AIModelDescriptor> models,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>>(
                new Dictionary<string, IReadOnlyList<AIModelFact>>());
    }

    public sealed class SecondFactProvider : IAIModelFactProvider
    {
        public TimeSpan CacheDuration => TimeSpan.Zero;

        public Task<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>> GetModelFactsAsync(
            AIModelFactContext context,
            IReadOnlyList<AIModelDescriptor> models,
            CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyDictionary<string, IReadOnlyList<AIModelFact>>>(
                new Dictionary<string, IReadOnlyList<AIModelFact>>());
    }
}
#endif
