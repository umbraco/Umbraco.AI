// EX-6 — Extract over the Management API (AC8, AC12: same auth as Decision)
#pragma warning disable UMBRACOAI_EXTRACTION // Inspects the experimental Extraction controller

// The Decision endpoint's auth is not exercised over HTTP in unit tests: it comes only from the
// [Authorize(Policy = ...)] attributes inherited from UmbracoAIManagementControllerBase, and Umbraco's
// backoffice auth turns a missing token into a 401. So, like SharedLookupAuthorizationTests, these pin the
// mechanism (the inherited policies) rather than a live 401. The live 401 is checked on the demo site in
// the wire task (T9).

using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Umbraco.AI.Web.Api.Management.Decision.Controllers;
using Umbraco.AI.Web.Api.Management.Extraction.Controllers;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.Tests.Unit.Api.Management.Extraction;

public class ExtractAuthorizationTests
{
    private static List<string?> GetPolicies(Type controllerType)
        => controllerType
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(a => a.Policy)
            .OrderBy(p => p)
            .ToList();

    #region Happy path

    [Fact(Skip = "Pending")]
    public void ExtractController_HasTheSamePoliciesAsTheDecisionEndpoint()
    {
        // A user who can call decision/ask can call extraction/extract.
        GetPolicies(typeof(ExtractController)).ShouldBe(GetPolicies(typeof(AskDecisionController)));
    }

    [Fact(Skip = "Pending")]
    public void ExtractionControllerBase_IsAManagementApiController()
    {
        typeof(ExtractionControllerBase).IsSubclassOf(typeof(Umbraco.AI.Web.Api.Management.Common.Controllers.UmbracoAICoreManagementControllerBase))
            .ShouldBeTrue();
    }

    #endregion

    #region Sad path

    [Fact(Skip = "Pending")]
    public void ExtractController_RequiresBackOfficeAccessSoAnUnauthenticatedCallIsRejected()
    {
        GetPolicies(typeof(ExtractController)).ShouldContain(AuthorizationPolicies.BackOfficeAccess);
    }

    [Fact(Skip = "Pending")]
    public void ExtractController_DoesNotAllowAnonymousAccess()
    {
        typeof(ExtractController).GetCustomAttribute<AllowAnonymousAttribute>(inherit: true).ShouldBeNull();
    }

    #endregion
}
