// MF-1: Package developers can supply facts about models.
// Real entry point: core's own registration (AddUmbracoAICore) on a real UmbracoBuilder, resolved after Build().
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.ModelFacts;
using Umbraco.AI.Extensions;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Tests.Unit.ModelFacts;

public class AIModelFactServiceRegistrationTests
{
    // ---------------------------------------------------------------- Happy path

    public class GivenADefaultCoreSetup : IDisposable
    {
        private readonly ServiceCollection _services = new();

        public GivenADefaultCoreSetup()
        {
            _services.AddLogging();
            var builder = new UmbracoBuilder(
                _services,
                new ConfigurationBuilder().Build(),
                new TypeLoader(Mock.Of<ITypeFinder>(), NullLogger<TypeLoader>.Instance));

            builder.AddUmbracoAICore();
            builder.Build();
        }

        public void Dispose() { }

        [Fact]
        public void TheServiceIsRegistered()
            => _services.ShouldContain(d => d.ServiceType == typeof(IAIModelFactService));
    }

    // No sad path: a single registration criterion.
}
