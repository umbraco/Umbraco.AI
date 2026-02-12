using System.Reflection;
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Tools.Scopes;
using MeaiAIFunctionFactory = Microsoft.Extensions.AI.AIFunctionFactory;

namespace Umbraco.AI.Core.Tools;

/// <summary>
/// Default factory for creating MEAI AIFunction instances from AI tools.
/// </summary>
internal sealed class AIFunctionFactory : IAIFunctionFactory
{
    private readonly AIToolScopeCollection _scopeCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="AIFunctionFactory"/> class.
    /// </summary>
    /// <param name="scopeCollection">The tool scope collection for enriching tool descriptions.</param>
    public AIFunctionFactory(AIToolScopeCollection scopeCollection)
    {
        _scopeCollection = scopeCollection;
    }
    /// <inheritdoc />
    public AIFunction Create(IAITool tool)
    {
        // Enrich description with scope metadata (ForEntityTypes)
        var enrichedDescription = EnrichDescription(tool);

        // For typed tools, we need to create a delegate with the proper signature
        // The interface's ExecuteAsync provides a common API
        if (tool.ArgsType is not null)
        {
            // Create typed delegate using reflection to match TArgs
            return CreateTypedFunction(tool, enrichedDescription);
        }

        // For untyped tools, create a simple delegate
        return MeaiAIFunctionFactory.Create(
            tool.ExecuteAsync,
            name: tool.Id,
            description: enrichedDescription);
    }

    /// <inheritdoc />
    public IReadOnlyList<AIFunction> Create(IEnumerable<IAITool> tools)
        => tools.Select(Create).ToList();

    private static AIFunction CreateTypedFunction(IAITool tool, string description)
    {
        // Use the generic AIFunctionFactory.Create with typed delegate
        // This allows MEAI to infer schema from TArgs [Description] attributes
        var method = typeof(AIFunctionFactory)
            .GetMethod(nameof(CreateTypedFunctionCore), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(tool.ArgsType!);

        return (AIFunction)method.Invoke(null, [tool, description])!;
    }

    private static AIFunction CreateTypedFunctionCore<TArgs>(IAITool tool, string description) where TArgs : class
    {
        // Create a typed delegate that MEAI can use to infer schema from TArgs
        Func<TArgs, CancellationToken, Task<object>> execute = tool.ExecuteAsync;

        return MeaiAIFunctionFactory.Create(execute, name: tool.Id, description: description);
    }

    /// <summary>
    /// Enriches the tool description with scope metadata (ForEntityTypes).
    /// </summary>
    private string EnrichDescription(IAITool tool)
    {
        var description = tool.Description;

        // Look up tool scope
        var scope = _scopeCollection.GetById(tool.ScopeId);
        if (scope is null)
        {
            return description;
        }

        // If scope has ForEntityTypes, append to description
        if (scope.ForEntityTypes.Count > 0)
        {
            var entityTypes = string.Join(", ", scope.ForEntityTypes);
            description = $"{description} [Suitable for entity types: {entityTypes}]";
        }

        return description;
    }
}
