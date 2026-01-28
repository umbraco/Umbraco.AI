using Umbraco.Ai.Core.Models;

namespace Umbraco.Ai.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AiModelRef"/> instances in tests.
/// </summary>
public class AiModelRefBuilder
{
    private string _providerId = "test-provider";
    private string _modelId = "test-model";

    public AiModelRefBuilder WithProviderId(string providerId)
    {
        _providerId = providerId;
        return this;
    }

    public AiModelRefBuilder WithModelId(string modelId)
    {
        _modelId = modelId;
        return this;
    }

    public AiModelRef Build()
    {
        return new AiModelRef(_providerId, _modelId);
    }

    public static implicit operator AiModelRef(AiModelRefBuilder builder) => builder.Build();
}
