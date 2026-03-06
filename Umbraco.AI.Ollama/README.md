# Umbraco.AI.Ollama

[![NuGet](https://img.shields.io/nuget/v/Umbraco.AI.Ollama.svg)](https://www.nuget.org/packages/Umbraco.AI.Ollama)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Umbraco.AI.Ollama.svg)](https://www.nuget.org/packages/Umbraco.AI.Ollama)

Ollama provider for [Umbraco.AI](https://github.com/umbraco/Umbraco.AI), enabling integration with locally-hosted or remote Ollama instances for AI-powered features in Umbraco CMS.

## Features

- 🚀 Support for all Ollama chat models (Llama, Mistral, Gemma, CodeLlama, etc.)
- 💻 Local development-friendly with default localhost configuration
- 🔐 Optional authentication for remote/managed Ollama instances
- 🎯 Automatic model discovery and filtering
- ⚡ Streaming and non-streaming chat completions
- 🧠 Built on Microsoft.Extensions.AI abstractions

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Umbraco.AI.Ollama
```

Or via the NuGet Package Manager UI in Visual Studio.

## Prerequisites

- Umbraco CMS 17.1.0 or later
- [Umbraco.AI](https://www.nuget.org/packages/Umbraco.AI) 1.0.0 or later
- .NET 10.0 or later
- [Ollama](https://ollama.ai) installed locally or access to a remote Ollama instance

## Quick Start

### 1. Install Ollama

If running locally, install Ollama from [https://ollama.ai](https://ollama.ai) and pull a model:

```bash
ollama pull llama3.2
```

### 2. Configure in Umbraco Backoffice

1. Navigate to **Settings** → **AI** → **Connections**
2. Click **Create Connection**
3. Select **Ollama** as the provider
4. Configure settings:
   - **Endpoint**: `http://localhost:11434` (default for local instances)
   - **API Key**: Leave empty for local instances, or add if using remote authentication
   - **Custom Headers**: Optional authentication headers (e.g., `Authorization: Bearer YOUR_TOKEN`)

### 3. Create an AI Profile

1. Navigate to **Settings** → **AI** → **Profiles**
2. Create a new profile using your Ollama connection
3. Select a model (e.g., `llama3.2`, `mistral`, `codellama`)

## Configuration

### Local Instance (Default)

```
Endpoint: http://localhost:11434
API Key: (leave empty)
Custom Headers: (leave empty)
```

### Remote Instance with API Key

```
Endpoint: https://your-ollama-instance.com
API Key: your-api-key-here
Custom Headers: (leave empty)
```

### Remote Instance with Custom Authentication

```
Endpoint: https://your-ollama-instance.com
API Key: (leave empty)
Custom Headers:
Authorization: Bearer YOUR_TOKEN
X-Custom-Header: value
```

## Supported Models

The provider automatically discovers all locally available Ollama models except embedding-only models (those with "embed" in the name).

Popular chat models include:

- **Llama 3.2** - Meta's latest open model
- **Mistral** - High-performance open model
- **Gemma** - Google's lightweight model
- **CodeLlama** - Specialized for code generation
- **Phi** - Microsoft's compact model

To see available models, run: `ollama list`

## Usage Example

```csharp
using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Services;

public class MyService
{
    private readonly IAIChatService _chatService;

    public MyService(IAIChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<string> GetChatResponse(Guid profileId)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, "You are a helpful assistant."),
            new(ChatRole.User, "What is Umbraco?")
        };

        var response = await _chatService.GetChatResponseAsync(
            profileId,
            messages,
            cancellationToken: CancellationToken.None
        );

        return response.Text;
    }
}
```

## Troubleshooting

### Connection Failed

- Verify Ollama is running: `ollama serve`
- Check endpoint URL is correct
- Ensure firewall allows connection to Ollama port (default: 11434)

### Model Not Found

- Pull the model: `ollama pull model-name`
- Restart Umbraco to refresh model cache
- Check model is listed: `ollama list`

### Authentication Errors

- Verify API key or custom headers are correct
- Check remote instance authentication requirements
- Ensure headers are formatted correctly (one per line: `Header-Name: value`)

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](../../CONTRIBUTING.md) for guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE.md](../LICENSE.md) file for details.

## Links

- [Umbraco.AI Documentation](https://github.com/umbraco/Umbraco.AI)
- [Ollama Documentation](https://ollama.ai)
- [OllamaSharp Library](https://github.com/awaescher/OllamaSharp)
- [Microsoft.Extensions.AI](https://github.com/dotnet/extensions)

## Support

- 🐛 [Report Issues](https://github.com/umbraco/Umbraco.AI/issues)
- 💬 [Umbraco Community](https://our.umbraco.com/)
- 📖 [Documentation](https://github.com/umbraco/Umbraco.AI)
