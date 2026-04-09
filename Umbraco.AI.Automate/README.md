# Umbraco.AI.Automate

Umbraco Automate integration for Umbraco.AI.Agent. Exposes AI agents as workflow actions and AI events as workflow triggers.

> **Note:** This package is part of the [Umbraco.AI](https://github.com/umbraco/Umbraco.AI) monorepo.

## Features

- **Run AI Agent** action - Execute any configured AI agent as a step in an Automate workflow
- **Agent Executed** trigger - Start automations when AI agents complete execution

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

- **Agent** - Select the AI agent to run (by alias)
- **Message** - The message to send to the agent (supports `${ binding }` syntax)

The action outputs the agent's response, success status, and execution duration for use in subsequent workflow steps.

### Agent Executed Trigger

Configure the "AI Agent Executed" trigger to start a workflow when an agent completes:

- **Agent Alias** (optional) - Filter to only trigger for a specific agent

The trigger outputs agent metadata (ID, alias, name, success status, duration) for use in workflow steps.

## Related Packages

- [Umbraco.AI.Agent](https://www.nuget.org/packages/Umbraco.AI.Agent) - AI agent management
- [Umbraco.AI](https://www.nuget.org/packages/Umbraco.AI) - Core AI capabilities for Umbraco
