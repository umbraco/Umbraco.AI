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
using Umbraco.Cms.Core.Mapping;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to get a test grader by ID.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
[UmbracoAIVersionedManagementApiRoute("test-graders")]
public class ByIdTestGraderController : TestControllerBase
{
    private readonly AITestGraderCollection _testGraders;
    private readonly IUmbracoMapper _umbracoMapper;
    private readonly IAIExperimentalFeatures _experimentalFeatures;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdTestGraderController"/> class.
    /// </summary>
    [Obsolete("Use the constructor that accepts an IAIExperimentalFeatures so that a grader whose "
        + "required capabilities are disabled 404s like an unknown id. Will be removed in v19.")]
    public ByIdTestGraderController(AITestGraderCollection testGraders, IUmbracoMapper umbracoMapper)
        : this(
            testGraders,
            umbracoMapper,
            StaticServiceProvider.Instance.GetRequiredService<IAIExperimentalFeatures>())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ByIdTestGraderController"/> class.
    /// </summary>
    /// <remarks>
    /// Marked as the activation constructor: MVC builds controllers through
    /// <c>ActivatorUtilities</c>, which requires exactly one applicable constructor and throws when
    /// it can satisfy more than one. Keeping the obsolete overload around for binary compatibility
    /// means this attribute is what stops activation becoming ambiguous.
    /// </remarks>
    [ActivatorUtilitiesConstructor]
    public ByIdTestGraderController(
        AITestGraderCollection testGraders,
        IUmbracoMapper umbracoMapper,
        IAIExperimentalFeatures experimentalFeatures)
    {
        _testGraders = testGraders;
        _umbracoMapper = umbracoMapper;
        _experimentalFeatures = experimentalFeatures;
    }

    /// <summary>
    /// Get a test grader by its ID including configuration schema.
    /// </summary>
    /// <remarks>
    /// Returns the same 404 for a grader whose required capabilities
    /// (<see cref="AIRequiresCapabilityAttribute"/>) aren't all currently enabled as for an unknown
    /// id. The grader stays in the underlying collection, so a previously-configured test
    /// referencing it still resolves elsewhere and fails safe rather than open.
    /// </remarks>
    /// <param name="id">The unique identifier of the test grader.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The test grader details with configuration schema.</returns>
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TestGraderResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetTestGraderById(
        string id,
        CancellationToken cancellationToken = default)
    {
        IAITestGrader? grader = _testGraders.FirstOrDefault(g =>
            g.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (grader is null || !grader.GetType().AreRequiredCapabilitiesEnabled(_experimentalFeatures))
        {
            return Task.FromResult(TestNotFound());
        }

        var responseModel = _umbracoMapper.Map<IAITestGrader, TestGraderResponseModel>(grader);
        return Task.FromResult<IActionResult>(Ok(responseModel));
    }
}
