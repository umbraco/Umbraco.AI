using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.Prompt.Web.Api.Management.Utils.Controllers;

/// <summary>
/// Controller for retrieving content type and property aliases.
/// </summary>
[ApiVersion("1.0")]
[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]
public class AliasLookupController : UtilsControllerBase
{
    private readonly IContentTypeService _contentTypeService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AliasLookupController"/> class.
    /// </summary>
    public AliasLookupController(IContentTypeService contentTypeService)
    {
        _contentTypeService = contentTypeService;
    }

    /// <summary>
    /// Gets all document type aliases, optionally filtered by a search query.
    /// </summary>
    /// <param name="query">Optional search query to filter aliases.</param>
    /// <returns>A list of document type aliases.</returns>
    [HttpGet("document-type-aliases")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public IActionResult GetDocumentTypeAliases([FromQuery] string? query = null)
    {
        var allAliases = _contentTypeService.GetAllContentTypeAliases();

        if (!string.IsNullOrEmpty(query))
        {
            allAliases = allAliases.Where(a =>
                a.Contains(query, StringComparison.InvariantCultureIgnoreCase));
        }

        return Ok(allAliases.OrderBy(x => x).Take(20));
    }

    /// <summary>
    /// Gets all property aliases, optionally filtered by a search query.
    /// </summary>
    /// <param name="query">Optional search query to filter aliases.</param>
    /// <returns>A list of property aliases.</returns>
    [HttpGet("property-aliases")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType<IEnumerable<string>>(StatusCodes.Status200OK)]
    public IActionResult GetPropertyAliases([FromQuery] string? query = null)
    {
        var allAliases = _contentTypeService.GetAllPropertyTypeAliases();

        if (!string.IsNullOrEmpty(query))
        {
            allAliases = allAliases.Where(a =>
                a.Contains(query, StringComparison.InvariantCultureIgnoreCase));
        }

        return Ok(allAliases.OrderBy(x => x).Take(20));
    }
}
