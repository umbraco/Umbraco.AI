using System.Reflection;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Routing;

using Shouldly;

using Umbraco.AI.Agent.Web.Api.Management.Agent.Controllers;

using Xunit;

namespace Umbraco.AI.Agent.Tests.Unit.Api;

/// <summary>
/// The CMS adds 401/403 responses to every Management API operation itself, and throws
/// "An item with the same key has already been added" (a 500 on the OpenAPI document) when the
/// action also declares the same status. This mirrors the CMS rule so a clash fails here.
/// </summary>
public class ManagementApiResponseCollisionTests
{
    // ManagementApiControllerBase carries two [Authorize] attributes (backoffice access + feature flags).
    private const int BaseAuthorizeAttributeCount = 2;

    [Fact]
    public void Actions_DoNotDeclareStatusesTheCmsAddsToTheOpenApiDocument()
    {
        var collisions = new List<string>();

        foreach (var controller in typeof(CreateAgentController).Assembly.GetTypes()
                     .Where(t => !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t)))
        {
            foreach (var action in controller.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(true).Any()))
            {
                if (HasAttribute<AllowAnonymousAttribute>(action, controller))
                {
                    continue;
                }

                var declared = action.GetCustomAttributes(true).Concat(controller.GetCustomAttributes(true))
                    .OfType<IApiResponseMetadataProvider>()
                    .Select(a => a.StatusCode)
                    .ToHashSet();

                if (declared.Contains(StatusCodes.Status401Unauthorized))
                {
                    collisions.Add($"{controller.Name}.{action.Name} declares 401");
                }

                if (declared.Contains(StatusCodes.Status403Forbidden) && CmsAdds403(action, controller))
                {
                    collisions.Add($"{controller.Name}.{action.Name} declares 403");
                }
            }
        }

        collisions.ShouldBeEmpty();
    }

    private static bool HasAttribute<T>(MethodInfo action, Type controller) =>
        action.GetCustomAttributes(true).OfType<T>().Any() || controller.GetCustomAttributes(true).OfType<T>().Any();

    // Same rule as BackOfficeSecurityRequirementsTransformer (Umbraco CMS 18).
    private static bool CmsAdds403(MethodInfo action, Type controller)
    {
        var authorizeCount = action.GetCustomAttributes(true).Count(a => a is AuthorizeAttribute)
                             + controller.GetCustomAttributes(true).Count(a => a is AuthorizeAttribute);

        return authorizeCount > BaseAuthorizeAttributeCount
               || controller.GetConstructors().Any(c => c.GetParameters().Any(p => p.ParameterType == typeof(IAuthorizationService)));
    }
}
