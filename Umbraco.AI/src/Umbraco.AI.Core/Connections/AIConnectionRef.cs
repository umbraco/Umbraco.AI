namespace Umbraco.AI.Core.Connections;

/// <summary>
/// Lightweight reference to a connection (for use in profiles, lists, etc.).
/// </summary>
public record AIConnectionRef(Guid Id, string Name);
