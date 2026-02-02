using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.Tests.Common.Fakes;

/// <summary>
/// Fake implementation of <see cref="IAiEmbeddingCapability"/> for use in tests.
/// </summary>
public class FakeEmbeddingCapability : IAiEmbeddingCapability
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;
    private readonly IReadOnlyList<AIModelDescriptor> _models;

    public FakeEmbeddingCapability(
        IEmbeddingGenerator<string, Embedding<float>>? generator = null,
        IReadOnlyList<AIModelDescriptor>? models = null)
    {
        _generator = generator ?? new FakeEmbeddingGenerator();
        _models = models ?? new List<AIModelDescriptor>
        {
            new(new AIModelRef("fake-provider", "fake-embedding-1"), "Fake Embedding Model 1")
        };
    }

    public AICapability Kind => AICapability.Embedding;

    public IEmbeddingGenerator<string, Embedding<float>> CreateGenerator(object? settings, string? modelId = null)
    {
        return _generator;
    }

    public Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(object? settings = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_models);
    }
}

/// <summary>
/// Fake implementation of <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for use in tests.
/// </summary>
public class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly Func<IEnumerable<string>, EmbeddingGenerationOptions?, CancellationToken, Task<GeneratedEmbeddings<Embedding<float>>>>? _generateHandler;
    private readonly float[] _defaultEmbedding;

    public FakeEmbeddingGenerator(float[]? defaultEmbedding = null)
    {
        _defaultEmbedding = defaultEmbedding ?? [0.1f, 0.2f, 0.3f];
    }

    public FakeEmbeddingGenerator(Func<IEnumerable<string>, EmbeddingGenerationOptions?, CancellationToken, Task<GeneratedEmbeddings<Embedding<float>>>> generateHandler)
    {
        _generateHandler = generateHandler;
        _defaultEmbedding = [0.1f, 0.2f, 0.3f];
    }

    /// <summary>
    /// Gets the list of values that were sent to this generator.
    /// </summary>
    public List<IEnumerable<string>> ReceivedValues { get; } = [];

    /// <summary>
    /// Gets the list of options that were sent to this generator.
    /// </summary>
    public List<EmbeddingGenerationOptions?> ReceivedOptions { get; } = [];

    public EmbeddingGeneratorMetadata Metadata { get; } = new("FakeEmbeddingGenerator");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ReceivedValues.Add(values);
        ReceivedOptions.Add(options);

        if (_generateHandler is not null)
        {
            return await _generateHandler(values, options, cancellationToken);
        }

        await Task.CompletedTask;

        var embeddings = values.Select(_ => new Embedding<float>(_defaultEmbedding)).ToList();
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
