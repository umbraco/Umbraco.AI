using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Guardrails.Evaluators;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Management.Common.Routing;
using Umbraco.AI.Web.Api.Management.Guardrail.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.DependencyInjection;
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
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllGuardrailEvaluatorsController"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IAIExperimentalFeatures so that evaluators whose "
        + "required capabilities are disabled can be omitted. Will be removed in v20.")]
    public AllGuardrailEvaluatorsController(AIGuardrailEvaluatorCollection evaluators, IUmbracoMapper umbracoMapper)
        : this(
            evaluators,
            umbracoMapper,
            StaticServiceProvider.Instance.GetRequiredService<IAIExperimentalFeatures>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AllGuardrailEvaluatorsController"/> class.
    /// </summary>
    /// <remarks>
    /// Marked as the activation constructor: MVC builds controllers through
    /// <c>ActivatorUtilities</c>, which requires exactly one applicable constructor and throws when
    /// it can satisfy more than one. Keeping the obsolete overload around for binary compatibility
    /// means this attribute is what stops activation becoming ambiguous.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public AllGuardrailEvaluatorsController(
        AIGuardrailEvaluatorCollection evaluators,
        IUmbracoMapper umbracoMapper,
        IAIExperimentalFeatures experimentalFeatures)
    {
        _evaluators = evaluators;
        _umbracoMapper = umbracoMapper;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <summary>
    /// Gets all available guardrail evaluators that can be used to evaluate content.
    /// Evaluators are discovered via the [AIGuardrailEvaluator] attribute and registered in DI.
    /// </summary>
    /// <remarks>
    /// Omits an evaluator whose required capabilities (<see cref="AIRequiresCapabilityAttribute"/>)
    /// aren't all currently enabled. The evaluator stays in the underlying collection, so a
    /// previously-saved rule referencing it still resolves and fails safe rather than open.
    /// </remarks>
    /// <returns>List of available guardrail evaluators with their configuration schemas.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<GuardrailEvaluatorInfoModel>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<GuardrailEvaluatorInfoModel>> GetAllGuardrailEvaluators()
    {
        var visibleEvaluators = _evaluators.Where(e => e.AreRequiredCapabilitiesEnabled(_experimentalFeatures));
        var evaluators = _umbracoMapper.MapEnumerable<IAIGuardrailEvaluator, GuardrailEvaluatorInfoModel>(visibleEvaluators);
        return Ok(evaluators);
    }
}
