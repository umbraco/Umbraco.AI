# Umbraco.Ai Architecture Overview

This document provides a comprehensive architecture overview of Umbraco.Ai and its add-on packages.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              UMBRACO CMS 17.x                                   │
│                                                                                 │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                         UMBRACO BACKOFFICE UI                             │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐            │  │
│  │  │  AI Connections │  │   AI Profiles   │  │  AI Prompts/    │            │  │
│  │  │   Management    │  │   Management    │  │  Agents Mgmt    │            │  │
│  │  └────────┬────────┘  └────────┬────────┘  └────────┬────────┘            │  │
│  └───────────┼────────────────────┼────────────────────┼────────────────────┘  │
│              │                    │                    │                       │
│  ┌───────────┴────────────────────┴────────────────────┴────────────────────┐  │
│  │                        MANAGEMENT API LAYER                              │  │
│  │                    /umbraco/ai/management/api/v1/                        │  │
│  └──────────────────────────────────┬───────────────────────────────────────┘  │
│                                     │                                          │
│  ┌──────────────────────────────────┴───────────────────────────────────────┐  │
│  │                                                                          │  │
│  │  ┌──────────────────────────────────────────────────────────────────┐    │  │
│  │  │                     UMBRACO.AI (CORE)                            │    │  │
│  │  │  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐  │    │  │
│  │  │  │  Provider  │  │ Connection │  │  Profile   │  │  Chat/     │  │    │  │
│  │  │  │   System   │  │   System   │  │   System   │  │ Embedding  │  │    │  │
│  │  │  └────────────┘  └────────────┘  └────────────┘  │  Services  │  │    │  │
│  │  │                                                  └────────────┘  │    │  │
│  │  └──────────────────────────────────────────────────────────────────┘    │  │
│  │                                     │                                    │  │
│  │    ┌────────────────────────────────┼───────────────────────────────┐    │  │
│  │    │                               │                                │    │  │
│  │    ▼                               ▼                                ▼    │  │
│  │  ┌──────────────┐  ┌───────────────────────────┐  ┌──────────────────┐   │  │
│  │  │ UMBRACO.AI.  │  │    UMBRACO.AI.PROMPT      │  │  UMBRACO.AI.     │   │  │
│  │  │   OPENAI     │  │   (Prompt Templates)      │  │     AGENT        │   │  │
│  │  │  (Provider)  │  │       (Add-on)            │  │    (Add-on)      │   │  │
│  │  └──────────────┘  └───────────────────────────┘  └──────────────────┘   │  │
│  │                                                                          │  │
│  └──────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                     MICROSOFT.EXTENSIONS.AI (M.E.AI)                            │
│         ┌─────────────────────────┐  ┌─────────────────────────────┐            │
│         │      IChatClient        │  │  IEmbeddingGenerator<>      │            │
│         └─────────────────────────┘  └─────────────────────────────┘            │
└─────────────────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                           AI SERVICE PROVIDERS                                  │
│    ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐        │
│    │  OpenAI  │  │  Azure   │  │ Anthropic│  │  Ollama  │  │  Custom  │        │
│    │          │  │  OpenAI  │  │          │  │  (Local) │  │ Provider │        │
│    └──────────┘  └──────────┘  └──────────┘  └──────────┘  └──────────┘        │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## Product Dependencies

```
                    ┌─────────────────────────────┐
                    │      UMBRACO.AI (Core)      │
                    │                             │
                    │  • Provider abstraction     │
                    │  • Connection management    │
                    │  • Profile management       │
                    │  • Chat/Embedding services  │
                    │  • Middleware pipeline      │
                    └──────────────┬──────────────┘
                                   │
           ┌───────────────────────┼───────────────────────┐
           │                       │                       │
           ▼                       ▼                       ▼
┌──────────────────────┐ ┌──────────────────────┐ ┌──────────────────────┐
│  UMBRACO.AI.OPENAI   │ │  UMBRACO.AI.PROMPT   │ │   UMBRACO.AI.AGENT   │
│     (Provider)       │ │      (Add-on)        │ │       (Add-on)       │
│                      │ │                      │ │                      │
│ • OpenAI API client  │ │ • Prompt templates   │ │ • Agent definitions  │
│ • Chat capability    │ │ • Variable support   │ │ • Behavior config    │
│ • Embedding cap.     │ │ • Execute prompts    │ │ • Profile linking    │
│ • Model listing      │ │ • Scope validation   │ │                      │
└──────────────────────┘ └──────────────────────┘ └──────────────────────┘
```

## Core Hierarchical Model

The core architecture follows a clear hierarchy:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              PROVIDER                                       │
│  Plugin that implements AI capabilities (e.g., OpenAI, Azure, Anthropic)   │
│  • Discovered via [AiProvider] attribute                                    │
│  • Registers supported capabilities                                         │
│  • Defines provider-specific settings                                       │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ 1:N
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                             CONNECTION                                      │
│  Stores API credentials and provider configuration                         │
│  • API key (can reference config via $ConfigPath)                          │
│  • Endpoint URL                                                            │
│  • Provider-specific settings                                              │
│  • Identifies alias for easy reference                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ 1:N
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              PROFILE                                        │
│  Use-case specific configuration combining connection + model settings     │
│  • Links to a connection                                                   │
│  • Model selection (gpt-4, gpt-3.5-turbo, etc.)                           │
│  • Temperature, max tokens                                                 │
│  • System prompt                                                           │
│  • Can be set as default for capability                                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ Used by
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            AI REQUEST                                       │
│  Actual chat/embedding call using configured profile                       │
│  • IAiChatService.GetResponseAsync()                                       │
│  • IAiEmbeddingService.GenerateEmbeddingAsync()                           │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Capability System

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           IAiCapability                                     │
│                         (Base Interface)                                    │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │
            ┌───────────────────┴───────────────────┐
            │                                       │
            ▼                                       ▼
┌───────────────────────────────┐   ┌───────────────────────────────┐
│      IAiChatCapability        │   │   IAiEmbeddingCapability      │
│                               │   │                               │
│  Creates: IChatClient         │   │  Creates: IEmbeddingGenerator │
│                               │   │                               │
│  Methods:                     │   │  Methods:                     │
│  • CreateClientAsync()        │   │  • CreateGeneratorAsync()     │
│  • GetModelsAsync()           │   │  • GetModelsAsync()           │
└───────────────────────────────┘   └───────────────────────────────┘
            │                                       │
            │  Implementations                      │  Implementations
            ▼                                       ▼
┌───────────────────────────────┐   ┌───────────────────────────────┐
│   OpenAiChatCapability        │   │  OpenAiEmbeddingCapability    │
│   AzureChatCapability         │   │  AzureEmbeddingCapability     │
│   OllamaChatCapability        │   │  OllamaEmbeddingCapability    │
│   ... (extensible)            │   │  ... (extensible)             │
└───────────────────────────────┘   └───────────────────────────────┘
```

## Chat Request Flow

```
┌──────────────────┐
│  Developer Code  │
│                  │
│  await chatSvc   │
│  .GetResponse    │
│  Async(messages) │
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────────────────────────────────────┐
│                      IAiChatService                              │
│  GetResponseAsync(messages, options?, profileId?)                │
└────────────────────────────┬─────────────────────────────────────┘
                             │
         ┌───────────────────┴───────────────────┐
         │                                       │
         ▼                                       ▼
┌──────────────────────┐              ┌──────────────────────┐
│  IAiProfileService   │              │ IAiConnectionService │
│                      │              │                      │
│  GetProfileAsync()   │──────────────│ GetConfigured        │
│  Resolves: settings, │              │ ProviderAsync()      │
│  model, temperature  │              │ Resolves: provider   │
└──────────────────────┘              │ with credentials     │
                                      └──────────┬───────────┘
                                                 │
                                                 ▼
                              ┌───────────────────────────────────┐
                              │     AiChatClientFactory           │
                              │                                   │
                              │  CreateChatClientAsync(profile)   │
                              └───────────────────┬───────────────┘
                                                  │
                                                  ▼
                              ┌───────────────────────────────────┐
                              │     Middleware Pipeline           │
                              │                                   │
                              │  ┌─────────────────────────────┐  │
                              │  │   Logging Middleware        │  │
                              │  └──────────────┬──────────────┘  │
                              │                 ▼                 │
                              │  ┌─────────────────────────────┐  │
                              │  │   Tracing Middleware        │  │
                              │  └──────────────┬──────────────┘  │
                              │                 ▼                 │
                              │  ┌─────────────────────────────┐  │
                              │  │   Custom Middleware (...)   │  │
                              │  └──────────────┬──────────────┘  │
                              └─────────────────┼─────────────────┘
                                                │
                                                ▼
                              ┌───────────────────────────────────┐
                              │     IAiChatCapability             │
                              │                                   │
                              │  CreateClient(settings, model)    │
                              └───────────────────┬───────────────┘
                                                  │
                                                  ▼
                              ┌───────────────────────────────────┐
                              │     OpenAI SDK / HTTP Client      │
                              │                                   │
                              │  Actual API call to AI service    │
                              └───────────────────┬───────────────┘
                                                  │
                                                  ▼
                              ┌───────────────────────────────────┐
                              │     ChatResponse (M.E.AI)         │
                              │                                   │
                              │  Standardized response format     │
                              └───────────────────────────────────┘
```

## Project Structure (Per Product)

```
ProductName/
│
├── src/
│   │
│   ├── ProductName.Core/                    # Domain Layer
│   │   ├── Connections/                     # Connection domain
│   │   │   ├── IAiConnectionService.cs
│   │   │   ├── AiConnection.cs
│   │   │   └── IAiConnectionRepository.cs
│   │   ├── Profiles/                        # Profile domain
│   │   │   ├── IAiProfileService.cs
│   │   │   ├── AiProfile.cs
│   │   │   └── IAiProfileRepository.cs
│   │   ├── Providers/                       # Provider abstractions
│   │   │   ├── IAiProvider.cs
│   │   │   ├── IAiCapability.cs
│   │   │   └── AiProviderBase.cs
│   │   ├── Chat/                            # Chat service
│   │   │   ├── IAiChatService.cs
│   │   │   └── IAiChatMiddleware.cs
│   │   └── Embeddings/                      # Embedding service
│   │       └── IAiEmbeddingService.cs
│   │
│   ├── ProductName.Persistence/             # Data Access Layer
│   │   ├── UmbracoAiDbContext.cs
│   │   ├── Entities/
│   │   │   ├── AiConnectionEntity.cs
│   │   │   └── AiProfileEntity.cs
│   │   └── Repositories/
│   │       ├── EfCoreAiConnectionRepository.cs
│   │       └── EfCoreAiProfileRepository.cs
│   │
│   ├── ProductName.Persistence.SqlServer/   # SQL Server Migrations
│   │   └── Migrations/
│   │
│   ├── ProductName.Persistence.Sqlite/      # SQLite Migrations
│   │   └── Migrations/
│   │
│   ├── ProductName.Web/                     # API Layer
│   │   └── Api/Management/
│   │       ├── Controllers/
│   │       │   ├── AllConnectionController.cs
│   │       │   ├── CreateConnectionController.cs
│   │       │   └── ...
│   │       ├── Models/
│   │       │   ├── ConnectionResponseModel.cs
│   │       │   └── CreateConnectionRequestModel.cs
│   │       └── Mapping/
│   │
│   ├── ProductName.Web.StaticAssets/        # Frontend Layer
│   │   └── Client/
│   │       ├── src/
│   │       │   ├── components/              # Lit web components
│   │       │   ├── api/                     # Generated OpenAPI client
│   │       │   └── stores/                  # State management
│   │       ├── package.json
│   │       └── vite.config.ts
│   │
│   ├── ProductName.Startup/                 # DI Registration
│   │   └── UmbracoAiComposer.cs
│   │
│   └── ProductName/                         # Meta-package
│       └── ProductName.csproj               # References all projects
│
├── tests/
│   ├── ProductName.Tests.Unit/
│   ├── ProductName.Tests.Integration/
│   └── ProductName.Tests.Common/
│
├── ProductName.sln
└── CLAUDE.md
```

## Extension Points

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          EXTENSION POINTS                                   │
└─────────────────────────────────────────────────────────────────────────────┘

1. CUSTOM PROVIDERS
   ┌────────────────────────────────────────────────────────────────────────┐
   │  [AiProvider("custom", "My Custom Provider")]                         │
   │  public class CustomProvider : AiProviderBase<CustomProviderSettings> │
   │  {                                                                    │
   │      public CustomProvider(IAiProviderInfrastructure infrastructure)  │
   │          : base(infrastructure)                                       │
   │      {                                                                │
   │          WithCapability<CustomChatCapability>();                      │
   │      }                                                                │
   │  }                                                                    │
   └────────────────────────────────────────────────────────────────────────┘

2. CUSTOM MIDDLEWARE
   ┌────────────────────────────────────────────────────────────────────────┐
   │  public class LoggingMiddleware : IAiChatMiddleware                   │
   │  {                                                                    │
   │      public IChatClient Apply(IChatClient client)                     │
   │      {                                                                │
   │          return new LoggingChatClient(client);                        │
   │      }                                                                │
   │  }                                                                    │
   │                                                                       │
   │  // Register in Composer:                                             │
   │  builder.AiChatMiddleware().Append<LoggingMiddleware>();              │
   └────────────────────────────────────────────────────────────────────────┘

3. CUSTOM CAPABILITIES
   ┌────────────────────────────────────────────────────────────────────────┐
   │  public class CustomChatCapability                                    │
   │      : AiChatCapabilityBase<CustomProviderSettings>                   │
   │  {                                                                    │
   │      public override IChatClient CreateClient(                        │
   │          CustomProviderSettings settings, string modelId) { ... }     │
   │                                                                       │
   │      public override Task<IEnumerable<AiModel>> GetModelsAsync() ...  │
   │  }                                                                    │
   └────────────────────────────────────────────────────────────────────────┘

4. DI REGISTRATION
   ┌────────────────────────────────────────────────────────────────────────┐
   │  public class MyComposer : IComposer                                  │
   │  {                                                                    │
   │      public void Compose(IUmbracoBuilder builder)                     │
   │      {                                                                │
   │          builder.AiProviders()                                        │
   │              .Add<CustomProvider>()                                   │
   │              .Exclude<UnwantedProvider>();                            │
   │                                                                       │
   │          builder.AiChatMiddleware()                                   │
   │              .Append<LoggingMiddleware>()                             │
   │              .InsertBefore<LoggingMiddleware, TracingMiddleware>();   │
   │      }                                                                │
   │  }                                                                    │
   └────────────────────────────────────────────────────────────────────────┘
```

## Umbraco.Ai.Prompt Features

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        UMBRACO.AI.PROMPT                                    │
│                    Prompt Template Management                               │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                            AiPrompt                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  Id          : Guid           │  Unique identifier                         │
│  Alias       : string         │  URL-safe identifier for API access        │
│  Name        : string         │  Display name                              │
│  Description : string?        │  Optional description                      │
│  Content     : string         │  Template with {{placeholders}}            │
│  ProfileId   : Guid?          │  Links to AI profile for execution         │
│  Tags        : string[]       │  Categorization tags                       │
│  IsActive    : bool           │  Enable/disable flag                       │
│  Scope       : AiPromptScope? │  Where prompt can be executed              │
└─────────────────────────────────────────────────────────────────────────────┘

FEATURES:
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ Template        │  │ Variable        │  │ Direct          │
│ Management      │  │ Substitution    │  │ Execution       │
│                 │  │                 │  │                 │
│ • CRUD ops      │  │ • {{name}}      │  │ • Execute with  │
│ • Alias-based   │  │ • {{context}}   │  │   variables     │
│   access        │  │ • Custom vars   │  │ • Returns AI    │
│ • Tagging       │  │                 │  │   response      │
└─────────────────┘  └─────────────────┘  └─────────────────┘

EXAMPLE PROMPT:
┌─────────────────────────────────────────────────────────────────────────────┐
│  Name: "Product Description Generator"                                      │
│  Alias: "product-description"                                              │
│  Content:                                                                   │
│    "Write a compelling product description for {{productName}}.            │
│     Key features: {{features}}                                             │
│     Target audience: {{audience}}                                          │
│     Tone: {{tone}}"                                                        │
│  ProfileId: <linked-profile-id>                                            │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Umbraco.Ai.Agent Features

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         UMBRACO.AI.AGENT                                    │
│                      AI Agent Management                                    │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                             AiAgent                                         │
├─────────────────────────────────────────────────────────────────────────────┤
│  Id           : Guid          │  Unique identifier                         │
│  Alias        : string        │  URL-safe identifier for API access        │
│  Name         : string        │  Display name                              │
│  Description  : string?       │  Optional description                      │
│  ProfileId    : Guid          │  Required link to AI profile               │
│  Instructions : string?       │  Agent behavior/personality instructions   │
│  IsActive     : bool          │  Enable/disable flag                       │
└─────────────────────────────────────────────────────────────────────────────┘

FEATURES:
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│ Agent           │  │ Behavior        │  │ Profile         │
│ Definitions     │  │ Configuration   │  │ Integration     │
│                 │  │                 │  │                 │
│ • CRUD ops      │  │ • Instructions  │  │ • Links to      │
│ • Alias-based   │  │ • Personality   │  │   connection    │
│   access        │  │ • Constraints   │  │ • Model config  │
│ • Activation    │  │                 │  │ • Temperature   │
└─────────────────┘  └─────────────────┘  └─────────────────┘

EXAMPLE AGENT:
┌─────────────────────────────────────────────────────────────────────────────┐
│  Name: "Customer Support Agent"                                             │
│  Alias: "support-agent"                                                    │
│  ProfileId: <gpt-4-profile-id>                                             │
│  Instructions:                                                              │
│    "You are a helpful customer support agent for an e-commerce website.    │
│     Always be polite and professional. If you don't know the answer,       │
│     offer to connect the customer with a human agent. Never discuss        │
│     competitor products or share internal company information."            │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Database Schema

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          DATABASE TABLES                                    │
└─────────────────────────────────────────────────────────────────────────────┘

UMBRACO.AI (CORE)                    Prefix: UmbracoAi_
┌─────────────────────────────────┐  ┌─────────────────────────────────┐
│      UmbracoAiConnections       │  │       UmbracoAiProfiles         │
├─────────────────────────────────┤  ├─────────────────────────────────┤
│ Id          : uniqueidentifier  │  │ Id          : uniqueidentifier  │
│ Alias       : nvarchar(255)     │  │ Alias       : nvarchar(255)     │
│ Name        : nvarchar(255)     │  │ Name        : nvarchar(255)     │
│ ProviderId  : nvarchar(100)     │  │ ConnectionId: uniqueidentifier  │◄─┐
│ Settings    : nvarchar(max)     │  │ Capability  : int               │  │
│ DateCreated : datetime2         │  │ ModelId     : nvarchar(255)     │  │
│ DateModified: datetime2         │  │ Settings    : nvarchar(max)     │  │
└─────────────────────────────────┘  │ IsDefault   : bit               │  │
              │                       │ DateCreated : datetime2         │  │
              │ FK                    │ DateModified: datetime2         │  │
              └───────────────────────┴─────────────────────────────────┘  │
                                                                           │
UMBRACO.AI.PROMPT                    Prefix: UmbracoAiPrompt_              │
┌─────────────────────────────────┐                                        │
│       UmbracoAiPrompts          │                                        │
├─────────────────────────────────┤                                        │
│ Id          : uniqueidentifier  │                                        │
│ Alias       : nvarchar(255)     │                                        │
│ Name        : nvarchar(255)     │                                        │
│ Description : nvarchar(max)     │                                        │
│ Content     : nvarchar(max)     │                                        │
│ ProfileId   : uniqueidentifier  │────────────────────────────────────────┤
│ Tags        : nvarchar(max)     │                                        │
│ IsActive    : bit               │                                        │
│ Scope       : nvarchar(max)     │                                        │
│ DateCreated : datetime2         │                                        │
│ DateModified: datetime2         │                                        │
└─────────────────────────────────┘                                        │
                                                                           │
UMBRACO.AI.AGENT                     Prefix: UmbracoAiAgent_               │
┌─────────────────────────────────┐                                        │
│        UmbracoAiAgents          │                                        │
├─────────────────────────────────┤                                        │
│ Id          : uniqueidentifier  │                                        │
│ Alias       : nvarchar(255)     │                                        │
│ Name        : nvarchar(255)     │                                        │
│ Description : nvarchar(max)     │                                        │
│ ProfileId   : uniqueidentifier  │────────────────────────────────────────┘
│ Instructions: nvarchar(max)     │
│ IsActive    : bit               │
│ DateCreated : datetime2         │
│ DateModified: datetime2         │
└─────────────────────────────────┘
```

## Technology Stack

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          TECHNOLOGY STACK                                   │
└─────────────────────────────────────────────────────────────────────────────┘

BACKEND                              FRONTEND
┌─────────────────────────────────┐  ┌─────────────────────────────────┐
│ .NET 10.0                       │  │ Lit Web Components              │
│ Umbraco CMS 17.x                │  │ TypeScript                      │
│ Entity Framework Core 10.x      │  │ Vite (build tool)               │
│ Microsoft.Extensions.AI         │  │ @hey-api/openapi-ts             │
│ OpenAI SDK v2.x                 │  │                                 │
└─────────────────────────────────┘  └─────────────────────────────────┘

DATABASE                             PACKAGE MANAGEMENT
┌─────────────────────────────────┐  ┌─────────────────────────────────┐
│ SQL Server (production)         │  │ Central Package Management      │
│ SQLite (development/testing)    │  │ (Directory.Packages.props)      │
│ EF Core Migrations              │  │ NuGet packages                  │
└─────────────────────────────────┘  │ npm packages                    │
                                     └─────────────────────────────────┘
```

## API Endpoints Summary

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         MANAGEMENT API                                      │
│                    /umbraco/ai/management/api/v1/                           │
└─────────────────────────────────────────────────────────────────────────────┘

PROVIDERS (Core)
  GET    /providers                    List all registered providers
  GET    /providers/{providerId}       Get provider details

CONNECTIONS (Core)
  GET    /connections                  List all connections
  GET    /connections/{idOrAlias}      Get connection by ID or alias
  POST   /connections                  Create connection
  PUT    /connections/{idOrAlias}      Update connection
  DELETE /connections/{idOrAlias}      Delete connection
  POST   /connections/{idOrAlias}/test Test connection
  GET    /connections/{idOrAlias}/models      Get available models
  GET    /connections/{idOrAlias}/capabilities Get supported capabilities

PROFILES (Core)
  GET    /profiles                     List all profiles
  GET    /profiles/{idOrAlias}         Get profile by ID or alias
  POST   /profiles                     Create profile
  PUT    /profiles/{idOrAlias}         Update profile
  DELETE /profiles/{idOrAlias}         Delete profile

PROMPTS (Add-on)
  GET    /prompts                      List all prompts (paged)
  GET    /prompts/{idOrAlias}          Get prompt by ID or alias
  GET    /prompts/profile/{profileId}  Get prompts by profile
  POST   /prompts                      Create prompt
  PUT    /prompts/{idOrAlias}          Update prompt
  DELETE /prompts/{idOrAlias}          Delete prompt

AGENTS (Add-on)
  GET    /agents                       List all agents (paged)
  GET    /agents/{idOrAlias}           Get agent by ID or alias
  GET    /agents/profile/{profileId}   Get agents by profile
  POST   /agents                       Create agent
  PUT    /agents/{idOrAlias}           Update agent
  DELETE /agents/{idOrAlias}           Delete agent
```

---

## Summary

**Umbraco.Ai** provides a clean, provider-agnostic AI integration layer for Umbraco CMS built on Microsoft.Extensions.AI. The architecture emphasizes:

1. **Extensibility** - Custom providers, capabilities, and middleware can be added via collection builders
2. **Configuration-driven** - Settings can reference configuration values for secure credential management
3. **Separation of concerns** - Clear boundaries between Core, Persistence, Web, and Frontend layers
4. **Umbraco integration** - Uses Umbraco's DI patterns, backoffice security, and composer system
5. **Multiple database support** - SQL Server and SQLite via EF Core migrations

The add-on packages (Prompt and Agent) extend the core functionality to provide prompt template management and AI agent definitions, both building on the core Profile system for AI configuration.
