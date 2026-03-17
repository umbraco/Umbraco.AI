using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Web.Api.Management.Common.Routing;
using Umbraco.AI.Web.Api.Management.Guardrail.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Guardrail.Controllers;

/// <summary>
/// Controller to list all available guardrail evaluators.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
[UmbracoAIVersionedManagementApiRoute("guardrail-evaluators")]
public class AllGuardrailEvaluatorsController : GuardrailControllerBase
{
    private readonly AIGuardrailEvaluatorCollection _evaluators;
    private readonly IUmbracoMapper _umbracoMapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllGuardrailEvaluatorsController"/> class.
    /// </summary>
    public AllGuardrailEvaluatorsController(AIGuardrailEvaluatorCollection evaluators, IUmbracoMapper umbracoMapper)
    {
        _evaluators = evaluators;
        _umbracoMapper = umbracoMapper;
    }

    /// <summary>
    /// Gets all available guardrail evaluators that can be used to evaluate content.
    /// Evaluators are discovered via the [AIGuardrailEvaluator] attribute and registered in DI.
    /// </summary>
    /// <returns>List of available guardrail evaluators with their configuration schemas.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<GuardrailEvaluatorInfoModel>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<GuardrailEvaluatorInfoModel>> GetAllGuardrailEvaluators()
    {
        var evaluators = _umbracoMapper.MapEnumerable<IAIGuardrailEvaluator, GuardrailEvaluatorInfoModel>(_evaluators);
        return Ok(evaluators);
    }
}
