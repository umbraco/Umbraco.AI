using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;

namespace Umbraco.AI.Web.Api.Management.Decision.Controllers;

/// <summary>
/// Gates every Decision management API action behind the Decision capability's experimental flag.
/// </summary>
/// <remarks>
/// Runs as a resource filter — before request-body model binding — rather than as a check inside the
/// action. The Decision request body is polymorphic on <c>$type</c>
/// (see <see cref="Models.DecisionQuestionModel"/>), so an unrecognized or missing discriminator throws
/// during binding, and <c>[ApiController]</c>'s automatic invalid-model-state 400 response runs as an
/// action filter — after binding, but still before the action body. Left unguarded, that automatic 400
/// would pre-empt an in-action flag check, turning a flag-off request with a structurally invalid body
/// into a 400 instead of the empty 404 the capability contract promises regardless of body validity.
/// A resource filter runs before both binding and that automatic response, so it short-circuits first.
/// </remarks>
internal sealed class DecisionCapabilityGateFilter : IAsyncResourceFilter
{
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecisionCapabilityGateFilter"/> class.
    /// </summary>
    public DecisionCapabilityGateFilter(IAIExperimentalFeatures experimentalFeatures)
    {
        _experimentalFeatures = experimentalFeatures;
    }

    /// <inheritdoc />
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (!_experimentalFeatures.IsCapabilityEnabled(AICapability.Decision))
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}
