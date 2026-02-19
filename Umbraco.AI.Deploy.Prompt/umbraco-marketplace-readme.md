## Umbraco.AI.Deploy.Prompt

Umbraco Deploy support for AI prompt templates - deploy prompts with scoping rules and profile dependencies across environments.

### Features

- **Prompt Deployment** - Deploy AI prompt templates with instructions and configurations
- **Profile Dependencies** - Automatic resolution of prompt-to-profile relationships
- **Scope Deployment** - Deploy scoping rules for content types, properties, and editors
- **Tag Preservation** - Maintain prompt categorization and organization
- **Context References** - Deploy context ID references (future feature)
- **Disk-Based Artifacts** - Git-friendly JSON artifacts for GitOps workflows
- **Multi-Pass Processing** - Pass 2 creates prompts, Pass 4 resolves profile dependencies

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- Umbraco.AI.Prompt 1.0.0+
- Umbraco.AI.Deploy 1.0.0+
- Umbraco Deploy 17.0.0+
- .NET 10.0

### Installation

```bash
dotnet add package Umbraco.AI.Deploy.Prompt
```

Requires both `Umbraco.AI.Deploy` and `Umbraco.AI.Prompt` to be installed.
