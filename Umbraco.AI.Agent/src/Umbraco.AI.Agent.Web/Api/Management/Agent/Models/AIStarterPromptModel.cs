namespace Umbraco.AI.Agent.Web.Api.Management.Agent.Models;

/// <summary>
/// API model for a single starter prompt shown as a clickable chip in an empty chat.
/// </summary>
public class AIStarterPromptModel
{
    /// <summary>
    /// The prompt text sent when the starter is clicked.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;
}
