# Capabilities in Umbraco.AI

## What Are Capabilities?

Capabilities are the different types of AI functionality that Umbraco.AI provides. Think of them as categories of AI skills—just like a person might have skills in writing, analysis, or visual arts, AI providers have different capabilities they can offer.

Each capability represents a specific type of AI task. When you want to use AI in your Umbraco application, you choose the capability that matches what you're trying to accomplish.

## How Capabilities Fit Into Umbraco.AI

Umbraco.AI is built around a simple hierarchy:

```
┌─────────────────────────────────────────────────────┐
│                   Umbraco.AI                        │
├─────────────────────────────────────────────────────┤
│                                                     │
│   Providers (OpenAI, Azure, etc.)                   │
│        │                                            │
│        ▼                                            │
│   Capabilities (Chat, Embedding, Media, etc.)       │
│        │                                            │
│        ▼                                            │
│   Your Application                                  │
│                                                     │
└─────────────────────────────────────────────────────┘
```

Here's how these pieces work together:

- **Providers** are the AI services you connect to—companies like OpenAI or Microsoft Azure that offer AI models
- **Capabilities** are what those providers can do—the specific AI functions they support
- **Your Application** uses capabilities through Umbraco.AI, without needing to know the technical details of each provider

Different providers may support different capabilities. For example, one provider might excel at text generation while another specializes in image analysis. Umbraco.AI gives you a consistent way to use these capabilities regardless of which provider powers them.

## Current Capabilities

### Chat

The Chat capability enables conversational AI interactions. You can ask questions, provide context, and receive intelligent text responses.

**What it does:**

- Generates human-like text responses
- Maintains conversation context across multiple exchanges
- Follows instructions and adapts to different tones and styles

**Common uses in Umbraco:**

- Content generation (product descriptions, blog posts, summaries)
- Question answering and knowledge retrieval
- Translation and localization assistance
- Drafting emails, headlines, or marketing copy

**Example scenarios:**

- "Write a compelling product description for this new item"
- "Summarize this article in three bullet points"
- "Translate this content to French while maintaining a professional tone"

### Embedding

The Embedding capability converts text into numerical representations called vectors. These vectors capture the semantic meaning of text, enabling powerful search and comparison features.

**What it does:**

- Transforms text into mathematical representations
- Captures meaning, not just keywords
- Enables similarity comparisons between different pieces of content

**Common uses in Umbraco:**

- Semantic search (finding content by meaning, not just exact words)
- Content recommendations ("articles similar to this one")
- Clustering related content together
- Powering AI-assisted content discovery

**Example scenarios:**

- A visitor searches "how to return an item" and finds your "Refund Policy" page, even though those exact words aren't used
- Automatically suggesting related articles at the bottom of a blog post
- Finding duplicate or near-duplicate content across your site

## Future Capabilities

Umbraco.AI is designed to grow as AI technology evolves. Here are the capabilities planned for future releases:

### Media

The Media capability will handle image, audio, and video AI tasks.

**What it will do:**

- Generate images from text descriptions
- Analyze and describe image content
- Transcribe audio and video content
- Process and understand visual information

**Potential uses:**

- Automatically generating hero images for articles
- Creating alt-text for accessibility
- Transcribing video content for search indexing
- Analyzing uploaded images for content tagging

### Moderation

The Moderation capability will analyze content for safety. and policy compliance.

**What it will do:**

- Detect potentially harmful or inappropriate content
- Flag content that may violate policies
- Categorize content by safety level
- Provide confidence scores for moderation decisions

**Potential uses:**

- Filtering user-generated comments before publication
- Reviewing AI-generated content before it goes live
- Ensuring brand-safe content across your site
- Flagging content that needs human review

### Tool Calling

The Tool Calling capability will allow AI to perform actions, not just generate text.

**What it will do:**

- Execute predefined functions based on AI decisions
- Retrieve data from external systems
- Perform multi-step automated workflows
- Integrate AI decisions with business logic

**Potential uses:**

- AI assistants that can look up order status or inventory
- Automated content workflows that publish, categorize, and notify
- Smart forms that adapt based on user responses
- Integration with external services and APIs

## Why This Design?

The capability-based approach offers several advantages:

**Flexibility**
Switch between AI providers without rewriting your application. If you start with OpenAI but later want to use Azure, your code stays the same—only the configuration changes.

**Future-Proof**
As new AI capabilities emerge, they can be added to Umbraco.AI without disrupting existing functionality. Your investment in learning and implementing capabilities today will continue to pay off.

**Clarity**
Each capability has a clear purpose. You don't need to understand the entire AI landscape—just pick the capability that matches your need and use it.

**Choice**
Use only what you need. If you only need chat functionality, you're not burdened with complexity from other capabilities. Add more as your needs grow.

## Capabilities and Profiles

Capabilities work hand-in-hand with Umbraco.AI's profile system. A profile is a pre-configured setup for using a specific capability.

Each profile targets exactly one capability. You might have:

- A "content-writer" profile using the Chat capability with creative settings
- A "site-search" profile using the Embedding capability optimized for your content
- A "translator" profile using Chat with specific language instructions

Profiles include settings like:

- Which AI model to use
- How creative or deterministic the responses should be
- Default instructions or context
- Connection credentials

This separation means you can have multiple profiles for the same capability, each tuned for different purposes.

## Supported Capabilities by Provider

| Provider |  Chat   | Embedding |  Media  | Moderation |
| -------- | :-----: | :-------: | :-----: | :--------: |
| OpenAI   |    ✓    |     ✓     | Planned |  Planned   |
| Azure    | Planned |  Planned  | Planned |  Planned   |

As Umbraco.AI evolves, more providers and capabilities will be added. The capability-based architecture ensures these additions integrate smoothly without disrupting existing functionality.
