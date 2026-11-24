using System.Reflection;
using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Umbraco.AI.Core.Security;
using Umbraco.AI.Web.Authorization;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security.Authorization;

namespace Umbraco.AI.Tests.Unit.Api.Management;

/// <summary>
/// Shared helpers for the authorization tests: they read the policies off a real controller and evaluate
/// them with the real handlers.
/// </summary>
internal static class AIAuthorizationTestHelper
{
    // Every verb declared on AIPermissions, so a new verb is covered without editing this list.
    internal static readonly string[] AllVerbs = typeof(AIPermissions)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToArray();

    // Every policy name declared on AIAuthorizationPolicies.
    private static readonly HashSet<string> AIPolicyNames = typeof(AIAuthorizationPolicies)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToHashSet(StringComparer.Ordinal);

    internal static bool IsAIPolicy(string? policy)
        => policy is not null && AIPolicyNames.Contains(policy);

    // Every [Authorize(Policy)] on the controller and its bases must pass, as in ASP.NET Core.
    internal static List<string?> GetPolicies(Type controllerType)
        => controllerType
            .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
            .Select(a => a.Policy)
            .ToList();

    internal static async Task<bool> IsAllowedAsync(IAuthorizationService authorizationService, Type controllerType)
    {
        // The CMS base policy needs a real backoffice login, so only the AI policies are evaluated here.
        foreach (var policy in GetPolicies(controllerType).Where(IsAIPolicy))
        {
            var result = await authorizationService.AuthorizeAsync(new ClaimsPrincipal(), policy!);
            if (!result.Succeeded)
            {
                return false;
            }
        }

        return true;
    }

    internal static IAuthorizationService CreateAuthorizationService(bool hasSection, string[] verbs)
    {
        var user = new Mock<IUser>();
        user.SetupGet(u => u.AllowedSections).Returns(hasSection ? [Core.Constants.Sections.AI] : []);

        IUser? outUser = user.Object;
        var helper = new Mock<IAuthorizationHelper>();
        helper.Setup(h => h.TryGetUmbracoUser(It.IsAny<ClaimsPrincipal>(), out outUser)).Returns(true);

        var permissions = new Mock<IAIPermissionService>();
        permissions.Setup(p => p.HasPermission(It.IsAny<string>())).Returns<string>(verbs.Contains);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(helper.Object);
        services.AddSingleton(permissions.Object);
        services.AddSingleton<IAuthorizationHandler, AISectionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationHandler, AIPermissionAuthorizationHandler>();
        services.AddAuthorization(AIAuthorizationPolicies.Register);

        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }
}
