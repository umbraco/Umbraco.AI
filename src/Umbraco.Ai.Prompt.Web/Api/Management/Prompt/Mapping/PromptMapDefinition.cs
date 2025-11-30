using Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Prompt.Mapping;

/// <summary>
/// UmbracoMapper definitions for prompt models.
/// </summary>
public class PromptMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<Core.Prompts.Prompt, PromptResponseModel>((_, _) => new PromptResponseModel
        {
            Id = Guid.Empty,
            Alias = string.Empty,
            Name = string.Empty,
            Content = string.Empty
        }, Map);

        mapper.Define<Core.Prompts.Prompt, PromptItemResponseModel>((_, _) => new PromptItemResponseModel
        {
            Id = Guid.Empty,
            Alias = string.Empty,
            Name = string.Empty
        }, MapItem);
    }

    private static void Map(Core.Prompts.Prompt source, PromptResponseModel target, MapperContext context)
    {
        target = new PromptResponseModel
        {
            Id = source.Id,
            Alias = source.Alias,
            Name = source.Name,
            Description = source.Description,
            Content = source.Content,
            ProfileId = source.ProfileId,
            Tags = source.Tags,
            IsActive = source.IsActive,
            DateCreated = source.DateCreated,
            DateModified = source.DateModified
        };
    }

    private static void MapItem(Core.Prompts.Prompt source, PromptItemResponseModel target, MapperContext context)
    {
        target = new PromptItemResponseModel
        {
            Id = source.Id,
            Alias = source.Alias,
            Name = source.Name,
            Description = source.Description,
            ProfileId = source.ProfileId,
            IsActive = source.IsActive
        };
    }
}
