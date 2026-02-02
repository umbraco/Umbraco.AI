using Umbraco.AI.Agent.Core.Scopes;

namespace Umbraco.AI.Agent.Copilot.Scope;

[AIAgentScope(ScopeId, Icon = "icon-chat")]
public class CopilotAgentScope : AIAgentScopeBase
{
    /// <summary>
    /// The identifier for the Copilot agent scope.
    /// </summary>
    public const string ScopeId = "copilot"; 
}