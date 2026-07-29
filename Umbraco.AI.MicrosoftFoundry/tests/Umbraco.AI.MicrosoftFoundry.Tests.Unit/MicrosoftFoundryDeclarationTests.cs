using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Models;
using Umbraco.AI.MicrosoftFoundry.Tests.Unit.Fakes;

// The deployments response and its entries are internal to the provider package, reached here through
// InternalsVisibleTo. Same for the cache-key prefix and MapDeployments.

namespace Umbraco.AI.MicrosoftFoundry.Tests.Unit;

/// <summary>
/// What the capabilities declare per model, which is both what the profile editor renders from and what the
/// core capability bases strip from a request. Going through the real capability rather than the predicate
/// pins the wiring: a capability that stopped overriding <c>GetSettingsSupport</c>, or a provider that
/// stopped carrying what a deployment fronts, fails here.
/// </summary>
public class MicrosoftFoundryDeclarationTests
{
    private const string SamplingGroup = "temperature,topP,topK,frequencyPenalty,presencePenalty";

    [Theory]
    [InlineData("o3")]
    [InlineData("gpt-5")]
    [InlineData("claude-opus-4-8")]
    public void ChatGetSettingsSupport_ModelRejectingSamplingParameters_DeclaresTheSamplingGroup(string modelId)
    {
        var metadata = CreateChatCapability(CreateProvider()).GetSettingsSupport(modelId).ToMetadata();

        metadata[AIModelMetadataKeys.ProfileSettingsUnsupported].ShouldBe(SamplingGroup);
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-35-turbo")]
    [InlineData("mistral-large-2411")]
    [InlineData("prod-chat-1")]
    public void ChatGetSettingsSupport_ModelAcceptingSamplingParameters_DeclaresNothing(string modelId)
    {
        var metadata = CreateChatCapability(CreateProvider()).GetSettingsSupport(modelId).ToMetadata();

        metadata.ShouldBeEmpty();
    }

    [Fact]
    public void ChatGetSettingsSupport_DeploymentFrontingARestrictedModel_DeclaresFromTheDeployment()
    {
        // The failure this fix exists for: the profile carries a deployment name that reveals nothing, so
        // without the deployment's metadata the declaration would be empty and a temperature would go out
        // to a model that rejects it.
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = CreateProvider(cache);
        PrimeModelInfo(cache, new MicrosoftFoundryModelInfo
        {
            Id = "prod-chat",
            ModelName = "o3",
            ModelVersion = "2025-04-16",
            ModelPublisher = "OpenAI",
        });

        var metadata = CreateChatCapability(provider).GetSettingsSupport("prod-chat").ToMetadata();

        metadata[AIModelMetadataKeys.ProfileSettingsUnsupported].ShouldBe(SamplingGroup);
    }

    [Fact]
    public void ChatGetSettingsSupport_DeploymentFrontingAnAcceptingModel_DeclaresNothing()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = CreateProvider(cache);
        PrimeModelInfo(cache, new MicrosoftFoundryModelInfo
        {
            Id = "prod-chat",
            ModelName = "gpt-4o",
            ModelPublisher = "OpenAI",
        });

        CreateChatCapability(provider).GetSettingsSupport("prod-chat").ToMetadata().ShouldBeEmpty();
    }

    [Fact]
    public void ChatGetSettingsSupport_ListingNeverFetched_FallsBackToTheModelId()
    {
        // Nothing cached, so the deployment name is all there is. It reads as no known restriction, which
        // is today's behaviour rather than a new silent drop.
        CreateChatCapability(CreateProvider())
            .GetSettingsSupport("prod-chat")
            .ToMetadata()
            .ShouldBeEmpty();
    }

    [Fact]
    public void EmbeddingGetSettingsSupport_ModelRejectingDimensions_DeclaresDimensions()
    {
        var metadata = CreateEmbeddingCapability(CreateProvider())
            .GetSettingsSupport("text-embedding-ada-002")
            .ToMetadata();

        metadata[AIModelMetadataKeys.ProfileSettingsUnsupported].ShouldBe("dimensions");
    }

    [Theory]
    [InlineData("text-embedding-3-small")]
    [InlineData("cohere-embed-v3-english")]
    public void EmbeddingGetSettingsSupport_ModelAcceptingDimensions_DeclaresNothing(string modelId)
    {
        var metadata = CreateEmbeddingCapability(CreateProvider()).GetSettingsSupport(modelId).ToMetadata();

        metadata.ShouldBeEmpty();
    }

    [Fact]
    public void EmbeddingGetSettingsSupport_DeploymentFrontingAda002_DeclaresFromTheDeployment()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var provider = CreateProvider(cache);
        PrimeModelInfo(cache, new MicrosoftFoundryModelInfo
        {
            Id = "emb-prod",
            ModelName = "text-embedding-ada-002",
            ModelPublisher = "OpenAI",
        });

        var metadata = CreateEmbeddingCapability(provider).GetSettingsSupport("emb-prod").ToMetadata();

        metadata[AIModelMetadataKeys.ProfileSettingsUnsupported].ShouldBe("dimensions");
    }

    [Fact]
    public void MapDeployments_CarriesWhatEachDeploymentFronts()
    {
        // The mapping that used to keep only the deployment name. Without these three fields every
        // declaration above would fall back to the name.
        var mapped = MicrosoftFoundryProvider.MapDeployments(new MicrosoftFoundryDeploymentsResponse
        {
            Value =
            [
                new MicrosoftFoundryDeploymentInfo
                {
                    Name = "prod-chat",
                    ModelName = "o3",
                    ModelVersion = "2025-04-16",
                    ModelPublisher = "OpenAI",
                },
            ],
        });

        var model = mapped.ShouldHaveSingleItem();
        model.Id.ShouldBe("prod-chat");
        model.ModelName.ShouldBe("o3");
        model.ModelVersion.ShouldBe("2025-04-16");
        model.ModelPublisher.ShouldBe("OpenAI");
    }

    [Fact]
    public void MapDeployments_NoDeployments_ReturnsEmpty()
    {
        MicrosoftFoundryProvider.MapDeployments(new MicrosoftFoundryDeploymentsResponse()).ShouldBeEmpty();
        MicrosoftFoundryProvider.MapDeployments(null).ShouldBeEmpty();
    }

    /// <summary>
    /// Puts a listing entry where <c>TryGetModelInfo</c> reads it, standing in for a completed model
    /// listing without needing Entra ID auth and the deployments endpoint.
    /// </summary>
    private static void PrimeModelInfo(IMemoryCache cache, MicrosoftFoundryModelInfo model)
        => cache.Set(MicrosoftFoundryProvider.ModelInfoCacheKeyPrefix + model.Id, model);

    private static MicrosoftFoundryChatCapability CreateChatCapability(MicrosoftFoundryProvider provider)
        => new(provider, NullLogger<MicrosoftFoundryChatCapability>.Instance);

    private static MicrosoftFoundryEmbeddingCapability CreateEmbeddingCapability(MicrosoftFoundryProvider provider)
        => new(provider, NullLogger<MicrosoftFoundryEmbeddingCapability>.Instance);

    private static MicrosoftFoundryProvider CreateProvider(IMemoryCache? cache = null)
        => new(
            new FakeProviderInfrastructure(),
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            new UnusedHttpClientFactory(),
            NullLogger<MicrosoftFoundryProvider>.Instance);
}
