using System.Reflection;
using Microsoft.Extensions.AI;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// Default factory for creating MEAI AIFunction instances from AI tools.
/// </summary>
internal sealed class AIFunctionFactory : IAiFunctionFactory
{
    /// <inheritdoc />
    public AIFunction Create(IAiTool tool)
    {
        // For typed tools, we need to create a delegate with the proper signature
        // The interface's ExecuteAsync provides a common API
        if (tool.ArgsType is not null)
        {
            // Create typed delegate using reflection to match TArgs
            return CreateTypedFunction(tool);
        }

        // For untyped tools, create a simple delegate
        return AIFunctionFactory.Create(
            tool.ExecuteAsync,
            name: tool.Id,
            description: tool.Description);
    }

    /// <inheritdoc />
    public IReadOnlyList<AIFunction> Create(IEnumerable<IAiTool> tools)
        => tools.Select(Create).ToList();

    private static AIFunction CreateTypedFunction(IAiTool tool)
    {
        // Use the generic AIFunctionFactory.Create with typed delegate
        // This allows MEAI to infer schema from TArgs [Description] attributes
        var method = typeof(AIFunctionFactory)
            .GetMethod(nameof(CreateTypedFunctionCore), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(tool.ArgsType!);

        return (AIFunction)method.Invoke(null, [tool])!;
    }

    private static AIFunction CreateTypedFunctionCore<TArgs>(IAiTool tool) where TArgs : class
    {
        // Create a typed delegate that MEAI can use to infer schema from TArgs
        Func<TArgs, CancellationToken, Task<object>> execute =
            (args, ct) => tool.ExecuteAsync(args, ct);

        return AIFunctionFactory.Create(execute, name: tool.Id, description: tool.Description);
    }
}
