using Umbraco.Ai.Web.Api.Management.Common.Controllers;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Api.Common.Filters;

namespace Umbraco.Ai.Agent.Web.Api.Management.Common.Controllers;

/// <summary>
/// Base controller for Umbraco AI agent management API controllers.
/// </summary>
[MapToApi(Constants.ManagementApi.ApiName)]
[JsonOptionsName(Constants.ManagementApi.ApiName)]
public abstract class UmbracoAiAgentManagementControllerBase : UmbracoAiManagementControllerBase
{ }
