## Umbraco.AI.HuggingFace

Hugging Face provider for Umbraco.AI — connect to hundreds of open-weights models through the
[Hugging Face Inference Providers](https://huggingface.co/docs/inference-providers/index) router.

### Features

- **Open-Weights Catalog** — Access conversational models from Llama, DeepSeek, Qwen, Mistral, GPT-OSS, and more through a single endpoint
- **Multi-Provider Routing** — Requests are routed to inference partners (Cerebras, Together, Fireworks, SambaNova, Groq, Replicate, …) automatically, or pinned with a `:provider` suffix
- **Chat Completions** — Streaming and non-streaming chat with tool-calling support
- **Dynamic Model Discovery** — Available models fetched live from the router
- **Custom Endpoints** — Point at any OpenAI-compatible gateway

### Requirements

- Umbraco CMS 17.0.0+
- Umbraco.AI 1.0.0+
- .NET 10.0
- A Hugging Face access token with Inference Providers permissions
