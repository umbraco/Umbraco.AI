namespace Umbraco.AI.Agent.Core.McpPoc;

/// <summary>
/// PROOF OF CONCEPT ONLY. Shared-account OAuth sign-in for MCP servers.
/// </summary>
public interface IMcpPocSignInService
{
    /// <summary>Finds a configured server by alias.</summary>
    McpPocServer? GetServer(string alias);

    /// <summary>Lists configured servers and whether each is signed in.</summary>
    IReadOnlyList<McpPocServerStatus> GetServerStatuses();

    /// <summary>
    /// Starts a sign-in and returns the address to open in a browser.
    /// </summary>
    Task<string> StartSignInAsync(string alias, string redirectUri, string connectedBy, CancellationToken cancellationToken);

    /// <summary>
    /// Finishes a sign-in from the OAuth callback. Returns the server alias.
    /// </summary>
    Task<string> CompleteSignInAsync(string state, string code, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a valid access token, refreshing it if needed. <c>null</c> when not signed in.
    /// </summary>
    Task<string?> GetAccessTokenAsync(string alias, CancellationToken cancellationToken);
}

/// <summary>
/// PROOF OF CONCEPT ONLY. A configured server and whether it is signed in.
/// </summary>
public sealed record McpPocServerStatus(string Alias, string Name, string Url, bool Connected, string? ConnectedBy);

/// <summary>
/// PROOF OF CONCEPT ONLY. Constants.
/// </summary>
public static class McpPocConstants
{
    public const string HttpClientName = "UmbracoAI.McpPoc";

    public const string CallbackPath = "/umbraco/ai/mcp-poc/callback";
}
