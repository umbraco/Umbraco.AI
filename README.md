# Umbraco.Ai

AI integration layer for Umbraco CMS, built on Microsoft.Extensions.AI.

## Products

This is a monorepo containing multiple Umbraco.Ai packages:

| Product | Description | Version | Location |
|---------|-------------|---------|----------|
| **Umbraco.Ai** | Core AI integration layer | 1.x | `Umbraco.Ai/` |
| **Umbraco.Ai.Agent** | AI agent management and runtime | 1.x | `Umbraco.Ai.Agent/` |
| **Umbraco.Ai.Agent.Copilot** | Copilot chat UI for agents (frontend-only) | 1.x | `Umbraco.Ai.Agent.Copilot/` |
| **Umbraco.Ai.Prompt** | Prompt template management | 1.x | `Umbraco.Ai.Prompt/` |
| **Umbraco.Ai.OpenAi** | OpenAI provider | 1.x | `Umbraco.Ai.OpenAi/` |
| **Umbraco.Ai.Anthropic** | Anthropic provider | 1.x | `Umbraco.Ai.Anthropic/` |
| **Umbraco.Ai.Amazon** | Amazon Bedrock provider | 1.x | `Umbraco.Ai.Amazon/` |
| **Umbraco.Ai.Google** | Google Gemini provider | 1.x | `Umbraco.Ai.Google/` |
| **Umbraco.Ai.MicrosoftFoundry** | Microsoft AI Foundry provider | 1.x | `Umbraco.Ai.MicrosoftFoundry/` |

## Quick Start

The fastest way to get started is using the install-demo script, which creates a unified development environment with all packages and a demo Umbraco site:

```bash
# Windows
.\scripts\install-demo-site.ps1

# Linux/Mac
./scripts/install-demo-site.sh
```

This creates:
- `Umbraco.Ai.local.sln` - Unified solution with all products
- `demo/Umbraco.Ai.DemoSite/` - Umbraco instance with all packages referenced

After running the script, build the frontend and backend:

```bash
# Install frontend dependencies
npm install

# Build all frontend packages
npm run build

# Build the unified solution
dotnet build Umbraco.Ai.local.sln

# Run the demo site (from demo/Umbraco.Ai.DemoSite/)
cd demo/Umbraco.Ai.DemoSite
dotnet run
```

**Demo site credentials:** admin@example.com / password1234

## Local Development

### Building Individual Products

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
            └── Umbraco.Ai.Agent.Copilot (Chat UI - depends on Agent)
```

## Documentation

- **[User Documentation](docs/public/README.md)** - Getting started, concepts, API reference, and guides
- [CLAUDE.md](CLAUDE.md) - Development guide, build commands, and coding standards
- Product-specific guides:
  - [Umbraco.Ai/CLAUDE.md](Umbraco.Ai/CLAUDE.md) - Core package
  - [Umbraco.Ai.Agent/CLAUDE.md](Umbraco.Ai.Agent/CLAUDE.md) - Agent add-on
  - [Umbraco.Ai.Agent.Copilot/CLAUDE.md](Umbraco.Ai.Agent.Copilot/CLAUDE.md) - Agent Copilot add-on
  - [Umbraco.Ai.Prompt/CLAUDE.md](Umbraco.Ai.Prompt/CLAUDE.md) - Prompt add-on

## Target Framework

- .NET 10.0 (`net10.0`)
- Umbraco CMS 17.x
- Central Package Management via `Directory.Packages.props`

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines on:
- Development workflow and branch naming conventions
- Commit message format (conventional commits)
- Changelog generation and maintenance
- Pull request process
- Release and deployment procedures
- Coding standards

For development setup and build commands, see [CLAUDE.md](CLAUDE.md).

## License

This project is licensed under the MIT License. See [LICENSE.md](LICENSE.md) for details.
