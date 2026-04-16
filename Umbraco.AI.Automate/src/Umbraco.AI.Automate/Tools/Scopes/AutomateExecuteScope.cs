using Umbraco.AI.Core.Tools.Scopes;

namespace Umbraco.AI.Automate.Tools.Scopes;

/// <summary>
/// The automate execute scope. Tools that trigger automations should use this scope.
/// </summary>
[AIToolScope(ScopeId, Icon = "icon-flash", IsDestructive = true, Domain = "Automations")]
public sealed class AutomateExecuteScope : AIToolScopeBase
{
    /// <summary>
    /// The unique identifier for the automate execute scope.
    /// </summary>
    public const string ScopeId = "automate-execute";
}
