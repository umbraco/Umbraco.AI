using Umbraco.Ai.Core.EntityAdapter;
using Umbraco.Ai.Core.RequestContext;
using Umbraco.Ai.Prompt.Core.Prompts;
using Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.Ai.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Mapping;

/// <summary>
/// UmbracoMapper definitions for prompt execution models.
/// </summary>
public class PromptExecutionMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Request mapping (request model -> domain)
        // Using factory-only pattern since init-only properties must be set at construction
        mapper.Define<PromptExecutionRequestModel, AiPromptExecutionRequest>(
            CreateExecutionRequestFactory,
            (_, _, _) => { });

        // Response mapping (domain -> response model)
        // Using factory-only pattern since init-only properties must be set at construction
        mapper.Define<AiPromptExecutionResult, PromptExecutionResponseModel>(
            CreateExecutionResponseFactory,
            (_, _, _) => { });
    }

    // Umbraco.Code.MapAll
    private static AiPromptExecutionRequest CreateExecutionRequestFactory(
        PromptExecutionRequestModel source,
        MapperContext context)
    {
        return new AiPromptExecutionRequest
        {
            EntityId = source.EntityId,
            EntityType = source.EntityType,
            PropertyAlias = source.PropertyAlias,
            Culture = source.Culture,
            Segment = source.Segment,
            Context = source.Context?.Select(item => new AiRequestContextItem
            {
                Description = item.Description,
                Value = item.Value
            }).ToList(),
        };
    }

    // Umbraco.Code.MapAll
    private static PromptExecutionResponseModel CreateExecutionResponseFactory(
        AiPromptExecutionResult source,
        MapperContext context)
    {
        return new PromptExecutionResponseModel
        {
            Content = source.Content,
            Usage = source.Usage is not null
                ? new UsageModel
                {
                    InputTokens = source.Usage.InputTokenCount,
                    OutputTokens = source.Usage.OutputTokenCount,
                    TotalTokens = source.Usage.TotalTokenCount
                }
                : null,
            PropertyChanges = source.PropertyChanges?.Select(change => new PropertyChangeModel
            {
                Alias = change.Alias,
                Value = change.Value,
                Culture = change.Culture,
                Segment = change.Segment
            }).ToList()
        };
    }
}
