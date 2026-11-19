using System.Net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Umbraco.AI.Agent.Core.McpPoc;

namespace Umbraco.AI.Agent.Web.Api.Management.McpPoc;

/// <summary>
/// PROOF OF CONCEPT ONLY. The OAuth return page.
/// </summary>
/// <remarks>
/// Anonymous on purpose: a browser redirect from the sign-in page carries no backoffice login.
/// It trusts nothing in the request except the one-time <c>state</c>, which points at a pending
/// sign-in that a signed-in backoffice user created.
/// </remarks>
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route(McpPocConstants.CallbackPath)]
public class McpPocCallbackController : Controller
{
    private readonly IMcpPocSignInService _signInService;

    /// <summary>
    /// Creates a new instance of the controller.
    /// </summary>
    public McpPocCallbackController(IMcpPocSignInService signInService)
        => _signInService = signInService;

    /// <summary>
    /// Finishes the sign-in.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return Page("Sign-in failed", $"{error}: {errorDescription}");
        }

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
        {
            return Page("Sign-in failed", "The sign-in page didn't send back a code.");
        }

        try
        {
            var alias = await _signInService.CompleteSignInAsync(state, code, cancellationToken);
            return Page("Connected", $"'{alias}' is connected. You can close this window.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Page("Sign-in failed", ex.Message);
        }
    }

    private ContentResult Page(string title, string message)
        => Content(
            $"<!doctype html><meta charset=utf-8><title>{WebUtility.HtmlEncode(title)}</title>"
            + $"<body style=\"font-family:sans-serif;padding:2rem\"><h1>{WebUtility.HtmlEncode(title)}</h1>"
            + $"<p>{WebUtility.HtmlEncode(message)}</p></body>",
            "text/html");
}
