using Umbraco.AI.Agent.Conversations.Core;
using Umbraco.AI.Agent.Conversations.Core.Projects;
using Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Models;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Web.Api.Management.Context.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Agent.Conversations.Web.Api.Management.Projects.Mapping;

/// <summary>
/// UmbracoMapper definitions for project models. Project resources reuse the core
/// <see cref="ContextResourceModel"/> DTO; the injection mode is converted between the domain enum and
/// its string representation.
/// </summary>
internal sealed class ProjectMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        mapper.Define<AIProject, ProjectResponseModel>((_, _) => new ProjectResponseModel(), MapToResponse);
        mapper.Define<AIAttachedResource, ContextResourceModel>((_, _) => new ContextResourceModel(), MapToResponse);
        mapper.Define<ProjectRequestModel, AIProject>((_, _) => new AIProject(), MapFromRequest);
        mapper.Define<ContextResourceModel, AIAttachedResource>(
            (_, _) => new AIAttachedResource { ResourceTypeId = string.Empty }, MapFromRequest);
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AIProject source, ProjectResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Instructions = source.Instructions;
        target.ContextIds = source.ContextIds.ToArray();
        target.Resources = context.MapEnumerable<AIAttachedResource, ContextResourceModel>(source.Resources);
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AIAttachedResource source, ContextResourceModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.ResourceTypeId = source.ResourceTypeId;
        target.Name = source.Name ?? string.Empty;
        target.Description = source.Description;
        target.SortOrder = source.SortOrder;
        target.Settings = source.Settings;
        target.InjectionMode = source.InjectionMode.ToString();
    }

    // Umbraco.Code.MapAll -Id -UserKey -DateCreated -DateModified -Version
    private static void MapFromRequest(ProjectRequestModel source, AIProject target, MapperContext context)
    {
        target.Name = source.Name;
        target.Description = source.Description;
        target.Instructions = source.Instructions;
        target.ContextIds = source.ContextIds.ToList();
        target.Resources = context.MapEnumerable<ContextResourceModel, AIAttachedResource>(source.Resources);
    }

    // Umbraco.Code.MapAll
    private static void MapFromRequest(ContextResourceModel source, AIAttachedResource target, MapperContext context)
    {
        target.Id = source.Id;
        target.ResourceTypeId = source.ResourceTypeId;
        target.Name = source.Name;
        target.Description = source.Description;
        target.SortOrder = source.SortOrder;
        target.Settings = source.Settings;
        target.InjectionMode = Enum.TryParse<AIContextResourceInjectionMode>(source.InjectionMode, true, out var mode)
            ? mode
            : AIContextResourceInjectionMode.Always;
    }
}
