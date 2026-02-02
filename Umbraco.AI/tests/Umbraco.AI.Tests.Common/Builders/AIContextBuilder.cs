using Umbraco.AI.Core.Contexts;

namespace Umbraco.AI.Tests.Common.Builders;

/// <summary>
/// Fluent builder for creating <see cref="AIContext"/> instances in tests.
/// </summary>
public class AIContextBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _alias = "test-context";
    private string _name = "Test Context";
    private DateTime _dateCreated = DateTime.UtcNow;
    private DateTime _dateModified = DateTime.UtcNow;
    private IList<AIContextResource> _resources = new List<AIContextResource>();

    public AIContextBuilder WithId(Guid id)
    {
        _id = id;
        return this;
    }

    public AIContextBuilder WithAlias(string alias)
    {
        _alias = alias;
        return this;
    }

    public AIContextBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public AIContextBuilder WithDateCreated(DateTime dateCreated)
    {
        _dateCreated = dateCreated;
        return this;
    }

    public AIContextBuilder WithDateModified(DateTime dateModified)
    {
        _dateModified = dateModified;
        return this;
    }

    public AIContextBuilder WithResources(params AIContextResource[] resources)
    {
        _resources = resources.ToList();
        return this;
    }

    public AIContextBuilder WithResources(IEnumerable<AIContextResource> resources)
    {
        _resources = resources.ToList();
        return this;
    }

    public AIContextBuilder AddResource(AIContextResource resource)
    {
        _resources.Add(resource);
        return this;
    }

    public AIContextBuilder AddResource(AIContextResourceBuilder resourceBuilder)
    {
        _resources.Add(resourceBuilder.Build());
        return this;
    }

    public AIContext Build()
    {
        return new AIContext
        {
            Id = _id,
            Alias = _alias,
            Name = _name,
            DateCreated = _dateCreated,
            DateModified = _dateModified,
            Resources = _resources
        };
    }

    public static implicit operator AIContext(AIContextBuilder builder) => builder.Build();
}
