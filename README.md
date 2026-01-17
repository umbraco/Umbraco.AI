# Umbraco.Ai

AI integration layer for Umbraco CMS, built on Microsoft.Extensions.AI.

## Products

This is a monorepo containing multiple Umbraco.Ai packages:

| Product | Description | Version | Location |
|---------|-------------|---------|----------|
| **Umbraco.Ai** | Core AI integration layer | 17.x | `Umbraco.Ai/` |
| **Umbraco.Ai.Agent** | AI agent management | 17.x | `Umbraco.Ai.Agent/` |
| **Umbraco.Ai.Prompt** | Prompt template management | 17.x | `Umbraco.Ai.Prompt/` |
| **Umbraco.Ai.OpenAi** | OpenAI provider | 1.x | `Umbraco.Ai.OpenAi/` |
| **Umbraco.Ai.Anthropic** | Anthropic provider | 1.x | `Umbraco.Ai.Anthropic/` |

## Quick Start

```bash
# Build all products
dotnet build Umbraco.Ai.sln

# Build distribution packages
.\Build-Distribution.ps1
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
    ├── Umbraco.Ai.Prompt (Add-on - depends on Core)
    └── Umbraco.Ai.Agent (Add-on - depends on Core)
```

## Documentation

- [CLAUDE.md](CLAUDE.md) - Development guide, build commands, and coding standards
- Product-specific guides:
  - [Umbraco.Ai/CLAUDE.md](Umbraco.Ai/CLAUDE.md) - Core package
  - [Umbraco.Ai.Agent/CLAUDE.md](Umbraco.Ai.Agent/CLAUDE.md) - Agent add-on
  - [Umbraco.Ai.Prompt/CLAUDE.md](Umbraco.Ai.Prompt/CLAUDE.md) - Prompt add-on

## Building for Distribution

The monorepo uses conditional references to support both local development and distribution builds:

- **Local development** (default): Uses project references for cross-project debugging
- **Distribution**: Uses package references via `-p:UseProjectReferences=false`

```bash
# Build all products as standalone NuGet packages
.\Build-Distribution.ps1

# Output: dist/nupkg/*.nupkg
```

## Release Process

This monorepo supports independent versioning per product:

- **Core/Agent/Prompt**: Version 17.x (matches Umbraco CMS)
- **Providers (OpenAI/Anthropic)**: Version 1.x (independent versioning)

### Branch Naming Convention

- `main` - Main development branch
- `feature/<product>-<name>` - Feature branches (e.g., `feature/core-add-caching`)
- `release/<product>-<version>` - Release branches (e.g., `release/core-17.0.1`)
- `hotfix/<product>-<version>` - Hotfix branches (e.g., `hotfix/openai-1.0.1`)

### Release Tags

- `release-core-17.0.1` - Core release
- `release-agent-17.0.1` - Agent release
- `release-prompt-17.0.1` - Prompt release
- `release-openai-1.0.1` - OpenAI provider release
- `release-anthropic-1.0.1` - Anthropic provider release

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
