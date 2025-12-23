using Microsoft.Extensions.AI;
using Umbraco.Ai.Core.Tools;

namespace Umbraco.Ai.Extensions;

/// <summary>
/// Extension methods for <see cref="AiToolCollection"/>.
/// </summary>
public static class AiToolCollectionExtensions
{
    /// <summary>
    /// Converts specified tools to AIFunction instances for MEAI ChatOptions.
    /// </summary>
    /// <param name="tools">The tool collection.</param>
    /// <param name="toolIds">The IDs of tools to convert.</param>
    /// <param name="factory">The function factory.</param>
    /// <returns>A list of AIFunctions for the specified tools.</returns>
    public static IReadOnlyList<AIFunction> ToAIFunctions(
        this AiToolCollection tools,
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
        this AiToolCollection tools,
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
        this AiToolCollection tools,
        IAiFunctionFactory factory)
    {
        return factory.Create(tools);
    }
}
