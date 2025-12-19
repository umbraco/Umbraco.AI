using Umbraco.Ai.Agent.Core.Agents;
using Umbraco.Ai.Agent.Web.Api.Management.Agent.Models;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Agent.Web.Api.Management.Agent.Mapping;

/// <summary>
/// UmbracoMapper definitions for agent execution models.
/// </summary>
public class AgentExecutionMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Request mapping (request model -> domain)
        // Using factory-only pattern since init-only properties must be set at construction
        mapper.Define<AgentExecutionRequestModel, AiAgentExecutionRequest>(
            CreateExecutionRequestFactory,
            (_, _, _) => { });

        // Response mapping (domain -> response model)
        // Using factory-only pattern since init-only properties must be set at construction
        mapper.Define<AiAgentExecutionResult, AgentExecutionResponseModel>(
            CreateExecutionResponseFactory,
            (_, _, _) => { });
    }

    // Umbraco.Code.MapAll
    private static AiAgentExecutionRequest CreateExecutionRequestFactory(
        AgentExecutionRequestModel source,
        MapperContext context)
    {
        return new AiAgentExecutionRequest
        {
            EntityId = source.EntityId,
            EntityType = source.EntityType,
            PropertyAlias = source.PropertyAlias,
            Culture = source.Culture,
            Segment = source.Segment,
            LocalContent = source.LocalContent,
            Context = source.Context,
        };
    }

    // Umbraco.Code.MapAll
    private static AgentExecutionResponseModel CreateExecutionResponseFactory(
        AiAgentExecutionResult source,
        MapperContext context)
    {
        return new AgentExecutionResponseModel
        {
            Content = source.Content,
            Usage = source.Usage is not null
                ? new UsageModel
                {
                    InputTokens = source.Usage.InputTokenCount,
                    OutputTokens = source.Usage.OutputTokenCount,
                    TotalTokens = source.Usage.TotalTokenCount
                }
                : null
        };
    }
}
