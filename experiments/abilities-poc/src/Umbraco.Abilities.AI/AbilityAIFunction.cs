using System.Text.Json;
using System.Text.Json.Nodes;

using Microsoft.Extensions.AI;

using Umbraco.Abilities.ChangeSets;

namespace Umbraco.Abilities.AI;

/// <summary>
/// Exposes one ability as an M.E.AI tool. Nothing here is specific to an ability: every ability a package
/// declares becomes a tool the same way. An Automate or MCP adapter would be the same size.
/// </summary>
public sealed class AbilityAIFunction : AIFunction
{
    private readonly IAbility _ability;
    private readonly AbilityInvoker _invoker;
    private readonly Func<AbilityContext> _context;

    public AbilityAIFunction(IAbility ability, AbilityInvoker invoker, Func<AbilityContext> context)
    {
        _ability = ability;
        _invoker = invoker;
        _context = context;
        JsonSchema = AIJsonUtilities.CreateJsonSchema(ability.Descriptor.InputType, serializerOptions: AbilityJson.Options);
        AdditionalProperties = new Dictionary<string, object?>
        {
            ["abilityId"] = ability.Descriptor.Id,
            ["kind"] = ability.Descriptor.Kind.ToString(),
            ["requiresApproval"] = ability.Descriptor.RequiresApproval,
        };
    }

    /// <summary>Tool names allow letters, digits, _ and -, so <c>entity.addItem</c> becomes <c>entity_addItem</c>.</summary>
    public override string Name => ToToolName(_ability.Descriptor.Id);

    public override string Description => _ability.Descriptor.Description;

    public override JsonElement JsonSchema { get; }

    public override IReadOnlyDictionary<string, object?> AdditionalProperties { get; }

    public AbilityDescriptor Descriptor => _ability.Descriptor;

    public static string ToToolName(string abilityId) => abilityId.Replace('.', '_');

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        var input = new JsonObject();
        foreach (var (name, value) in arguments)
        {
            input[name] = value switch
            {
                null => null,
                JsonElement element => JsonNode.Parse(element.GetRawText()),
                JsonNode node => node.DeepClone(),
                _ => JsonSerializer.SerializeToNode(value, AbilityJson.Options),
            };
        }

        var result = await _invoker.InvokeAsync(_ability.Descriptor.Id, input, _context(), cancellationToken);

        // Failures go back to the model as data, so it can correct Input failures and stop on the others.
        object payload = result.Success
            ? new { success = true, output = result.Output }
            : new
            {
                success = false,
                category = result.Failure!.Category.ToString(),
                correctable = result.Failure.Correctable,
                message = result.Failure.Message,
                problems = result.Failure.Problems,
            };

        return JsonSerializer.SerializeToElement(payload, AbilityJson.Options);
    }
}

public static class AbilityAITools
{
    /// <summary>
    /// Every ability as a tool. Abilities that need approval are wrapped so a chat client pauses for a person,
    /// the same mechanism Umbraco.AI's HITL approval uses.
    /// </summary>
    public static IReadOnlyList<AIFunction> Create(AbilityRegistry abilities, AbilityInvoker invoker, Func<AbilityContext> context)
        => abilities.All.Select(ability =>
        {
            AIFunction function = new AbilityAIFunction(ability, invoker, context);
#pragma warning disable MEAI001 // Approval support in M.E.AI is experimental.
            return ability.Descriptor.RequiresApproval ? new ApprovalRequiredAIFunction(function) : function;
#pragma warning restore MEAI001
        }).ToList();
}
