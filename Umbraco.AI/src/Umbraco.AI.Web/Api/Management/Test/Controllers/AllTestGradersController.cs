using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Extensions;
using Umbraco.AI.Web.Api.Management.Common.Routing;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.DependencyInjection;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to list all available test graders.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
[UmbracoAIVersionedManagementApiRoute("test-graders")]
public class AllTestGradersController : TestControllerBase
{
    private readonly AITestGraderCollection _testGraders;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllTestGradersController"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IAIExperimentalFeatures so that graders whose "
        + "required capabilities are disabled can be omitted. Will be removed in v19.")]
    public AllTestGradersController(AITestGraderCollection testGraders)
        : this(testGraders, StaticServiceProvider.Instance.GetRequiredService<IAIExperimentalFeatures>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AllTestGradersController"/> class.
    /// </summary>
    /// <remarks>
    /// Marked as the activation constructor: MVC builds controllers through
    /// <c>ActivatorUtilities</c>, which requires exactly one applicable constructor and throws when
    /// it can satisfy more than one. Keeping the obsolete overload around for binary compatibility
    /// means this attribute is what stops activation becoming ambiguous.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public AllTestGradersController(AITestGraderCollection testGraders, IAIExperimentalFeatures experimentalFeatures)
    {
        _testGraders = testGraders;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <summary>
    /// Gets all available test graders that can be used to grade test outputs.
    /// Graders are discovered via the [AITestGrader] attribute and registered in DI.
    /// </summary>
    /// <remarks>
    /// Omits a grader whose required capabilities (<see cref="AIRequiresCapabilityAttribute"/>)
    /// aren't all currently enabled. The grader stays in the underlying collection, so a
    /// previously-configured test referencing it still resolves and fails safe rather than open.
    /// </remarks>
    /// <returns>List of available test graders.</returns>
    [HttpGet]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<TestGraderInfoModel>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TestGraderInfoModel>> GetAllTestGraders()
    {
        var graders = _testGraders
            .Where(grader => grader.AreRequiredCapabilitiesEnabled(_experimentalFeatures))
            .Select(grader => new TestGraderInfoModel
            {
                Id = grader.Id,
                Name = grader.Name,
                Description = grader.Description,
                Type = grader.Type.ToString()
            });

        return Ok(graders);
    }
}
