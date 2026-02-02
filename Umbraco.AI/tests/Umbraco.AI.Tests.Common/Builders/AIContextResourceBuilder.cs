using Umbraco.AI.Core;
using Umbraco.AI.Core.Contexts;

namespace Umbraco.AI.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AIContextResource"/> instances in tests.
/// </summary>
public class AIContextResourceBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _resourceTypeId = "text";
    private string _name = "Test Resource";
    private string? _description;
    private int _sortOrder;
    private string _data = "{}";
    private AIContextResourceInjectionMode _injectionMode = AIContextResourceInjectionMode.Always;

    public AIContextResourceBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AIContextResourceBuilder WithResourceTypeId(string resourceTypeId)
    {
        _resourceTypeId = resourceTypeId;
        return this;
    }

    public AIContextResourceBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AIContextResourceBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public AIContextResourceBuilder WithSortOrder(int sortOrder)
    {
        _sortOrder = sortOrder;
        return this;
    }

    public AIContextResourceBuilder WithData(string data)
    {
        _data = data;
        return this;
    }

    public AIContextResourceBuilder WithInjectionMode(AIContextResourceInjectionMode injectionMode)
    {
        _injectionMode = injectionMode;
        return this;
    }

    public AIContextResourceBuilder AsAlwaysInjected()
    {
        _injectionMode = AIContextResourceInjectionMode.Always;
        return this;
    }

    public AIContextResourceBuilder AsOnDemand()
    {
        _injectionMode = AIContextResourceInjectionMode.OnDemand;
        return this;
    }

    public AIContextResourceBuilder AsBrandVoice(string tone = "Professional", string targetAudience = "General")
    {
        _resourceTypeId = "brand-voice";
        _data = System.Text.Json.JsonSerializer.Serialize(new
        {
            tone,
            targetAudience,
            styleGuidelines = (string?)null,
            avoidList = Array.Empty<string>()
        },
        Constants.DefaultJsonSerializerOptions);
        return this;
    }

    public AIContextResourceBuilder AsText(string content)
    {
        _resourceTypeId = "text";
        _data = System.Text.Json.JsonSerializer.Serialize(new { content }, Constants.DefaultJsonSerializerOptions);
        return this;
    }

    public AIContextResource Build()
    {
        return new AIContextResource
        {
            Id = _id,
            ResourceTypeId = _resourceTypeId,
            Name = _name,
            Description = _description,
            SortOrder = _sortOrder,
            Data = _data,
            InjectionMode = _injectionMode
        };
    }

    public static implicit operator AIContextResource(AIContextResourceBuilder builder) => builder.Build();
}
