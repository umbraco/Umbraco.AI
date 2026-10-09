#if MODEL_FACTS_PENDING // Pending: T4 — remove this guard (and the matching #endif) in the commit that makes these specs pass.
// MF-1: Package developers can supply facts about models (AC2).
// Real entry point: core's own registration (AddUmbracoAICore, which AddUmbracoAI calls) on a real
// UmbracoBuilder, registered with UmbracoBuilder.Build() and resolved from the built service provider.
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.ModelFacts;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Tests.Unit.ModelFacts;

public class AIModelFactProviderDefaultRegistrationTests
{
    // ---------------------------------------------------------------- Happy path

    public class GivenADefaultCoreSetup : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public GivenADefaultCoreSetup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            var builder = new UmbracoBuilder(
                services,
                new ConfigurationBuilder().Build(),
                new TypeLoader(Mock.Of<ITypeFinder>(), NullLogger<TypeLoader>.Instance));

            builder.AddUmbracoAICore();

            builder.Build();
            _serviceProvider = services.BuildServiceProvider();
        }

        public void Dispose() => _serviceProvider.Dispose();

        [Fact] // MF-1 AC2
        public void TheFirstProviderIsTheBuiltInMetadataProvider()
            => _serviceProvider.GetRequiredService<AIModelFactProviderCollection>()
                .First()
                .ShouldBeOfType<AIMetadataModelFactProvider>();
    }

    // No sad path: AC2 is a single registration criterion.
}
#endif
