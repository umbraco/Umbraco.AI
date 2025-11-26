# Umbraco.Ai

![Code Coverage](https://gist.githubusercontent.com/mattbrailsford/b54d9b1b62c50c438c273e4660f09d99/raw/code-coverage-results.svg)

Umbraco.Ai is an AI integration package for Umbraco CMS that provides a unified way to connect and interact with AI providers. Built on top of `Microsoft.Extensions.AI`, it offers a provider-agnostic abstraction layer with Umbraco-specific features like connections, profiles, and configurable middleware.

## Features

- **Provider-agnostic architecture** - Support for multiple AI providers (OpenAI, Azure, and more) through a common interface
- **Connection management** - Configure and manage multiple AI provider connections with different credentials and settings
- **Profiles** - Create reusable AI profiles with predefined settings (temperature, max tokens, model selection)
- **Middleware pipeline** - Extensible middleware system for logging, caching, rate limiting, and custom processing
- **Chat completions** - Full support for chat-based AI interactions with streaming capabilities
- **Embeddings** - Generate text embeddings for semantic search and similarity matching
- **Backoffice integration** - Management UI integrated into the Umbraco backoffice

## Project Structure

- **Umbraco.Ai.Core** - Core abstractions, providers, services, and models
- **Umbraco.Ai.OpenAi** - OpenAI provider implementation
- **Umbraco.Ai.Web** - Management API and backoffice endpoints
- **Umbraco.Ai.Web.StaticAssets** - Frontend assets for backoffice UI
- **Umbraco.Ai.Startup** - Composition and startup configuration
- **Umbraco.Ai** - Meta-package that references all components

## Getting Started

### Setting Up a Demo Site

To quickly set up a local demo site for development and testing:

```powershell
.\scripts\Install-DemoSite.ps1
```

This script will:
- Create a demo folder with a new Umbraco site (`Umbraco.Ai.DemoSite`)
- Copy `Umbraco.Ai.sln` to `Umbraco.Ai.local.sln`
- Add the demo site to the local solution in a "Demo" solution folder
- Add project references to `Umbraco.Ai.Startup` and `Umbraco.Ai.Web.StaticAssets`
- Configure the demo site to work with local package development (disables central package management)

After running the script, you can open `Umbraco.Ai.local.sln` to work with both the package source code and the demo site.