namespace Umbraco.AI.Agent.Core.Models;

/// <summary>
/// Configuration options for Umbraco.AI.Agent.
/// </summary>
public class AIAgentOptions
{
    /// <summary>
    /// The configuration section name.
    /// </summary>
    public const string SectionName = "Umbraco:AI:Agent";

    /// <summary>
    /// Gets or sets the retention period (in hours) for uploaded file attachments.
    /// Thread directories whose files have not been modified within this period are cleaned up.
    /// Defaults to 24 hours.
    /// </summary>
    public int FileRetentionHours { get; set; } = 24;
}
