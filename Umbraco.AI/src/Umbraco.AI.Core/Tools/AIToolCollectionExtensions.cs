using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Tools;

namespace Umbraco.AI.Extensions;

/// <summary>
/// Extension methods for <see cref="AIToolCollection"/>.
/// </summary>
public static class AIToolCollectionExtensions
{
    /// <summary>
    /// Converts specified tools to AIFunction instances for MEAI ChatOptions.
    /// </summary>
    /// <param name="tools">The tool collection.</param>
    /// <param name="toolIds">The IDs of tools to convert.</param>
    /// <param name="factory">The function factory.</param>
    /// <returns>A list of AIFunctions for the specified tools.</returns>
    public static IReadOnlyList<AIFunction> ToAIFunctions(
        this AIToolCollection tools,
        IEnumerable<string> toolIds,
        IAiFunctionFactory factory)
    {
        var selectedTools = toolIds
            .Select(tools.GetById)
            .Where(t => t is not null)
            .Select(t => t!);

        return factory.Create(selectedTools);
    }

    /// <summary>
    /// Converts tools matching a predicate to AIFunction instances.
    /// </summary>
    /// <param name="tools">The tool collection.</param>
    /// <param name="predicate">The predicate to filter tools.</param>
    /// <param name="factory">The function factory.</param>
    /// <returns>A list of AIFunctions for matching tools.</returns>
    public static IReadOnlyList<AIFunction> ToAIFunctions(
        this AIToolCollection tools,
        Func<IAiTool, bool> predicate,
        IAiFunctionFactory factory)
    {
        return factory.Create(tools.Where(predicate));
    }

    /// <summary>
    /// Converts all tools to AIFunction instances.
    /// </summary>
    /// <param name="tools">The tool collection.</param>
    /// <param name="factory">The function factory.</param>
    /// <returns>A list of AIFunctions for all tools.</returns>
    public static IReadOnlyList<AIFunction> ToAIFunctions(
        this AIToolCollection tools,
        IAiFunctionFactory factory)
    {
        return factory.Create(tools);
    }

    /// <summary>
    /// Converts system tools to AIFunction instances.
    /// System tools are always included in agent requests and cannot be removed.
    /// </summary>
    /// <param name="tools">The tool collection.</param>
    /// <param name="factory">The function factory.</param>
    /// <returns>A list of AIFunctions for system tools.</returns>
    public static IReadOnlyList<AIFunction> ToSystemToolFunctions(
        this AIToolCollection tools,
        IAiFunctionFactory factory)
    {
        return factory.Create(tools.GetSystemTools());
    }

    /// <summary>
    /// Converts user tools to AIFunction instances.
    /// User tools can be configured and filtered by agents.
    /// </summary>
    /// <param name="tools">The tool collection.</param>
    /// <param name="factory">The function factory.</param>
    /// <returns>A list of AIFunctions for user tools.</returns>
    public static IReadOnlyList<AIFunction> ToUserToolFunctions(
        this AIToolCollection tools,
        IAiFunctionFactory factory)
    {
        return factory.Create(tools.GetUserTools());
    }
}
