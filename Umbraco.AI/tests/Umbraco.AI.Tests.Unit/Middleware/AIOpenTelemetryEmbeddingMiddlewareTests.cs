using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Umbraco.AI.Core.Chat.Middleware;
using Umbraco.AI.Core.Telemetry;

namespace Umbraco.AI.Tests.Unit.Middleware;

public class AIOpenTelemetryEmbeddingMiddlewareTests
{
    [Fact]
    public void Apply_ReturnsWrappedGenerator()
    {
        // Arrange
        var innerGenerator = new FakeEmbeddingGenerator();
        var middleware = new AIOpenTelemetryEmbeddingMiddleware(NullLoggerFactory.Instance);

        // Act
        var result = middleware.Apply(innerGenerator);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldNotBeSameAs(innerGenerator);
    }

    [Fact]
    public async Task Apply_WrappedGenerator_DelegatesToInnerGenerator()
    {
        // Arrange
        var innerGenerator = new FakeEmbeddingGenerator();
        var middleware = new AIOpenTelemetryEmbeddingMiddleware(NullLoggerFactory.Instance);
        var wrappedGenerator = middleware.Apply(innerGenerator);

        // Act
        var result = await wrappedGenerator.GenerateAsync(["test input"]);

        // Assert
        result.ShouldNotBeNull();
        innerGenerator.CallCount.ShouldBe(1);
    }

    /// <summary>
    /// Minimal fake embedding generator for testing.
    /// </summary>
    private sealed class FakeEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public int CallCount { get; private set; }

        public EmbeddingGeneratorMetadata Metadata { get; } = new("FakeEmbeddingGenerator");

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            var embeddings = new GeneratedEmbeddings<Embedding<float>>(
                values.Select(_ => new Embedding<float>(new float[] { 0.1f, 0.2f, 0.3f })).ToList());
            return Task.FromResult(embeddings);
        }

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
}
