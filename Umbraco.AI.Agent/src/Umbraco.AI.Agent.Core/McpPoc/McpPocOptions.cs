namespace Umbraco.AI.Agent.Core.McpPoc;

/// <summary>
/// PROOF OF CONCEPT ONLY. Settings for the MCP client POC, bound from <c>Umbraco:AI:Agent:McpPoc</c>.
/// Stands in for the admin server list the real feature would store in the database.
/// </summary>
public sealed class McpPocOptions
{
    /// <summary>
    /// The configuration section the options bind from.
    /// </summary>
    public const string SectionName = "Umbraco:AI:Agent:McpPoc";

    /// <summary>
    /// The MCP servers Copilot may use. Each one is signed in with one shared account.
    /// </summary>
    public List<McpPocServer> Servers { get; set; } = [];
}

/// <summary>
/// PROOF OF CONCEPT ONLY. One remote MCP server (Streamable HTTP).
/// </summary>
public sealed class McpPocServer
{
    /// <summary>
    /// Short unique alias. Also used to prefix tool names so two servers can't clash.
    /// </summary>
    public string Alias { get; set; } = string.Empty;

    /// <summary>
    /// Display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The MCP endpoint URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Tool names (as the server names them) the agent may call. Anything not listed is dropped,
    /// which is how phase 1's "read-only, enforced" rule would work. Empty = no tools.
    /// </summary>
    public List<string> AllowedTools { get; set; } = [];

    /// <summary>
    /// Agent aliases that get this server. Empty = every agent.
    /// </summary>
    public List<string> AgentAliases { get; set; } = [];
}
