using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Ai.Core.Tests;
using Umbraco.Ai.Web.Api.Management.Test.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Web.Api.Management.Test.Controllers;

/// <summary>
/// Controller to get all registered test graders.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AllTestGraderController : TestControllerBase
{
    private readonly AiTestGraderCollection _graders;

    public AllTestGraderController(AiTestGraderCollection graders)
    {
        _graders = graders;
    }

    /// <summary>
    /// Get all registered test graders.
    /// </summary>
    [HttpGet("test-graders")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(IEnumerable<TestGraderInfoModel>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var graders = _graders.Select(g => new TestGraderInfoModel
        {
            Id = g.Id,
            Name = g.Name,
            Description = g.Description,
            Type = (int)g.Type,
            HasConfigSchema = g.ConfigType is not null
        });

        return Ok(graders);
    }
}
