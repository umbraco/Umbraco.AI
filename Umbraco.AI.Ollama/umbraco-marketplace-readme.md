# Umbraco AI Ollama Provider

Connect your Umbraco CMS to locally-hosted or remote Ollama instances for powerful AI capabilities using open-source language models.

## What is Ollama?

Ollama is a tool that lets you run large language models like Llama, Mistral, and Gemma locally on your machine. This provider enables seamless integration between Umbraco and Ollama, giving you privacy-focused, cost-effective AI without external API dependencies.

## Features

✨ **Privacy First** - Run AI models entirely on your infrastructure  
🚀 **Easy Setup** - Works with local Ollama instances out of the box  
💰 **Cost Effective** - No per-token pricing, use AI as much as you need  
🎯 **Multiple Models** - Support for Llama, Mistral, Gemma, CodeLlama, and more  
⚡ **Streaming Support** - Get real-time responses as they're generated  
🔐 **Optional Auth** - Secure remote instances with API keys and custom headers

## Prerequisites

- Umbraco CMS 17.1.0+
- [Umbraco.AI](https://marketplace.umbraco.com/package/umbraco.ai) installed
- [Ollama](https://ollama.ai) installed locally or access to remote instance

## Quick Start

### 1. Install Ollama

Download and install Ollama from [ollama.ai](https://ollama.ai), then pull a model:

```bash
ollama pull llama3.2
```

### 2. Install the Provider

Install via NuGet or the Umbraco backoffice package manager.

### 3. Configure Connection

In your Umbraco backoffice:

1. Go to **Settings** → **AI** → **Connections**
2. Create a new **Ollama** connection
3. For local development, use default settings (endpoint: `http://localhost:11434`)

### 4. Create AI Profile

1. Go to **Settings** → **AI** → **Profiles**
2. Create a profile with your Ollama connection
3. Select a model like `llama3.2` or `mistral`

You're ready to use Ollama-powered AI in your Umbraco site!

## Popular Models

- **Llama 3.2** - Meta's latest high-performance model
- **Mistral** - Fast and capable open model
- **Gemma** - Google's lightweight efficient model
- **CodeLlama** - Specialized for code generation
- **Phi** - Microsoft's compact model

## Use Cases

- **Content Generation** - Create drafts, descriptions, metadata
- **Chatbots** - Build customer support or interactive assistants
- **Code Assistance** - Generate code snippets and documentation
- **Translation** - Translate content across languages
- **Summarization** - Condense articles and documents

## Configuration

**Local Instance (default)**

```
Endpoint: http://localhost:11434
```

**Remote Instance**

```
Endpoint: https://your-ollama.com
API Key: (optional)
Custom Headers: (optional)
```

## Why Ollama?

Unlike cloud-based AI services, Ollama runs entirely on your infrastructure, meaning:

- ✅ Complete data privacy
- ✅ No usage limits or quotas
- ✅ Predictable costs (hardware only)
- ✅ Works offline
- ✅ Full control over models and versions

## Links

- [GitHub Repository](https://github.com/umbraco/Umbraco.AI)
- [Ollama Website](https://ollama.ai)
- [Documentation](https://github.com/umbraco/Umbraco.AI/tree/main/Umbraco.AI.Ollama)
- [Report Issues](https://github.com/umbraco/Umbraco.AI/issues)

## License

MIT License - Free for commercial and personal use
