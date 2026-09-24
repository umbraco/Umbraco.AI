#pragma warning disable UMBRACOAI_DECISION // Exercises the experimental decision capability

using Moq;
using Umbraco.AI.Core.Decision;
using Umbraco.AI.Core.EditableModels;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.TypeSafe.Tests.Unit.Fakes;

/// <summary>
/// Builds a real <see cref="TypeSafeProvider"/> and gets clients through its real capability, with only the
/// <see cref="HttpMessageHandler"/> faked.
/// </summary>
internal static class TypeSafeTestHost
{
    public const string ApiKey = "test-api-key";

    public const string Endpoint = "https://api.typesafe.ai";

    public static TypeSafeProvider CreateProvider(HttpMessageHandler? handler = null)
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler ?? new ScriptedHttpMessageHandler(ScriptedHttpMessageHandler.Status(System.Net.HttpStatusCode.OK))));

        return new TypeSafeProvider(new ActivatorProviderInfrastructure(), httpClientFactory.Object);
    }

    public static async Task<IAIDecisionClient> CreateClientAsync(HttpMessageHandler handler, string? modelId = "jev-latest")
    {
        var capability = (IAIDecisionCapability)CreateProvider(handler).GetCapability<TypeSafeDecisionCapability>();
        var settings = new TypeSafeProviderSettings { ApiKey = ApiKey, Endpoint = Endpoint };

        return await capability.CreateClientAsync(settings, modelId, default);
    }

    private sealed class ActivatorProviderInfrastructure : IAIProviderInfrastructure
    {
        public IAICapabilityFactory CapabilityFactory { get; } = new ActivatorCapabilityFactory();

        public IAIEditableModelSchemaBuilder SchemaBuilder { get; } = Mock.Of<IAIEditableModelSchemaBuilder>();

        private sealed class ActivatorCapabilityFactory : IAICapabilityFactory
        {
            public TCapability Create<TCapability>(IAIProvider provider)
                where TCapability : class, IAICapability
                => (TCapability)Activator.CreateInstance(typeof(TCapability), provider)!;
        }
    }
}
