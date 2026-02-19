using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Web.Api.Management.Test.Models;
using Umbraco.AI.Web.Authorization;

namespace Umbraco.AI.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to list all available test graders.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AIAuthorizationPolicies.SectionAccessAI)]
public class AllTestGradersController : TestControllerBase
{
    private readonly AITestGraderCollection _testGraders;

    /// <summary>
    /// Initializes a new instance of the <see cref="AllTestGradersController"/> class.
    /// </summary>
    public AllTestGradersController(AITestGraderCollection testGraders)
    {
        _testGraders = testGraders;
    }

    /// <summary>
    /// Gets all available test graders that can be used to grade test outputs.
    /// Graders are discovered via the [AITestGrader] attribute and registered in DI.
    /// </summary>
    /// <returns>List of available test graders.</returns>
    [HttpGet("test-graders")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<TestGraderInfoModel>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<TestGraderInfoModel>> GetAllTestGraders()
    {
        var graders = _testGraders.Select(grader => new TestGraderInfoModel
        {
            Id = grader.Id,
            Name = grader.Name,
            Description = grader.Description,
            Type = grader.Type.ToString()
        });

        return Ok(graders);
    }
}
