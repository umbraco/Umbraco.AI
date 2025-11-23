namespace Umbraco.Ai.Core.Models;

/// <summary>
/// Lightweight reference to a connection (for use in profiles, lists, etc.).
/// </summary>
public record AiConnectionRef(Guid Id, string Name);
