## Umbraco.Ai.Amazon

Amazon Bedrock provider for Umbraco.Ai - access multiple foundation models through a single AWS connection.

### Features

- **Multi-Model Access** - Use Amazon Nova, Claude, Llama, Mistral, and more through one connection
- **Chat Completions** - Streaming chat with all Bedrock-supported chat models
- **Text Embeddings** - Generate embeddings with Amazon Titan or Cohere models
- **Cross-Region Inference** - Automatic model discovery via Bedrock inference profiles
- **AWS Integration** - Standard IAM authentication with Access Key ID and Secret Access Key

### Supported Models

- **Chat**: Amazon Nova, Anthropic Claude, Meta Llama, Mistral
- **Embeddings**: Amazon Titan Embeddings, Cohere Embed

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.Ai 17.0.0+
- .NET 10.0
- AWS account with Bedrock access
