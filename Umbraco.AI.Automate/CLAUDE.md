# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> **Note:** This is the Umbraco.AI.Automate package. See the [root CLAUDE.md](../CLAUDE.md) for shared coding standards, build commands, and repository-wide conventions that apply to all packages.

## Build Commands

```bash
# Build the solution
dotnet build Umbraco.AI.Automate.slnx

# Run tests
dotnet test Umbraco.AI.Automate.slnx
```

## Architecture Overview

Umbraco.AI.Automate provides Umbraco Automate integration for Umbraco.AI agents. It exposes AI agents as workflow actions and AI events as workflow triggers.

### Project Structure

Single-project structure (consistent with Deploy connector pattern):

| Project                                | Purpose                                  |
| -------------------------------------- | ---------------------------------------- |
| `Umbraco.AI.Automate`                  | Actions, triggers, and composer          |
| `Umbraco.AI.Automate.Tests.Unit`       | Unit tests                               |

**Why single-project?**
- Automate packages are integration layers, not domain models
- No database persistence needed (uses Automate's infrastructure)
- No backoffice UI components (Automate provides the workflow designer)
- Actions and triggers are auto-discovered via `[Action]` and `[Trigger]` attributes

### Key Components

**Actions** - Workflow steps that execute AI operations:
- `RunAgentAction` - Executes an AI agent with a message and returns the response

**Triggers** - Events that start automations:
- `AgentExecutedTrigger` - Fires when an AI agent completes execution

**Composer**:
- `UmbracoAIAutomateComposer` - Minimal composer; actions/triggers are auto-discovered

### Automate Extension Model

Actions and triggers use attribute-based discovery:

```csharp
// Actions use dynamic output schemas resolved from agent configuration
[Action("umbracoAI.runAgent", "Run AI Agent", Group = "AI", Icon = "icon-bot")]
public sealed class RunAgentAction : ActionBase<RunAgentSettings, object>

// Triggers use NotificationTriggerBase with dynamic output
[Trigger("umbracoAI.agentExecuted", "AI Agent Executed", Group = "AI", Icon = "icon-bot")]
public sealed class AgentExecutedTrigger
    : NotificationTriggerBase<AgentExecutedTriggerSettings, object, AIAgentExecutedNotification>
```

Settings POCOs use `[Field]` attribute with property editor UI aliases:

```csharp
[Field(Label = "Agent", Description = "The AI agent to execute.",
    EditorUiAlias = "Uai.PropertyEditorUi.AgentPicker")]
public Guid AgentId { get; set; }
```

### Alias Convention

Aliases use `umbracoAI.*` prefix (matching how Automate uses `umbracoAutomate.*` for built-in steps).

## Target Framework

- .NET 10.0 (`net10.0`)
- Uses Central Package Management (`Directory.Packages.props`)
- Nullable reference types enabled

## Dependencies

- Umbraco CMS 17.x
- Umbraco.AI.Agent 1.x
- Umbraco.Automate.Core 0.1.x

## Testing Focus

Unit tests cover:
- RunAgentAction: successful execution, agent not found, execution failure, cancellation
- AgentExecutedTrigger: event mapping, output population

## Related Documentation

- **[Umbraco.AI.Agent CLAUDE.md](../Umbraco.AI.Agent/CLAUDE.md)** - Agent domain model and services
- **[Umbraco.AI.Agent.Deploy CLAUDE.md](../Umbraco.AI.Agent.Deploy/CLAUDE.md)** - Similar connector pattern
