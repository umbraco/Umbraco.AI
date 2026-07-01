#pragma warning disable MEAI001 // IImageGenerator is experimental in M.E.AI
#pragma warning disable UMBRACOAI_IMAGEGEN // Test double for the experimental image-generation capability

using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAIImageGeneratorCapability"/> for use in tests.
/// </summary>
public class FakeImageGeneratorCapability : IAIImageGeneratorCapability
{
    private readonly IImageGenerator _generator;
    private readonly IReadOnlyList<AIModelDescriptor> _models;

    public FakeImageGeneratorCapability(IImageGenerator? generator = null, IReadOnlyList<AIModelDescriptor>? models = null)
    {
        _generator = generator ?? new FakeImageGenerator();
        _models = models ?? new List<AIModelDescriptor>
        {
            new(
                new AIModelRef("fake-provider", "gpt-image-1"),
                "GPT Image 1",
                new Dictionary<string, string>
                {
                    ["image.supportedSizes"] = "1024x1024,1024x1536,1536x1024",
                    ["image.maxEdge"] = "1536",
                }),
        };
    }

    public AICapability Kind => AICapability.ImageGeneration;

    public Task<IImageGenerator> CreateGeneratorAsync(object? settings = null, string? modelId = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_generator);
    }

    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(object? settings = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_models);
    }
}
