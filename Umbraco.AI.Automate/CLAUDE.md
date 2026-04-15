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

**Composer**:
- `UmbracoAIAutomateComposer` - Minimal composer; actions are auto-discovered

### Automate Extension Model

Actions use attribute-based discovery with dynamic output schemas:

```csharp
[Action("umbracoAI.runAgent", "Run AI Agent", Group = "AI", Icon = "icon-bot")]
public sealed class RunAgentAction : ActionBase<RunAgentSettings, object>
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
- RunAgentAction: successful execution, structured JSON parsing, agent not found, execution failure, cancellation

## Related Documentation

- **[Umbraco.AI.Agent CLAUDE.md](../Umbraco.AI.Agent/CLAUDE.md)** - Agent domain model and services
- **[Umbraco.AI.Agent.Deploy CLAUDE.md](../Umbraco.AI.Agent.Deploy/CLAUDE.md)** - Similar connector pattern
