using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Contexts.ResourceTypes;
using Umbraco.AI.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Context.Mapping;

/// <summary>
/// Map definitions for Context models.
/// </summary>
public class ContextMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Response mappings (domain -> response)
        mapper.Define<AIContext, ContextResponseModel>((_, _) => new ContextResponseModel(), MapToResponse);
        mapper.Define<AIContext, ContextItemResponseModel>((_, _) => new ContextItemResponseModel(), MapToItemResponse);
        mapper.Define<AIContextResource, ContextResourceModel>((_, _) => new ContextResourceModel(), MapResourceToModel);

        // Request mappings (request -> domain)
        mapper.Define<CreateContextRequestModel, AIContext>(CreateContextFactory, MapFromCreateRequest);
        mapper.Define<UpdateContextRequestModel, AIContext>((_, _) => new AIContext
        {
            Alias = string.Empty,
            Name = string.Empty
        }, MapFromUpdateRequest);
        mapper.Define<ContextResourceModel, AIContextResource>(CreateResourceFactory, MapResourceFromModel);
    }

    private static AIContext CreateContextFactory(CreateContextRequestModel source, MapperContext context)
    {
        return new AIContext
        {
            Alias = source.Alias,
            Name = source.Name
        };
    }

    private static AIContextResource CreateResourceFactory(ContextResourceModel source, MapperContext context)
    {
        return new AIContextResource
        {
            ResourceTypeId = source.ResourceTypeId,
            Name = source.Name,
            Data = source.Data
        };
    }

    // Umbraco.Code.MapAll -Id -Alias -DateCreated -DateModified -Version -CreatedByUserId -ModifiedByUserId
    private static void MapFromCreateRequest(CreateContextRequestModel source, AIContext target, MapperContext context)
    {
        target.Name = source.Name;
        target.Resources = source.Resources.Select(r => context.Map<AIContextResource>(r)!).ToList();
    }

    // Umbraco.Code.MapAll -Id -DateCreated -DateModified -Version -CreatedByUserId -ModifiedByUserId
    private static void MapFromUpdateRequest(UpdateContextRequestModel source, AIContext target, MapperContext context)
    {
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Resources = source.Resources.Select(r => context.Map<AIContextResource>(r)!).ToList();
    }

    // Umbraco.Code.MapAll -ResourceTypeId
    private static void MapResourceFromModel(ContextResourceModel source, AIContextResource target, MapperContext context)
    {
        target.Id = source.Id;
        // ResourceTypeId is set in factory (init-only property)
        target.Name = source.Name;
        target.Description = source.Description;
        target.SortOrder = source.SortOrder;
        target.Data = source.Data;
        target.InjectionMode = Enum.TryParse<AIContextResourceInjectionMode>(source.InjectionMode, true, out var mode)
            ? mode
            : AIContextResourceInjectionMode.Always;
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AIContext source, ContextResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
        target.Resources = source.Resources.Select(r => context.Map<ContextResourceModel>(r)!).ToList();
        target.Version = source.Version;
    }

    // Umbraco.Code.MapAll -Version
    private static void MapToItemResponse(AIContext source, ContextItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.ResourceCount = source.Resources.Count;
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
    }

    // Umbraco.Code.MapAll
    private static void MapResourceToModel(AIContextResource source, ContextResourceModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.ResourceTypeId = source.ResourceTypeId;
        target.Name = source.Name;
        target.Description = source.Description;
        target.SortOrder = source.SortOrder;
        target.Data = source.Data;
        target.InjectionMode = source.InjectionMode.ToString();
    }
}
