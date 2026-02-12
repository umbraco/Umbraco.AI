using Umbraco.AI.Core.RuntimeContext;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.AI.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.AI.Web.Api.Common.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Prompt.Web.Api.Management.Prompt.Mapping;

/// <summary>
/// UmbracoMapper definitions for prompt execution models.
/// </summary>
internal class PromptExecutionMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Request mapping (request model -> domain)
        // Using factory-only pattern since init-only properties must be set at construction
        mapper.Define<PromptExecutionRequestModel, AIPromptExecutionRequest>(
            CreateExecutionRequestFactory,
            (_, _, _) => { });

        // Response mapping (domain -> response model)
        // Using factory-only pattern since init-only properties must be set at construction
        mapper.Define<AIPromptExecutionResult, PromptExecutionResponseModel>(
            CreateExecutionResponseFactory,
            (_, _, _) => { });
    }

    // Umbraco.Code.MapAll
    private static AIPromptExecutionRequest CreateExecutionRequestFactory(
        PromptExecutionRequestModel source,
        MapperContext context)
    {
        return new AIPromptExecutionRequest
        {
            EntityId = source.EntityId,
            EntityType = source.EntityType,
            PropertyAlias = source.PropertyAlias,
            Culture = source.Culture,
            Segment = source.Segment,
            Context = source.Context?.Select(item => new AIRequestContextItem
            {
                Description = item.Description,
                Value = item.Value
            }).ToList(),
        };
    }

    // Umbraco.Code.MapAll
    private static PromptExecutionResponseModel CreateExecutionResponseFactory(
        AIPromptExecutionResult source,
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
            ValueChanges = source.ValueChanges?.Select(change => new ValueChangeModel
            {
                Path = change.Path,
                Value = change.Value,
                Culture = change.Culture,
                Segment = change.Segment
            }).ToList()
        };
    }
}
