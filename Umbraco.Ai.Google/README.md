# Umbraco.Ai.Google

[![NuGet](https://img.shields.io/nuget/v/Umbraco.Ai.Google.svg?style=flat&label=nuget)](https://www.nuget.org/packages/Umbraco.Ai.Google/)

Google provider plugin for Umbraco.Ai, enabling integration with Google's Gemini models.

## Features

- **Google AI Support** - Connect to Google's AI API (Gemini 2.0, 1.5 Pro, 1.5 Flash, etc.)
- **Chat Capabilities** - Full support for chat completions with streaming
- **Model Configuration** - Configure temperature, max tokens, and other model parameters
- **Middleware Support** - Compatible with Umbraco.Ai's middleware pipeline

## Monorepo Context

This package is part of the [Umbraco.Ai monorepo](../README.md). For local development, see the monorepo setup instructions in the root README.

## Installation

```bash
dotnet add package Umbraco.Ai.Google
```

## Requirements

- Umbraco CMS 17.0.0+
- Umbraco.Ai 17.0.0+
- .NET 10.0

## Configuration

After installation, create a connection in the Umbraco backoffice:

1. Navigate to the AI section
2. Create a new Google connection
3. Enter your API key from Google AI Studio
4. Create a profile using this connection

### API Configuration

```json
{
  "ApiKey": "AIza..."
}
```

## Supported Models

**Chat Models:**
- Gemini 2.0 Flash
- Gemini 2.0 Flash Lite
- Gemini 1.5 Pro
- Gemini 1.5 Flash
- Gemini 1.5 Flash 8B

## Documentation

- **[CLAUDE.md](CLAUDE.md)** - Development guide and technical details (if available)
- **[Root CLAUDE.md](../CLAUDE.md)** - Shared coding standards and conventions
- **[Contributing Guide](../CONTRIBUTING.md)** - How to contribute to the monorepo

## License

This project is licensed under the MIT License. See [LICENSE.md](../LICENSE.md) for details.
