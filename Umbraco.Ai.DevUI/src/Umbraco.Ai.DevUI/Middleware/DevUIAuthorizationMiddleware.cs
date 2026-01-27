using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.Ai.DevUI.Middleware;

/// <summary>
/// Middleware that requires backoffice authentication for DevUI endpoints.
/// </summary>
public class DevUIAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<DevUIAuthorizationMiddleware> _logger;

    public DevUIAuthorizationMiddleware(
        RequestDelegate next,
        IAuthorizationService authorizationService,
        ILogger<DevUIAuthorizationMiddleware> logger)
    {
        _next = next;
        _authorizationService = authorizationService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only check authentication for non-API DevUI paths (/umbraco/devui and /meta)
        // The /v1/entities API endpoints have .RequireAuthorization() and will be handled
        // by ASP.NET Core's authentication/authorization middleware which runs after this
        if (context.Request.Path.StartsWithSegments("/umbraco/devui") ||
            context.Request.Path.StartsWithSegments("/meta"))
        {
            _logger.LogDebug("DevUI endpoint requested: {Path}, User authenticated: {IsAuthenticated}",
                context.Request.Path, context.User.Identity?.IsAuthenticated);

            // For these endpoints, we need to check authentication early because they're
            // mapped via reflection and we can't add .RequireAuthorization() to them.
            // However, we need to be careful about middleware order.

            // Check if user is authenticated first
            if (context.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("Unauthenticated request to DevUI endpoint: {Path}. Redirecting to login.",
                    context.Request.Path);

                // For page requests, redirect to login
                context.Response.Redirect($"/umbraco/backoffice/login?returnPath={Uri.EscapeDataString(context.Request.Path)}");
                return;
            }

            // Require backoffice access
            var authResult = await _authorizationService.AuthorizeAsync(
                context.User,
                null,
                AuthorizationPolicies.BackOfficeAccess);

            if (!authResult.Succeeded)
            {
                _logger.LogWarning("Authorization failed for DevUI endpoint: {Path}, User: {User}",
                    context.Request.Path, context.User.Identity?.Name);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Forbidden",
                    status = 403,
                    detail = "You do not have permission to access DevUI. Backoffice access is required."
                });
                return;
            }

            _logger.LogDebug("DevUI authorization succeeded for user: {User}", context.User.Identity?.Name);
        }

        await _next(context);
    }
}
