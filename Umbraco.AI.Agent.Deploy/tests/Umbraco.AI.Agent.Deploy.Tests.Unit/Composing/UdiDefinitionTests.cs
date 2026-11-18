using System.Linq;
using System.Reflection;
using Shouldly;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Deploy;
using Xunit;

namespace Umbraco.AI.Agent.Deploy.Tests.Unit.Composing;

public class UdiDefinitionTests
{
    // Deploy registers the UDI types from the UdiDefinition attributes on the public service connectors
    [Theory]
    [InlineData(UmbracoAIAgentConstants.UdiEntityType.Agent)]
    public void Service_Connector_Declares_Udi_Type(string entityType)
    {
        UdiDefinitionAttribute[] udiDefinitions = typeof(UmbracoAIAgentConstants).Assembly.GetTypes()
            .Where(x => x is { IsPublic: true, IsAbstract: false } && typeof(IServiceConnector).IsAssignableFrom(x))
            .SelectMany(x => x.GetCustomAttributes<UdiDefinitionAttribute>(false))
            .Where(x => x.EntityType == entityType)
            .ToArray();

        udiDefinitions.ShouldHaveSingleItem().UdiType.ShouldBe(UdiType.GuidUdi);
    }
}
