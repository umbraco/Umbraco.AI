# Umbraco AI - Core Concepts

## What is Umbraco AI?

Umbraco AI is a unified AI integration layer for Umbraco CMS. It provides a standardized way to connect and use AI services throughout your Umbraco installation, enabling AI-powered features like content assistance, translation, semantic search, and more.

Built on top of [Microsoft.Extensions.AI](https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/), Umbraco AI leverages Microsoft's official abstraction layer for AI services, ensuring a solid foundation and broad compatibility.

## Goals

### Provider-Agnostic

Use any AI provider - OpenAI, Azure OpenAI, Anthropic, or even local models - without changing your application code. Switch providers or use multiple providers simultaneously based on your needs.

### Plugin Architecture

Install only what you need. AI providers are delivered as NuGet packages that you can add to your project. Want to use OpenAI? Install the OpenAI plugin. Need Azure OpenAI for compliance? Add that plugin instead - or use both.

### UI Configurable

Manage your AI configuration through the familiar Umbraco backoffice. Create connections, configure profiles, and adjust settings without touching code. This means administrators can manage AI settings without developer involvement.

### Flexibility

Support multiple AI connections and configurations for different use cases. Use a powerful model for complex content generation while using a faster, cheaper model for simple tasks - all from the same installation.

---

## Core Concepts

### Providers (Plugins)

**What they are:** Installable packages that add support for specific AI services.

**Why they exist:** Different organizations have different needs. Some require OpenAI, others need Azure for compliance, and some want to run local models for privacy. Providers let you install only what you need and switch between services easily.

**Examples:**

- **OpenAI** - GPT-4, GPT-3.5, DALL-E, embeddings
- **Azure OpenAI** - Enterprise-grade OpenAI with Azure security
- **Anthropic** - Claude models
- **Ollama** - Run open-source models locally

Each provider can offer different **capabilities** - the types of AI features it supports:

- **Chat** - Conversational AI for content assistance, Q&A, etc.
- **Embeddings** - Convert text to vectors for semantic search
- **Media** - Image generation and analysis
- **Moderation** - Content safety checking

---

### Connections

**What they are:** Saved configurations for connecting to AI services, including credentials and provider-specific settings.

**Why they exist:** You need somewhere secure to store your API keys and configure how you connect to AI services. Connections provide this, and allow you to have multiple configurations for different purposes.

**Use cases:**

- **Multiple environments:** "Production OpenAI" with production API keys vs "Development OpenAI" with test keys
- **Multiple providers:** Use OpenAI for chat and a different provider for embeddings
- **Cost control:** Different connections with different rate limits or spending caps

Connections are configured through the Umbraco backoffice UI. Each connection belongs to a specific provider and stores the credentials needed to authenticate with that service.

---

### Profiles

**What they are:** Pre-configured AI settings for specific purposes. A profile combines a connection with specific model settings tailored for a particular use case.

**Why they exist:** Different tasks need different AI configurations. Content writing benefits from creative, varied responses. Translation needs accuracy and consistency. Profiles let you define these configurations once and reuse them throughout your application.

**Examples:**

- **"Content Assistant"** - Uses GPT-4 with higher temperature (0.8) for creative content suggestions
- **"Translation"** - Uses GPT-3.5-turbo with low temperature (0.2) for fast, consistent translations
- **"Search Embeddings"** - Uses text-embedding-3-small for generating semantic search vectors

**Profile settings include:**

- Which connection to use
- Which model to use
- Temperature (creativity vs consistency)
- Maximum tokens (response length)
- System prompt templates

---

### Middleware

**What it is:** Optional processing layers that can intercept AI requests and responses.

**Why it exists:** Many AI applications need cross-cutting functionality like logging, caching, rate limiting, or content filtering. Middleware lets you add these features without modifying your core application code.

**Examples:**

- **Logging** - Track all AI requests for debugging and auditing
- **Caching** - Cache responses to reduce costs and improve speed
- **Rate limiting** - Prevent excessive API usage
- **Content filtering** - Apply additional safety checks to responses

---

## How It Works Together

The concepts build on each other in a clear hierarchy:

```
Provider (which AI service)
    └── Connection (how to authenticate)
            └── Profile (what settings to use)
                    └── AI Request (the actual call)
```

**Step by step:**

1. **Install a Provider** - Add the NuGet package for your chosen AI service (e.g., `Umbraco.AI.OpenAI`)

2. **Create a Connection** - In the backoffice, create a connection to that provider with your API credentials

3. **Define Profiles** - Create profiles that use your connection with specific settings for different purposes

4. **Use in your application** - Reference profiles by name when making AI requests

**Example scenario:**

You install the OpenAI provider and create a connection called "My OpenAI" with your API key. Then you create two profiles:

- "Content Writer" - uses GPT-4 with creative settings
- "Quick Translate" - uses GPT-3.5-turbo optimized for speed

Your content editors get AI assistance through the "Content Writer" profile, while automated translation jobs use "Quick Translate" - both using the same API key but with different behaviors.

---

## Developer Services

Umbraco AI exposes services that developers can use to integrate AI into their solutions. These services handle all the configuration complexity - developers simply request a profile and provide their prompt.

### How It Works

Instead of manually configuring AI clients, API keys, and settings, developers inject a service and request a profile by name:

```csharp
public class MyContentService
{
    private readonly IAIChatService _chatService;

    public MyContentService(IAIChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<string> GenerateSummaryAsync(string content)
    {
        var response = await _chatService.GetResponseAsync(
            profileAlias: "content-assistant",
            prompt: $"Summarize the following content:\n\n{content}");

        return response.Text;
    }
}
```

The service automatically:

- Looks up the "content-assistant" profile
- Uses the connection configured for that profile
- Applies all the profile settings (model, temperature, etc.)
- Handles authentication with the AI provider
- Returns the response

### Management API

These services are also exposed via a Management API, making it easy to integrate AI features into the Umbraco backoffice UI or custom dashboards. Frontend developers can call the API endpoints to access AI functionality without needing to implement any backend code.

This enables scenarios like:

- AI-powered content suggestions in custom property editors
- Backoffice dashboards with AI analytics
- Custom admin tools that leverage AI capabilities

### Benefits for Developers

**Separation of concerns:** Your code focuses on _what_ you want the AI to do, not _how_ to configure it. Configuration is managed separately in the backoffice.

**Easy testing:** Switch profiles between development and production without code changes.

**Consistent behavior:** All code using the same profile gets the same AI configuration automatically.

**Future-proof:** If administrators change providers or settings, your code continues to work unchanged.

**Backend and frontend:** Use the same AI capabilities from C# services or via the Management API from JavaScript/TypeScript.

---

## Benefits

### For Content Editors

AI features simply work. No technical knowledge needed - just use the AI-powered tools that appear in the content editing experience.

### For Administrators

Configure and manage AI through the familiar Umbraco backoffice. Control which AI services are used, manage API keys securely, and adjust settings without developer involvement.

### For Developers

Clean, well-documented APIs make it easy to add AI features to your Umbraco solutions. The provider-agnostic design means no vendor lock-in - switch providers without rewriting code. Extend the system with custom providers or middleware when needed.

### For Organizations

Centralized control over AI usage across your Umbraco installation. Monitor costs, ensure compliance, and maintain security with enterprise-ready features.

---

## Getting Started

To start using Umbraco AI:

1. Install the core package and at least one provider
2. Configure a connection in the backoffice
3. Create profiles for your use cases
4. Start using AI features in your application

For detailed setup instructions, see the [Getting Started guide](../README.md).
