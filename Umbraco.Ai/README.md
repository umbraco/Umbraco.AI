# Umbraco.Ai

Umbraco.Ai is an AI integration package for Umbraco CMS that provides a unified way to connect and interact with AI providers. Built on top of `Microsoft.Extensions.AI`, it offers a provider-agnostic abstraction layer with Umbraco-specific features like connections, profiles, and configurable middleware.

## Features

- **Provider-agnostic architecture** - Support for multiple AI providers (OpenAI, Azure, and more) through a common interface
- **Connection management** - Configure and manage multiple AI provider connections with different credentials and settings
- **Profiles** - Create reusable AI profiles with predefined settings (temperature, max tokens, model selection)
- **Contexts** - Define reusable AI contexts containing brand voice, guidelines, and reference materials that enrich AI operations
- **Tools** - Register custom AI tools that can be invoked by AI models for function calling scenarios
- **Middleware pipeline** - Extensible middleware system for logging, caching, rate limiting, and custom processing
- **Chat completions** - Full support for chat-based AI interactions with streaming capabilities
- **Embeddings** - Generate text embeddings for semantic search and similarity matching
- **Backoffice integration** - Management UI integrated into the Umbraco backoffice

## Project Structure

- **Umbraco.Ai.Core** - Core abstractions, providers, services, and models
- **Umbraco.Ai.Persistence** - EF Core DbContext, entities, and repository implementations
- **Umbraco.Ai.Persistence.SqlServer** - SQL Server migrations for persistence layer
- **Umbraco.Ai.Persistence.Sqlite** - SQLite migrations for persistence layer
- **Umbraco.Ai.Web** - Management API and backoffice endpoints
- **Umbraco.Ai.Web.StaticAssets** - Frontend assets for backoffice UI
- **Umbraco.Ai.Startup** - Composition and startup configuration
- **Umbraco.Ai** - Meta-package that references all components

## Monorepo Context

This package is part of the [Umbraco.Ai monorepo](../README.md). Provider packages are also included in the monorepo:
- **Umbraco.Ai.OpenAi** - Located in `../Umbraco.Ai.OpenAi/`
- **Umbraco.Ai.Anthropic** - Located in `../Umbraco.Ai.Anthropic/`

## Getting Started

### Installation

```bash
dotnet add package Umbraco.Ai
```

### Local Development

For local development and testing, use the monorepo setup script from the repository root:

```bash
# From repository root
.\scripts\install-demo-site.ps1  # Windows
./scripts/install-demo-site.sh   # Linux/Mac
```

This creates a unified solution (`Umbraco.Ai.local.sln`) with all packages and a demo site. See the [root README](../README.md) for details.

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide, architecture, and technical details for this package
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
