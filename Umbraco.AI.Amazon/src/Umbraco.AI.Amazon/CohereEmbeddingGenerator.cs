using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Amazon;

/// <summary>
/// <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/> for Cohere Embed models on Amazon Bedrock.
/// </summary>
/// <remarks>
/// AWSSDK.Extensions.Bedrock.MEAI's built-in generator only speaks the Amazon Titan protocol
/// (<c>{ "inputText": "..." } / { "embedding": [...] }</c>). Cohere Embed v3 and v4 require
/// <c>{ "texts": [...], "input_type": "...", "embedding_types": [...] }</c> and respond with
/// <c>{ "embeddings": { "float": [[...]] } }</c>, so we implement the protocol ourselves.
/// </remarks>
internal sealed class CohereEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private const string ProviderName = "aws.bedrock";
    private const string DefaultInputType = "search_document";

    /// <summary>
    /// Override key in <see cref="EmbeddingGenerationOptions.AdditionalProperties"/> to specify
    /// the Cohere <c>input_type</c> (e.g. <c>search_query</c> for queries vs <c>search_document</c>
    /// for indexed content).
    /// </summary>
    private const string InputTypeOption = "input_type";

    private readonly IAmazonBedrockRuntime _runtime;
    private readonly string _modelId;
    private readonly EmbeddingGeneratorMetadata _metadata;

    public CohereEmbeddingGenerator(IAmazonBedrockRuntime runtime, string modelId)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);

        _runtime = runtime;
        _modelId = modelId;
        _metadata = new EmbeddingGeneratorMetadata(ProviderName, defaultModelId: modelId);
    }

    public void Dispose()
    {
        // Runtime is owned by the caller.
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        if (serviceKey is not null)
        {
            return null;
        }

        if (serviceType == typeof(EmbeddingGeneratorMetadata))
        {
            return _metadata;
        }

        if (serviceType.IsInstanceOfType(_runtime))
        {
            return _runtime;
        }

        if (serviceType.IsInstanceOfType(this))
        {
            return this;
        }

        return null;
    }

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);

        var texts = values.ToList();
        if (texts.Count == 0)
        {
            return [];
        }

        var requestBody = new CohereEmbedRequest
        {
            Texts = texts,
            InputType = ResolveInputType(options),
            EmbeddingTypes = ["float"]
        };

        var request = new InvokeModelRequest
        {
            ModelId = options?.ModelId ?? _modelId,
            Accept = "application/json",
            ContentType = "application/json",
            Body = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(requestBody, CohereJsonContext.Default.CohereEmbedRequest))
        };

        var response = await _runtime.InvokeModelAsync(request, cancellationToken);

        var cohereResponse = JsonSerializer.Deserialize(response.Body, CohereJsonContext.Default.CohereEmbedResponse)
            ?? throw new InvalidOperationException("Cohere embedding response was empty.");

        var vectors = cohereResponse.Embeddings?.Float;
        if (vectors is null)
        {
            throw new InvalidOperationException(
                "Cohere embedding response did not contain float embeddings. " +
                "Check that the selected model supports the 'float' embedding type.");
        }

        var result = new GeneratedEmbeddings<Embedding<float>>(vectors.Count);
        foreach (var vector in vectors)
        {
            result.Add(new Embedding<float>(vector));
        }

        return result;
    }

    private static string ResolveInputType(EmbeddingGenerationOptions? options)
    {
        if (options?.AdditionalProperties is { } props
            && props.TryGetValue(InputTypeOption, out var value)
            && value is string str
            && !string.IsNullOrWhiteSpace(str))
        {
            return str;
        }

        return DefaultInputType;
    }

    internal sealed class CohereEmbedRequest
    {
        [JsonPropertyName("texts")]
        public List<string>? Texts { get; set; }

        [JsonPropertyName("input_type")]
        public string? InputType { get; set; }

        [JsonPropertyName("embedding_types")]
        public List<string>? EmbeddingTypes { get; set; }
    }

    internal sealed class CohereEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public CohereEmbedTypes? Embeddings { get; set; }
    }

    internal sealed class CohereEmbedTypes
    {
        [JsonPropertyName("float")]
        public List<float[]>? Float { get; set; }
    }
}

[JsonSerializable(typeof(CohereEmbeddingGenerator.CohereEmbedRequest))]
[JsonSerializable(typeof(CohereEmbeddingGenerator.CohereEmbedResponse))]
internal sealed partial class CohereJsonContext : JsonSerializerContext;
