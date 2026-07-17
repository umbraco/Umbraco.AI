using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.ResourceTypes;

namespace Umbraco.AI.Tests.Unit.Contexts;

public class AIContextProcessorTests
{
    private static AIContextProcessor CreateProcessor()
        => new(new AIContextResourceTypeCollection(() => []));

    private static AIResolvedResource OnDemand(string name, string contextName, string? contextDescription = null, string? description = null)
        => new()
        {
            Id = Guid.NewGuid(),
            ResourceTypeId = "text",
            Name = name,
            Description = description,
            InjectionMode = AIContextResourceInjectionMode.OnDemand,
            Source = "TestResolver",
            ContextName = contextName,
            ContextDescription = contextDescription
        };

    [Fact]
    public async Task ProcessContextForLlmAsync_GroupsOnDemandResourcesByContext()
    {
        var context = new AIResolvedContext
        {
            OnDemandResources =
            [
                OnDemand("Goals", "Umbraco Engage", "Marketing suite background", "How goals work"),
                OnDemand("Segments", "Umbraco Engage", "Marketing suite background"),
                OnDemand("Discounts", "Umbraco Commerce", "Commerce background")
            ]
        };

        var result = await CreateProcessor().ProcessContextForLlmAsync(context);

        // A group heading + description per context.
        result.ShouldContain("### Umbraco Engage");
        result.ShouldContain("Marketing suite background");
        result.ShouldContain("### Umbraco Commerce");
        result.ShouldContain("Commerce background");

        // Items still list name, id and their own description.
        result.ShouldContain("- **Goals**");
        result.ShouldContain("How goals work");
        result.ShouldContain("- **Discounts**");

        // Engage's heading precedes its items, which precede the Commerce heading (stable grouping).
        result.IndexOf("### Umbraco Engage").ShouldBeLessThan(result.IndexOf("- **Goals**"));
        result.IndexOf("- **Segments**").ShouldBeLessThan(result.IndexOf("### Umbraco Commerce"));
    }

    [Fact]
    public async Task ProcessContextForLlmAsync_OmitsContextDescriptionHeadingLineWhenNull()
    {
        var context = new AIResolvedContext
        {
            OnDemandResources = [OnDemand("Goals", "Umbraco Engage")]
        };

        var result = await CreateProcessor().ProcessContextForLlmAsync(context);

        result.ShouldContain("### Umbraco Engage");
        result.ShouldContain("- **Goals**");
    }
}
