using Microsoft.AspNetCore.Authorization;

namespace Umbraco.AI.Web.Authorization;

/// <summary>
/// Requires the current backoffice user to hold an AI right through one of their user groups.
/// </summary>
/// <param name="Verb">One of the <see cref="Core.Security.AIPermissions"/> constants.</param>
internal sealed record AIPermissionRequirement(string Verb) : IAuthorizationRequirement;
