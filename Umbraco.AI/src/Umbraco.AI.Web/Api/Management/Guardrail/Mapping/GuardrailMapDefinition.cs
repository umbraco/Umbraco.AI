using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Web.Api.Management.Common.Models;
using Umbraco.AI.Web.Api.Management.Guardrail.Models;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Mapping;

/// <summary>
/// Map definitions for Guardrail models.
/// </summary>
public class GuardrailMapDefinition : IMapDefinition
{
    /// <inheritdoc />
    public void DefineMaps(IUmbracoMapper mapper)
    {
        // Response mappings (domain -> response)
        mapper.Define<AIGuardrail, GuardrailResponseModel>((_, _) => new GuardrailResponseModel(), MapToResponse);
        mapper.Define<AIGuardrail, GuardrailItemResponseModel>((_, _) => new GuardrailItemResponseModel(), MapToItemResponse);
        mapper.Define<AIGuardrailRule, GuardrailRuleModel>((_, _) => new GuardrailRuleModel(), MapRuleToModel);

        // Evaluator mappings
        mapper.Define<IAIGuardrailEvaluator, GuardrailEvaluatorInfoModel>((_, _) => new GuardrailEvaluatorInfoModel(), MapEvaluatorToInfo);

        // Request mappings (request -> domain)
        mapper.Define<CreateGuardrailRequestModel, AIGuardrail>(CreateGuardrailFactory, MapFromCreateRequest);
        mapper.Define<UpdateGuardrailRequestModel, AIGuardrail>((_, _) => new AIGuardrail
        {
            Alias = string.Empty,
            Name = string.Empty
        }, MapFromUpdateRequest);
        mapper.Define<GuardrailRuleModel, AIGuardrailRule>(CreateRuleFactory, MapRuleFromModel);
    }

    private static AIGuardrail CreateGuardrailFactory(CreateGuardrailRequestModel source, MapperContext context)
    {
        return new AIGuardrail
        {
            Alias = source.Alias,
            Name = source.Name
        };
    }

    private static AIGuardrailRule CreateRuleFactory(GuardrailRuleModel source, MapperContext context)
    {
        return new AIGuardrailRule
        {
            EvaluatorId = source.EvaluatorId,
            Name = source.Name
        };
    }

    // Umbraco.Code.MapAll -Id -Alias -DateCreated -DateModified -Version -CreatedByUserId -ModifiedByUserId
    private static void MapFromCreateRequest(CreateGuardrailRequestModel source, AIGuardrail target, MapperContext context)
    {
        target.Name = source.Name;
        target.Rules = source.Rules.Select(r => context.Map<AIGuardrailRule>(r)!).ToList();
    }

    // Umbraco.Code.MapAll -Id -DateCreated -DateModified -Version -CreatedByUserId -ModifiedByUserId
    private static void MapFromUpdateRequest(UpdateGuardrailRequestModel source, AIGuardrail target, MapperContext context)
    {
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.Rules = source.Rules.Select(r => context.Map<AIGuardrailRule>(r)!).ToList();
    }

    // Umbraco.Code.MapAll -EvaluatorId -GuardrailName -GuardrailId
    private static void MapRuleFromModel(GuardrailRuleModel source, AIGuardrailRule target, MapperContext context)
    {
        target.Id = source.Id;
        // EvaluatorId is set in factory (init-only property)
        target.Name = source.Name;
        target.Phase = Enum.TryParse<AIGuardrailPhase>(source.Phase, true, out var phase)
            ? phase
            : AIGuardrailPhase.PostGenerate;
        target.Action = Enum.TryParse<AIGuardrailAction>(source.Action, true, out var action)
            ? action
            : AIGuardrailAction.Block;
        target.Config = source.Config;
        target.SortOrder = source.SortOrder;
    }

    // Umbraco.Code.MapAll
    private static void MapToResponse(AIGuardrail source, GuardrailResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
        target.Rules = source.Rules.Select(r => context.Map<GuardrailRuleModel>(r)!).ToList();
        target.Version = source.Version;
    }

    // Umbraco.Code.MapAll -Version
    private static void MapToItemResponse(AIGuardrail source, GuardrailItemResponseModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Alias = source.Alias;
        target.Name = source.Name;
        target.RuleCount = source.Rules.Count;
        target.DateCreated = source.DateCreated;
        target.DateModified = source.DateModified;
    }

    // Umbraco.Code.MapAll
    private static void MapRuleToModel(AIGuardrailRule source, GuardrailRuleModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.EvaluatorId = source.EvaluatorId;
        target.Name = source.Name;
        target.Phase = source.Phase.ToString();
        target.Action = source.Action.ToString();
        target.Config = source.Config;
        target.SortOrder = source.SortOrder;
    }

    // Umbraco.Code.MapAll
    private static void MapEvaluatorToInfo(IAIGuardrailEvaluator source, GuardrailEvaluatorInfoModel target, MapperContext context)
    {
        target.Id = source.Id;
        target.Name = source.Name;
        target.Description = source.Description;
        target.Type = source.Type.ToString();
        target.SupportsRedaction = source is IAIRedactableGuardrailEvaluator;
        target.ConfigSchema = source.ConfigType is not null
            ? context.Map<EditableModelSchemaModel>(source.GetConfigSchema())
            : null;
    }
}
