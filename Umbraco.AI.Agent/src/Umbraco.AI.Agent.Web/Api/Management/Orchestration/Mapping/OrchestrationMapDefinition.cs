using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.Agent.Core.Orchestrations;
using Umbraco.AI.Agent.Web.Api.Management.Agent.Models;
using Umbraco.AI.Agent.Web.Api.Management.Orchestration.Models;
using Umbraco.Cms.Core.Mapping;
using Umbraco.Cms.Core.Strings;

namespace Umbraco.AI.Agent.Web.Api.Management.Orchestration.Mapping;

/// <summary>
/// UmbracoMapper definitions for orchestration models.
/// </summary>
internal class OrchestrationMapDefinition(IShortStringHelper shortStringHelper) : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Response mappings (domain -> response)
        mapper.Define<AIOrchestration, OrchestrationResponseModel>((_, _) => new OrchestrationResponseModel(), MapToResponse);
        mapper.Define<AIOrchestration, OrchestrationItemResponseModel>((_, _) => new OrchestrationItemResponseModel(), MapToItemResponse);

        // Request mappings (request -> domain)
        mapper.Define<CreateOrchestrationRequestModel, AIOrchestration>(CreateOrchestrationFactory, MapFromCreateRequest);
        mapper.Define<UpdateOrchestrationRequestModel, AIOrchestration>((_, _) => new AIOrchestration
        {
            Id = Guid.Empty,
            Alias = string.Empty,
            Name = string.Empty
        }, MapFromUpdateRequest);
    }

    private AIOrchestration CreateOrchestrationFactory(CreateOrchestrationRequestModel source, MapperContext context)
    {
        return new AIOrchestration
        {
            Id = Guid.Empty,
            Alias = !string.IsNullOrWhiteSpace(source.Alias)
                ? source.Alias
                : shortStringHelper.CleanStringForSafeAlias(source.Alias),
            Name = source.Name,
            ProfileId = source.ProfileId
        };
    }

    // Umbraco.Code.MapAll -Id -DateCreated -DateModified -Version -CreatedByUserId -ModifiedByUserId
    private static void MapFromCreateRequest(CreateOrchestrationRequestModel source, AIOrchestration target, MapperContext context)
    {
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Description = source.Description;
        target.ProfileId = source.ProfileId;
        target.SurfaceIds = source.SurfaceIds?.ToList() ?? [];
        target.Scope = MapScopeFromRequest(source.Scope);
        target.Graph = MapGraphFromRequest(source.Graph);
        target.IsActive = true;
    }

    // Umbraco.Code.MapAll -Id -DateCreated -DateModified -Version -CreatedByUserId -ModifiedByUserId
    private static void MapFromUpdateRequest(UpdateOrchestrationRequestModel source, AIOrchestration target, MapperContext context)
    {
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Description = source.Description;
        target.ProfileId = source.ProfileId;
        target.SurfaceIds = source.SurfaceIds?.ToList() ?? [];
        target.Scope = MapScopeFromRequest(source.Scope);
        target.Graph = MapGraphFromRequest(source.Graph);
        target.IsActive = source.IsActive;
    }

    // Umbraco.Code.MapAll -CreatedByUserId -ModifiedByUserId
    private static void MapToResponse(AIOrchestration source, OrchestrationResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Description = source.Description;
        target.ProfileId = source.ProfileId;
        target.SurfaceIds = source.SurfaceIds;
        target.Scope = MapScopeToResponse(source.Scope);
        target.Graph = MapGraphToResponse(source.Graph);
        target.IsActive = source.IsActive;
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
        target.Version = source.Version;
    }

    // Umbraco.Code.MapAll -Graph -Version -CreatedByUserId -ModifiedByUserId
    private static void MapToItemResponse(AIOrchestration source, OrchestrationItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Description = source.Description;
        target.ProfileId = source.ProfileId;
        target.SurfaceIds = source.SurfaceIds;
        target.Scope = MapScopeToResponse(source.Scope);
        target.IsActive = source.IsActive;
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
    }

    private static AIOrchestrationGraph MapGraphFromRequest(OrchestrationGraphModel source)
    {
        return new AIOrchestrationGraph
        {
            Nodes = source.Nodes.Select(n => new AIOrchestrationNode
            {
                Id = n.Id,
                Type = n.Type,
                Label = n.Label,
                X = n.X,
                Y = n.Y,
                Config = MapNodeConfigFromRequest(n.Config)
            }).ToList<AIOrchestrationNode>(),
            Edges = source.Edges.Select(e => new AIOrchestrationEdge
            {
                Id = e.Id,
                SourceNodeId = e.SourceNodeId,
                TargetNodeId = e.TargetNodeId,
                IsDefault = e.IsDefault,
                Priority = e.Priority
            }).ToList<AIOrchestrationEdge>()
        };
    }

    private static AIOrchestrationNodeConfig MapNodeConfigFromRequest(OrchestrationNodeConfigModel source)
    {
        return new AIOrchestrationNodeConfig
        {
            AgentId = source.AgentId,
            ToolName = source.ToolName,
            Conditions = source.Conditions?.Select(c => new AIOrchestrationRouteCondition
            {
                Label = c.Label,
                Field = c.Field,
                Operator = c.Operator,
                Value = c.Value,
                TargetNodeId = c.TargetNodeId
            }).ToList(),
            AggregationStrategy = source.AggregationStrategy,
            ManagerInstructions = source.ManagerInstructions,
            ManagerProfileId = source.ManagerProfileId
        };
    }

    private static OrchestrationGraphModel MapGraphToResponse(AIOrchestrationGraph source)
    {
        return new OrchestrationGraphModel
        {
            Nodes = source.Nodes.Select(n => new OrchestrationNodeModel
            {
                Id = n.Id,
                Type = n.Type,
                Label = n.Label,
                X = n.X,
                Y = n.Y,
                Config = MapNodeConfigToResponse(n.Config)
            }).ToList(),
            Edges = source.Edges.Select(e => new OrchestrationEdgeModel
            {
                Id = e.Id,
                SourceNodeId = e.SourceNodeId,
                TargetNodeId = e.TargetNodeId,
                IsDefault = e.IsDefault,
                Priority = e.Priority
            }).ToList()
        };
    }

    private static OrchestrationNodeConfigModel MapNodeConfigToResponse(AIOrchestrationNodeConfig source)
    {
        return new OrchestrationNodeConfigModel
        {
            AgentId = source.AgentId,
            ToolName = source.ToolName,
            Conditions = source.Conditions?.Select(c => new OrchestrationRouteConditionModel
            {
                Label = c.Label,
                Field = c.Field,
                Operator = c.Operator,
                Value = c.Value,
                TargetNodeId = c.TargetNodeId
            }).ToList(),
            AggregationStrategy = source.AggregationStrategy,
            ManagerInstructions = source.ManagerInstructions,
            ManagerProfileId = source.ManagerProfileId
        };
    }

    private static AIAgentScope? MapScopeFromRequest(AIAgentScopeModel? source)
    {
        if (source is null)
        {
            return null;
        }

        return new AIAgentScope
        {
            AllowRules = source.AllowRules.Select(r => new AIAgentScopeRule
            {
                Sections = r.Sections?.ToList(),
                EntityTypes = r.EntityTypes?.ToList()
            }).ToList(),
            DenyRules = source.DenyRules.Select(r => new AIAgentScopeRule
            {
                Sections = r.Sections?.ToList(),
                EntityTypes = r.EntityTypes?.ToList()
            }).ToList()
        };
    }

    private static AIAgentScopeModel? MapScopeToResponse(AIAgentScope? source)
    {
        if (source is null)
        {
            return null;
        }

        return new AIAgentScopeModel
        {
            AllowRules = source.AllowRules.Select(r => new AIAgentScopeRuleModel
            {
                Sections = r.Sections?.ToList(),
                EntityTypes = r.EntityTypes?.ToList()
            }).ToList(),
            DenyRules = source.DenyRules.Select(r => new AIAgentScopeRuleModel
            {
                Sections = r.Sections?.ToList(),
                EntityTypes = r.EntityTypes?.ToList()
            }).ToList()
        };
    }
}
