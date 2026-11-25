namespace Umbraco.AI.Core.Security;

/// <summary>
/// Thrown when a service refuses a save because the current user lacks the right for it.
/// The Management API maps it to 403.
/// </summary>
internal sealed class AIPermissionDeniedException(string message) : Exception(message);
