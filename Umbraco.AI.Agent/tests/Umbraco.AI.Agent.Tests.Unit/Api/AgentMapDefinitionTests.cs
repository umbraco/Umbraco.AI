using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Mapping;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Strings;
using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Api;

/// <summary>
/// Tests for <see cref="AgentMapDefinition"/>'s handling of starter prompts. A silent drop here is
/// the most likely failure mode, and <see cref="AgentItemResponseModel"/> is what later phases
/// (rendering the chips in chat) read.
/// </summary>
public class AgentMapDefinitionTests
{
    private readonly UmbracoMapper _mapper;

    public AgentMapDefinitionTests()
    {
        _mapper = new UmbracoMapper(
            new MapDefinitionCollection(() => new IMapDefinition[]
            {
                new AgentMapDefinition(Mock.Of<IShortStringHelper>())
            }),
            Mock.Of<ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance);
    }

    [Fact]
    public void CreateRequest_WithStarterPrompts_SurvivesMapToDomain()
    {
        var request = new CreateAgentRequestModel
        {
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = "standard",
            StarterPrompts =
            [
                new AIStarterPromptModel { Prompt = "First starter" },
                new AIStarterPromptModel { Prompt = "Second starter" }
            ]
        };

        var agent = _mapper.Map<AIAgent>(request);

        agent.ShouldNotBeNull();
        agent!.StarterPrompts.Select(p => p.Prompt).ShouldBe(["First starter", "Second starter"]);
    }

    [Fact]
    public void CreateRequest_WithNoStarterPrompts_MapsToEmptyList()
    {
        var request = new CreateAgentRequestModel
        {
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = "standard"
        };

        var agent = _mapper.Map<AIAgent>(request);

        agent.ShouldNotBeNull();
        agent!.StarterPrompts.ShouldBeEmpty();
    }

    [Fact]
    public void Domain_WithStarterPrompts_SurvivesMapToResponseModel()
    {
        var agent = new AIAgent
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            StarterPrompts = [new AIStarterPrompt { Prompt = "First starter" }]
        };

        var response = _mapper.Map<AgentResponseModel>(agent);

        response.ShouldNotBeNull();
        response!.StarterPrompts.Select(p => p.Prompt).ShouldBe(["First starter"]);
    }

    [Fact]
    public void Domain_WithStarterPrompts_SurvivesMapToItemResponseModel()
    {
        var agent = new AIAgent
        {
            Id = Guid.NewGuid(),
            Alias = "test-agent",
            Name = "Test Agent",
            AgentType = AIAgentType.Standard,
            StarterPrompts =
            [
                new AIStarterPrompt { Prompt = "First starter" },
                new AIStarterPrompt { Prompt = "Second starter" }
            ]
        };

        var itemResponse = _mapper.Map<AgentItemResponseModel>(agent);

        itemResponse.ShouldNotBeNull();
        itemResponse!.StarterPrompts.Select(p => p.Prompt).ShouldBe(["First starter", "Second starter"]);
    }
}
