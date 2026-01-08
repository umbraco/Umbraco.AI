using Umbraco.Ai.Core;
using Umbraco.Ai.Core.Context;

namespace Umbraco.Ai.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AiContextResource"/> instances in tests.
/// </summary>
public class AiContextResourceBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _resourceTypeId = "text";
    private string _name = "Test Resource";
    private string? _description;
    private int _sortOrder;
    private string _data = "{}";
    private AiContextResourceInjectionMode _injectionMode = AiContextResourceInjectionMode.Always;

    public AiContextResourceBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AiContextResourceBuilder WithResourceTypeId(string resourceTypeId)
    {
        _resourceTypeId = resourceTypeId;
        return this;
    }

    public AiContextResourceBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AiContextResourceBuilder WithDescription(string? description)
    {
        _description = description;
        return this;
    }

    public AiContextResourceBuilder WithSortOrder(int sortOrder)
    {
        _sortOrder = sortOrder;
        return this;
    }

    public AiContextResourceBuilder WithData(string data)
    {
        _data = data;
        return this;
    }

    public AiContextResourceBuilder WithInjectionMode(AiContextResourceInjectionMode injectionMode)
    {
        _injectionMode = injectionMode;
        return this;
    }

    public AiContextResourceBuilder AsAlwaysInjected()
    {
        _injectionMode = AiContextResourceInjectionMode.Always;
        return this;
    }

    public AiContextResourceBuilder AsOnDemand()
    {
        _injectionMode = AiContextResourceInjectionMode.OnDemand;
        return this;
    }

    public AiContextResourceBuilder AsBrandVoice(string tone = "Professional", string targetAudience = "General")
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

    public AiContextResourceBuilder AsText(string content)
    {
        _resourceTypeId = "text";
        _data = System.Text.Json.JsonSerializer.Serialize(new { content }, Constants.DefaultJsonSerializerOptions);
        return this;
    }

    public AiContextResource Build()
    {
        return new AiContextResource
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

    public static implicit operator AiContextResource(AiContextResourceBuilder builder) => builder.Build();
}
