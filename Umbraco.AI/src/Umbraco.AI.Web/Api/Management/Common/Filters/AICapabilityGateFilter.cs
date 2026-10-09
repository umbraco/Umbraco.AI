using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;

namespace Umbraco.AI.Web.Api.Management.Common.Filters;

/// <summary>
/// Gates every action on the controller it is applied to (via <see cref="AICapabilityGateAttribute"/>)
/// behind an <see cref="AICapability"/>'s experimental flag.
/// </summary>
/// <remarks>
/// Runs as a resource filter — before request-body model binding — rather than as a check inside the
/// action. A polymorphic request body (e.g. Decision's <c>$type</c>-discriminated question) throws
/// during binding on an unrecognized or missing discriminator, and <c>[ApiController]</c>'s automatic
/// invalid-model-state 400 response runs as an action filter — after binding, but still before the
/// action body. Left unguarded, that automatic 400 would pre-empt an in-action flag check, turning a
/// flag-off request with a structurally invalid body into a 400 instead of the empty 404 the capability
/// contract promises regardless of body validity. A resource filter runs before both binding and that
/// automatic response, so it short-circuits first.
/// </remarks>
internal sealed class AICapabilityGateFilter : IAsyncResourceFilter
{
    private readonly AICapability _capability;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="AICapabilityGateFilter"/> class.
    /// </summary>
    public AICapabilityGateFilter(AICapability capability, IAIExperimentalFeatures experimentalFeatures)
    {
        _capability = capability;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <inheritdoc />
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        if (!_experimentalFeatures.IsCapabilityEnabled(_capability))
        {
            context.Result = new NotFoundResult();
            return;
        }

        await next();
    }
}
