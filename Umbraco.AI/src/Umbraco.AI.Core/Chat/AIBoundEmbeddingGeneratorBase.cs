using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Chat;

/// <summary>
/// Base class for generator decorators that bind context to requests.
/// </summary>
/// <typeparam name="TInput"></typeparam>
/// <typeparam name="TEmbedding"></typeparam>
/// <remarks>
/// Provides virtual methods to transform values and options before delegation.
/// Subclasses override <see cref="TransformValues"/> and/or <see cref="TransformOptions"/>
/// to customize behavior.
/// </remarks>
public class AIBoundEmbeddingGeneratorBase<TInput, TEmbedding> : IEmbeddingGenerator<TInput, TEmbedding>
    where TEmbedding : Embedding
{
    /// <summary>
    /// Gets the inner chat client that this decorator delegates to.
    /// </summary>
    protected IEmbeddingGenerator<TInput, TEmbedding> InnerGenerator { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AIBoundEmbeddingGeneratorBase{TInput,TEmbedding}"/> class.
    /// </summary>
    /// <param name="innerGenerator"></param>
    /// <exception cref="ArgumentNullException"></exception>
    protected AIBoundEmbeddingGeneratorBase(IEmbeddingGenerator<TInput, TEmbedding> innerGenerator)
    {
        InnerGenerator = innerGenerator ?? throw new ArgumentNullException(nameof(innerGenerator));
    }

    /// <inheritdoc />
    public virtual Task<GeneratedEmbeddings<TEmbedding>> GenerateAsync(IEnumerable<TInput> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default) =>
        InnerGenerator.GenerateAsync(
            TransformValues(values), 
            TransformOptions(options), 
            cancellationToken);

    /// <inheritdoc />
    public virtual object? GetService(Type serviceType, object? serviceKey = null)
    {
        // Return self if this exact type is requested
        if (serviceType == GetType())
        {
            return this;
        }

        // Delegate to inner client for other services
        return InnerGenerator.GetService(serviceType, serviceKey);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (InnerGenerator is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Transforms the values before passing to the inner generator.
    /// </summary>
    /// <param name="values">The original values.</param>
    /// <returns>The transformed values.</returns>
    protected virtual IEnumerable<TInput> TransformValues(IEnumerable<TInput> values)
        => values;

    /// <summary>
    /// Transforms the generator options before passing to the inner generator.
    /// </summary>
    /// <param name="options">The original generator options.</param>
    /// <returns>The transformed generator options.</returns>
    protected virtual EmbeddingGenerationOptions? TransformOptions(EmbeddingGenerationOptions? options)
        => options;
}