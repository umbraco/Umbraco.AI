# Umbraco.Ai

AI integration layer for Umbraco CMS, built on Microsoft.Extensions.AI.

## Products

This is a monorepo containing multiple Umbraco.Ai packages:

| Product | Description | Version | Location |
|---------|-------------|---------|----------|
| **Umbraco.Ai** | Core AI integration layer | 1.x | `Umbraco.Ai/` |
| **Umbraco.Ai.Agent** | AI agent management | 1.x | `Umbraco.Ai.Agent/` |
| **Umbraco.Ai.Prompt** | Prompt template management | 1.x | `Umbraco.Ai.Prompt/` |
| **Umbraco.Ai.OpenAi** | OpenAI provider | 1.x | `Umbraco.Ai.OpenAi/` |
| **Umbraco.Ai.Anthropic** | Anthropic provider | 1.x | `Umbraco.Ai.Anthropic/` |
| **Umbraco.Ai.Amazon** | Amazon Bedrock provider | 1.x | `Umbraco.Ai.Amazon/` |
| **Umbraco.Ai.Google** | Google Gemini provider | 1.x | `Umbraco.Ai.Google/` |
| **Umbraco.Ai.MicrosoftFoundry** | Microsoft AI Foundry provider | 1.x | `Umbraco.Ai.MicrosoftFoundry/` |

## Quick Start

```bash
# Build all products
dotnet build Umbraco.Ai.sln
```

## Local Development

Each product has its own solution file and can be built independently:

```bash
# Build individual products
dotnet build Umbraco.Ai/Umbraco.Ai.sln
dotnet build Umbraco.Ai.Agent/Umbraco.Ai.Agent.sln
dotnet build Umbraco.Ai.Prompt/Umbraco.Ai.Prompt.sln
dotnet build Umbraco.Ai.OpenAi/Umbraco.Ai.OpenAi.sln
dotnet build Umbraco.Ai.Anthropic/Umbraco.Ai.Anthropic.sln
dotnet build Umbraco.Ai.Amazon/Umbraco.Ai.Amazon.sln
dotnet build Umbraco.Ai.Google/Umbraco.Ai.Google.sln
dotnet build Umbraco.Ai.MicrosoftFoundry/Umbraco.Ai.MicrosoftFoundry.sln
```

### Frontend Development (npm Workspaces)

This monorepo uses **npm workspaces** for frontend dependency management. Add-on packages (`@umbraco-ai/prompt`, `@umbraco-ai/agent`) automatically reference the local `@umbraco-ai/core` during development using the `workspace:*` protocol.

```bash
# Install all workspace dependencies (run from monorepo root)
npm install

# Build all frontends (sequential: core -> prompt -> agent)
npm run build

# Watch all frontends in parallel
npm run watch

# Build/watch specific packages
npm run build:core
npm run build:prompt
npm run watch:agent
```

**Workspace Benefits:**
- Single `npm install` installs all dependencies across all packages
- Automatic local package linking (no manual `npm link` required)
- Common dependencies are hoisted to the root `node_modules`
- `workspace:*` automatically replaced with published version during `npm pack`

## Architecture

```
Umbraco.Ai (Core)
    ├── Umbraco.Ai.OpenAi (Provider - depends on Core)
    ├── Umbraco.Ai.Anthropic (Provider - depends on Core)
    ├── Umbraco.Ai.Amazon (Provider - depends on Core)
    ├── Umbraco.Ai.Google (Provider - depends on Core)
    ├── Umbraco.Ai.MicrosoftFoundry (Provider - depends on Core)
    ├── Umbraco.Ai.Prompt (Add-on - depends on Core)
    └── Umbraco.Ai.Agent (Add-on - depends on Core)
```

## Documentation

- **[User Documentation](docs/public/README.md)** - Getting started, concepts, API reference, and guides
- [CLAUDE.md](CLAUDE.md) - Development guide, build commands, and coding standards
- Product-specific guides:
  - [Umbraco.Ai/CLAUDE.md](Umbraco.Ai/CLAUDE.md) - Core package
  - [Umbraco.Ai.Agent/CLAUDE.md](Umbraco.Ai.Agent/CLAUDE.md) - Agent add-on
  - [Umbraco.Ai.Prompt/CLAUDE.md](Umbraco.Ai.Prompt/CLAUDE.md) - Prompt add-on

## Release Process

This monorepo supports independent versioning per product:

- **All packages**: Version 1.x (independent versioning from Umbraco CMS)

### Release and Hotfix Branch Packaging

On `release/*` branches, CI requires a `release-manifest.json` at repo root. It must be a JSON array of product names (e.g. `["Umbraco.Ai", "Umbraco.Ai.OpenAi"]`). The manifest is treated as the authoritative list of packages to pack, and CI will fail if any changed product is missing from the list.

On `hotfix/*` branches, the manifest is optional. If present, it is enforced the same way; if absent, change detection is used.

### Branch Naming Convention

- `main` - Main development branch
- `feature/<product>-<name>` - Feature branches (e.g., `feature/core-add-caching`)
- `release/<product>-<version>` - Release branches (e.g., `release/core-1.0.1`)
- `hotfix/<product>-<version>` - Hotfix branches (e.g., `hotfix/openai-1.0.1`)

### Release Tags

- `release-core-1.0.1` - Core release
- `release-agent-1.0.1` - Agent release
- `release-prompt-1.0.1` - Prompt release
- `release-openai-1.0.1` - OpenAI provider release
- `release-anthropic-1.0.1` - Anthropic provider release
- `release-amazon-1.0.1` - Amazon Bedrock provider release
- `release-google-1.0.1` - Google Gemini provider release
- `release-microsoft-foundry-1.0.1` - Microsoft AI Foundry provider release

## Target Framework

- .NET 10.0 (`net10.0`)
- Umbraco CMS 17.x
- Central Package Management via `Directory.Packages.props`

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines on:
- Development workflow and branch naming
- Pull request process
- Release and deployment procedures
- Coding standards and conventions

For technical details, see [CLAUDE.md](CLAUDE.md).

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
