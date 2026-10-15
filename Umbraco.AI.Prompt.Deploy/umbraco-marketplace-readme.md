## Umbraco.AI.Prompt.Deploy

Umbraco Deploy support for AI prompt templates - deploy prompts with scoping rules and profile dependencies across environments.

### Features

- **Prompt Deployment** - Deploy AI prompt templates with instructions and configurations
- **Profile Dependencies** - Automatic resolution of prompt-to-profile relationships
- **Scope Deployment** - Deploy scoping rules for content types, properties, and editors
- **Tag Preservation** - Maintain prompt categorization and organization
- **Context and Guardrail Dependencies** - Referenced contexts and guardrails are deployed alongside the prompt
- **Disk-Based Artifacts** - Git-friendly JSON artifacts for GitOps workflows
- **Ordered Processing** - Prompts are processed after the profiles, contexts and guardrails they reference

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- Umbraco.AI.Prompt 1.0.0+
- Umbraco.AI.Deploy 1.0.0+
- Umbraco Deploy 17.0.0+
- .NET 10.0

### Installation

```bash
dotnet add package Umbraco.AI.Prompt.Deploy
```

Requires both `Umbraco.AI.Deploy` and `Umbraco.AI.Prompt` to be installed.
