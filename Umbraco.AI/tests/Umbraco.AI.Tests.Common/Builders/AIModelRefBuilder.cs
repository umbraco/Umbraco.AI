using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AIModelRef"/> instances in tests.
/// </summary>
public class AIModelRefBuilder
{
    private string _providerId = "test-provider";
    private string _modelId = "test-model";

    public AIModelRefBuilder WithProviderId(string providerId)
    {
        _providerId = providerId;
        return this;
    }

    public AIModelRefBuilder WithModelId(string modelId)
    {
        _modelId = modelId;
        return this;
    }

    public AIModelRef Build()
    {
        return new AIModelRef(_providerId, _modelId);
    }

    public static implicit operator AIModelRef(AIModelRefBuilder builder) => builder.Build();
}
