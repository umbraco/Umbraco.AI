using Umbraco.AI.Web.Api.Management.Common.Controllers;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;

namespace Umbraco.AI.Prompt.Web.Api.Management.Common.Controllers;

/// <summary>
/// Base controller for Umbraco AI Prompt Management API controllers.
/// </summary>
[MapToApi(Constants.ManagementApi.ApiName)]
[JsonOptionsName(Constants.ManagementApi.ApiName)]
public abstract class UmbracoAIPromptManagementControllerBase : UmbracoAIManagementControllerBase
{ }
