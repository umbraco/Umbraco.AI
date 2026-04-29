# Umbraco.AI.Perplexity

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.Perplexity.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.AI.Perplexity/)

Perplexity provider plugin for Umbraco.AI, enabling integration with Perplexity's Sonar search-augmented chat models.

## Features

- **Perplexity API Support** - Connect to Perplexity's OpenAI-compatible API
- **Sonar Models** - Access Sonar, Sonar Pro, Sonar Reasoning Pro, and Sonar Deep Research
- **Search-Augmented Chat** - Responses grounded in current web results
- **Dynamic Model Discovery** - Sonar models are fetched at runtime, so new variants appear without a package update
- **Custom Endpoints** - Support for proxy servers or alternative endpoints

## Monorepo Context

This package is part of the [Umbraco.AI monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.AI.Perplexity
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Perplexity connection
3. Enter your Perplexity API key
4. Create a profile using this connection

### API Configuration

```json
{
    "ApiKey": "pplx-..."
}
```

## Supported Models

Perplexity's Sonar family is discovered dynamically at runtime. Current variants include:

- Sonar
- Sonar Pro
- Sonar Reasoning Pro
- Sonar Deep Research

## Limitations

**Tool / function calling is not supported.** Perplexity's Sonar `/chat/completions` endpoint does not accept the OpenAI-style `tools` parameter — Perplexity's models do web search natively, and that is their built-in capability instead of generic function calling. This provider transparently strips tool definitions from outgoing requests so prompts still execute, but any flow that depends on tools (e.g., Umbraco AI agents with content lookups, custom tools) will not work end-to-end with Perplexity. Use **OpenAI** or **Anthropic** providers for those flows.

This is a Perplexity product limitation, not a configuration issue with your API key. Perplexity's Agent API (`/v1/agent`) does support tool calling, but it routes to third-party models (OpenAI, Anthropic, etc.) rather than Sonar — that is a different integration not covered by this package.

**Recommended uses:** content generation, summarization, search-grounded Q&A, fact-finding prompts.

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
