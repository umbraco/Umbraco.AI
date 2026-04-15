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

        // For typed tools, build an AIFunction whose schema is TArgs at the top level,
        // so that each TArgs property is a top-level function parameter. This avoids a
        // nested "args" wrapper that Google Gemini's function-calling doesn't populate.
        if (tool.ArgsType is not null)
        {
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
        // Instantiate AIToolFunction<TArgs> via reflection to match the tool's argument type.
        var functionType = typeof(AIToolFunction<>).MakeGenericType(tool.ArgsType!);
        return (AIFunction)Activator.CreateInstance(functionType, tool, tool.Id, description)!;
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
