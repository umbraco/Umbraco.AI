# Umbraco.AI.Automate

Umbraco Automate integration for Umbraco.AI.Agent. Exposes AI agents as workflow actions.

> **Note:** This package is part of the [Umbraco.AI](https://github.com/umbraco/Umbraco.AI) monorepo.

## Features

- **Run AI Agent** action - Execute any configured AI agent as a step in an Automate workflow
- Dynamic output schema - agent's configured output schema drives Automate binding autocomplete
- Automations surface - scope which agents are available in Automate workflows

## Installation

```bash
dotnet add package Umbraco.AI.Automate
```

## Requirements

- Umbraco CMS 17+
- Umbraco.AI.Agent 1.x
- Umbraco.Automate 0.1+

## Usage

### Run AI Agent Action

Configure the "Run AI Agent" action in your Automate workflow:

- **Agent** - Select the AI agent to run (picker filtered to agents with the "Automations" surface)
- **Message** - The message to send to the agent (supports `${ binding }` syntax)

The action outputs the agent's response for use in subsequent workflow steps. If the agent has a structured output schema, individual fields are available for binding.

## Related Packages

- [Umbraco.AI.Agent](https://www.nuget.org/packages/Umbraco.AI.Agent) - AI agent management
- [Umbraco.AI](https://www.nuget.org/packages/Umbraco.AI) - Core AI capabilities for Umbraco

