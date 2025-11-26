using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Models;
using Umbraco.Ai.Core.Providers;

namespace Umbraco.Ai.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAiEmbeddingCapability"/> for use in tests.
/// </summary>
public class FakeEmbeddingCapability : IAiEmbeddingCapability
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly IReadOnlyList<AiModelDescriptor> _models;

    public FakeEmbeddingCapability(
        IEmbeddingGenerator<string, Embedding<float>>? generator = null,
        IReadOnlyList<AiModelDescriptor>? models = null)
    {
        _generator = generator ?? new FakeEmbeddingGenerator();
        _models = models ?? new List<AiModelDescriptor>
        {
            new(new AiModelRef("fake-provider", "fake-embedding-1"), "Fake Embedding Model 1")
        };
    }

    public AiCapability Kind => AiCapability.Embedding;

    public IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(object? settings)
    {
        return _generator;
    }

    public Task<IReadOnlyList<AiModelDescriptor>> GetModelsAsync(object? settings = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_models);
    }
}

/// <summary>
/// Fake implementation of <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for use in tests.
/// </summary>
public class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata { get; } = new("FakeEmbeddingGenerator");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        var embeddings = values.Select(_ => new Embedding<float>(new float[] { 0.1f, 0.2f, 0.3f })).ToList();
        return new GeneratedEmbeddings<Embedding<float>>(embeddings);
    }

    public object? GetService(Type serviceType, object? key = null)
    {
        if (serviceType == typeof(IEmbeddingGenerator<string, Embedding<float>>))
        {
            return this;
        }

        return null;
    }

    public void Dispose()
    {
        // Nothing to dispose
    }
}
