namespace Umbraco.AI.Core.Models;

/// <summary>
/// Describes an AI model with its reference, display name, and metadata.
/// </summary>
public sealed class AIModelDescriptor(
    AIModelRef model,
    string name,
    IReadOnlyDictionary<string, string>? metadata = null)
{
    /// <summary>
    /// The reference to the AI model.
    /// </summary>
    public AIModelRef Model { get; } = model;

    /// <summary>
    /// The display name of the AI model.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Additional metadata associated with the AI model.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; } = metadata ?? new Dictionary<string, string>();
}