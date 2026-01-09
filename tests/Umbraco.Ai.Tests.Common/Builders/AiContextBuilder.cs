using Umbraco.Ai.Core.Contexts;

namespace Umbraco.Ai.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AiContext"/> instances in tests.
/// </summary>
public class AiContextBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _alias = "test-context";
    private string _name = "Test Context";
    private DateTime _dateCreated = DateTime.UtcNow;
    private DateTime _dateModified = DateTime.UtcNow;
    private IList<AiContextResource> _resources = new List<AiContextResource>();

    public AiContextBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AiContextBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    public AiContextBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AiContextBuilder WithDateCreated(DateTime dateCreated)
    {
        _dateCreated = dateCreated;
        return this;
    }

    public AiContextBuilder WithDateModified(DateTime dateModified)
    {
        _dateModified = dateModified;
        return this;
    }

    public AiContextBuilder WithResources(params AiContextResource[] resources)
    {
        _resources = resources.ToList();
        return this;
    }

    public AiContextBuilder WithResources(IEnumerable<AiContextResource> resources)
    {
        _resources = resources.ToList();
        return this;
    }

    public AiContextBuilder AddResource(AiContextResource resource)
    {
        _resources.Add(resource);
        return this;
    }

    public AiContextBuilder AddResource(AiContextResourceBuilder resourceBuilder)
    {
        _resources.Add(resourceBuilder.Build());
        return this;
    }

    public AiContext Build()
    {
        return new AiContext
        {
            Id = _id,
            Alias = _alias,
            Name = _name,
            DateCreated = _dateCreated,
            DateModified = _dateModified,
            Resources = _resources
        };
    }

    public static implicit operator AiContext(AiContextBuilder builder) => builder.Build();
}
