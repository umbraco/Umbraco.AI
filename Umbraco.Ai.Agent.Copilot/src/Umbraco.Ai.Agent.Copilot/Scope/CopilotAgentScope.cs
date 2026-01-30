using Umbraco.Ai.Agent.Core.Scopes;

namespace Umbraco.Ai.Agent.Copilot.Scope;

[AiAgentScope(ScopeId, Icon = "icon-chat")]
public class CopilotAgentScope : AiAgentScopeBase
{
    /// <summary>
    /// The identifier for the Copilot agent scope.
    /// </summary>
    public const string ScopeId = "copilot"; 
}