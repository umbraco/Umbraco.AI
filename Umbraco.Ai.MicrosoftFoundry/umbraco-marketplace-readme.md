## Umbraco.Ai.MicrosoftFoundry

Microsoft AI Foundry provider for Umbraco.Ai - unified access to multiple AI providers through a single Azure endpoint.

### Features

- **Unified Endpoint** - One connection provides access to OpenAI, Mistral, Llama, Cohere, Phi, and more
- **Dual Azure Support** - Works with both Azure OpenAI Service and Microsoft AI Foundry (Azure AI Inference)
- **Chat Completions** - Streaming chat with any deployed chat model
- **Text Embeddings** - Generate embeddings with OpenAI or Cohere models
- **Dynamic Model Discovery** - Automatically lists models deployed in your AI hub
- **Pay-as-you-go** - Serverless API deployment for cost-effective scaling

### Supported Models

- **Chat**: GPT-4o, Mistral Large, Llama 3, Command-R, Phi-3, and more
- **Embeddings**: text-embedding-3-small/large, Cohere Embed

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.Ai 1.0.0+
- .NET 10.0
- Azure subscription with AI Foundry resource
