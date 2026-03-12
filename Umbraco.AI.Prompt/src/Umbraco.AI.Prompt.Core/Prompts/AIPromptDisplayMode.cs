namespace Umbraco.AI.Prompt.Core.Prompts;

/// <summary>
/// Determines where a prompt is displayed: as a property action or as a TipTap toolbar tool.
/// </summary>
public enum AIPromptDisplayMode
{
    /// <summary>
    /// The prompt appears as a property action on supported property editors.
    /// </summary>
    PropertyAction = 0,

    /// <summary>
    /// The prompt appears in the TipTap rich text editor toolbar dropdown.
    /// </summary>
    TipTapTool = 1
}
