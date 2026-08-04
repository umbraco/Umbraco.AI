using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Mapping;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Mapping;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Api.Management.Conversations;

/// <summary>
/// Covers the create mapping carrying a draft's own contexts and resources, so the client can persist an
/// unsaved conversation's attachments in the same request that creates it.
/// </summary>
public class ConversationMapDefinitionTests
{
    private readonly UmbracoMapper _mapper;

    public ConversationMapDefinitionTests() =>
        // ProjectMapDefinition is required too: the ContextResourceModel -> AIAttachedResource map lives
        // there, and MapEnumerable throws without it.
        _mapper = new UmbracoMapper(
            new MapDefinitionCollection(() => new IMapDefinition[]
            {
                new ConversationMapDefinition(),
                new ProjectMapDefinition()
            }),
            Mock.Of<ICoreScopeProvider>(),
            NullLogger<UmbracoMapper>.Instance);

    [Fact]
    public void MapFromCreate_CarriesContextIdsInOrder()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var model = new CreateConversationRequestModel { ContextIds = [first, second] };

        var result = _mapper.Map<AIConversation>(model);

        result.ShouldNotBeNull();
        result!.ContextIds.ShouldBe([first, second]);
    }

    [Fact]
    public void MapFromCreate_CarriesResources()
    {
        var resourceId = Guid.NewGuid();
        var settings = new { text = "hello" };
        var model = new CreateConversationRequestModel
        {
            Resources =
            [
                new ContextResourceModel
                {
                    Id = resourceId,
                    ResourceTypeId = "text",
                    Name = "Tone of voice",
                    Description = "How to write",
                    SortOrder = 3,
                    Settings = settings,
                    InjectionMode = nameof(AIContextResourceInjectionMode.OnDemand)
                }
            ]
        };

        var result = _mapper.Map<AIConversation>(model);

        result.ShouldNotBeNull();
        AIAttachedResource resource = result!.Resources.ShouldHaveSingleItem();
        resource.Id.ShouldBe(resourceId);
        resource.ResourceTypeId.ShouldBe("text");
        resource.Name.ShouldBe("Tone of voice");
        resource.Description.ShouldBe("How to write");
        resource.SortOrder.ShouldBe(3);
        resource.Settings.ShouldBeSameAs(settings);
        resource.InjectionMode.ShouldBe(AIContextResourceInjectionMode.OnDemand);
    }

    [Fact]
    public void MapFromCreate_WithNoContextsOrResources_MapsEmptyNotNull()
    {
        var result = _mapper.Map<AIConversation>(new CreateConversationRequestModel());

        result.ShouldNotBeNull();
        result!.ContextIds.ShouldBeEmpty();
        result.Resources.ShouldBeEmpty();
    }

    [Fact]
    public void MapFromCreate_StillMapsProjectTitleAgentAndProfile()
    {
        var projectId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var model = new CreateConversationRequestModel
        {
            ProjectId = projectId,
            Title = "Draft title",
            AgentIdOrAlias = "content-assistant",
            ProfileId = profileId
        };

        var result = _mapper.Map<AIConversation>(model);

        result.ShouldNotBeNull();
        result!.ProjectId.ShouldBe(projectId);
        result.Title.ShouldBe("Draft title");
        result.AgentIdOrAlias.ShouldBe("content-assistant");
        result.ProfileId.ShouldBe(profileId);
    }

    [Fact]
    public void MapFromUpdate_CarriesTheSameContextsAndResourcesAsCreate()
    {
        var contextId = Guid.NewGuid();
        var resource = new ContextResourceModel { ResourceTypeId = "text", Name = "Notes" };

        var created = _mapper.Map<AIConversation>(new CreateConversationRequestModel
        {
            ContextIds = [contextId],
            Resources = [resource]
        })!;
        var updated = _mapper.Map<AIConversation>(new UpdateConversationRequestModel
        {
            ContextIds = [contextId],
            Resources = [resource]
        })!;

        updated.ContextIds.ShouldBe(created.ContextIds);
        updated.Resources.Single().ResourceTypeId.ShouldBe(created.Resources.Single().ResourceTypeId);
        updated.Resources.Single().Name.ShouldBe(created.Resources.Single().Name);
    }
}
