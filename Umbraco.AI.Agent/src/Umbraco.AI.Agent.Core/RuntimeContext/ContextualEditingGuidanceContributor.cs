using Umbraco.AI.Agent.Core.Surfaces;
using Umbraco.AI.Core.RuntimeContext;

namespace Umbraco.AI.Agent.Core.RuntimeContext;

/// <summary>
/// Adds a system message explaining the editing boundary for surfaces that withhold destructive
/// backend tools (see <see cref="IAIAgentSurface.RestrictsDestructiveBackendTools"/>).
/// </summary>
/// <remarks>
/// <para>
/// This is the graceful counterpart to the hard tool lock applied in the agent factory: the lock
/// makes cross-item edits impossible, and this guidance makes the model <em>explain</em> that limit
/// rather than silently attempting an edit and hitting a tool error. The wording is deliberately
/// generic (it does not name any particular broader UI) so this stays in core with no dependency on
/// product packages; a package that offers a broader editing surface can add its own handoff
/// guidance as an additional contribution.
/// </para>
/// <para>
/// Must run after <see cref="SurfaceContextContributor"/> so the surface id is present in the
/// runtime context.
/// </para>
/// </remarks>
internal sealed class ContextualEditingGuidanceContributor : IAIRuntimeContextContributor
{
    private readonly AIAgentSurfaceCollection _surfaces;

    public ContextualEditingGuidanceContributor(AIAgentSurfaceCollection surfaces)
    {
        _surfaces = surfaces ?? throw new ArgumentNullException(nameof(surfaces));
    }

    /// <inheritdoc />
    public void Contribute(AIRuntimeContext context)
    {
        var surfaceId = context.GetValue<string>(Constants.ContextKeys.Surface);
        if (string.IsNullOrEmpty(surfaceId))
        {
            return;
        }

        if (_surfaces.GetById(surfaceId)?.RestrictsDestructiveBackendTools != true)
        {
            return;
        }

        context.SystemMessageParts.Add(
            "You are assisting with the item the user currently has open. You can read and search " +
            "across the whole site for reference, but you can only make changes to this one item. " +
            "If the user asks you to modify a different item, explain that you can only edit the item " +
            "they have open, and suggest they open that item or use a broader tool for changes that " +
            "span multiple items.");
    }
}
