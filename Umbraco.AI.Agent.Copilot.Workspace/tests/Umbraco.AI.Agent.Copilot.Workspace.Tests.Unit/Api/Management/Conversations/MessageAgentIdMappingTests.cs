// S8 - Workspace remembers which agent answered (AC3, AC5)
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Umbraco.AI.Agent.Conversations.Core.Conversations;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Mapping;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Conversations.Models;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Mapping;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Scoping;
using Xunit;

namespace Umbraco.AI.Agent.Copilot.Workspace.Tests.Unit.Api.Management.Conversations;

/// <summary>
/// The conversation messages response carries each message's agent ID, so a reopened chat can show
/// which agent wrote each reply.
/// </summary>
/// <remarks>
/// Assumed seam (T16 may adjust it here): <see cref="MessageResponseModel"/> gains <c>Guid? AgentId</c>,
/// mapped from <c>AIMessage.AgentId</c> (added in T14).
/// </remarks>
public class MessageAgentIdMappingTests
{
    private static readonly Guid AgentA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static UmbracoMapper CreateMapper()
        => new(
            new MapDefinitionCollection(() => new IMapDefinition[]
            {
                new ConversationMapDefinition(),
                new ProjectMapDefinition()
            }),
            TestCoreScopeProvider.Create(),
            NullLogger<UmbracoMapper>.Instance);

    public class GivenAnAssistantMessageByAgentA
    {
        private readonly MessageResponseModel? _result;

        public GivenAnAssistantMessageByAgentA()
            => _result = CreateMapper().Map<MessageResponseModel>(
                new AIMessage { Role = "assistant", ContentJson = "{}", AgentId = AgentA });

        // AC3
        [Fact]
        public void CarriesAgentA() => _result!.AgentId.ShouldBe(AgentA);
    }

    public class GivenALegacyMessageWithNoAgent
    {
        private readonly MessageResponseModel? _result;

        public GivenALegacyMessageWithNoAgent()
            => _result = CreateMapper().Map<MessageResponseModel>(
                new AIMessage { Role = "assistant", ContentJson = "{}", AgentId = null });

        // AC5
        [Fact]
        public void CarriesNoAgent() => _result!.AgentId.ShouldBeNull();
    }
}
