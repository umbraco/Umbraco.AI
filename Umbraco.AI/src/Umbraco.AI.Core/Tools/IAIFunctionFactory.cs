using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// Factory for creating MEAI AIFunction instances from AI tools.
/// </summary>
public interface IAIFunctionFactory
{
    /// <summary>
    /// Creates an AIFunction from the specified tool.
    /// </summary>
    /// <param name="tool">The tool to create a function for.</param>
    /// <returns>An AIFunction that invokes the tool.</returns>
    AIFunction Create(IAITool tool);

    /// <summary>
    /// Creates AIFunctions from multiple tools.
    /// </summary>
    /// <param name="tools">The tools to create functions for.</param>
    /// <returns>A list of AIFunctions.</returns>
    IReadOnlyList<AIFunction> Create(IEnumerable<IAITool> tools);
}
