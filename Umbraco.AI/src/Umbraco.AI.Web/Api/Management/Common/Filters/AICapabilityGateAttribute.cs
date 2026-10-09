using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Models;

namespace Umbraco.AI.Web.Api.Management.Common.Filters;

/// <summary>
/// Applies <see cref="AICapabilityGateFilter"/> to a controller, gating every action behind the
/// given <see cref="AICapability"/>'s experimental flag (404 when disabled).
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
internal sealed class AICapabilityGateAttribute : TypeFilterAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AICapabilityGateAttribute"/> class.
    /// </summary>
    /// <param name="capability">The capability whose experimental flag gates the controller.</param>
    public AICapabilityGateAttribute(AICapability capability)
        : base(typeof(AICapabilityGateFilter))
    {
        Arguments = [capability];
    }
}
