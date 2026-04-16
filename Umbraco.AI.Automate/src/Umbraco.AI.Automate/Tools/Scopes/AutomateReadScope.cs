using Umbraco.AI.Core.Tools.Scopes;

namespace Umbraco.AI.Automate.Tools.Scopes;

/// <summary>
/// The automate read scope. Tools that list automations or check run status should use this scope.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-flash", Domain = "Automations")]
public sealed class AutomateReadScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the automate read scope.
    /// </summary>
    public const string ScopeId = "automate-read";
}
