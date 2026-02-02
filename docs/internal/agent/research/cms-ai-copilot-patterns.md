# CMS AI/Copilot/Agent Integration Patterns Research

> Research conducted December 2025 - exploring how CMS platforms integrate AI assistants, copilots, and agents into their editing experiences.

## Executive Summary

The CMS industry has largely converged on **contextual, document-scoped AI assistance** rather than global conversational copilots. Most implementations are "AI buttons" (one-shot operations) rather than true conversational interfaces. Cross-document and bulk operations are handled through separate workflows with review/approve patterns.

**Key findings:**
- No major CMS has solved the "AI modifying content that's simultaneously open in an editor" problem. They sidestep it through architectural separation.
- 2025 marks a shift from "assistive AI" to "agentic AI" - autonomous agents that execute multi-step workflows.
- New patterns emerging: agent orchestration, adaptive content, connected context across tools.

---

## Table of Contents

### Part 1: Industry Research
1. [The Core Challenge](#the-core-challenge)
2. [Industry Patterns](#industry-patterns)
3. [Platform-Specific Analysis](#platform-specific-analysis)
   - [Drupal AI](#drupal-ai)
   - [Sanity AI Assist](#sanity-ai-assist)
   - [Contentful](#contentful)
   - [Sitecore Stream](#sitecore-stream)
   - [Storyblok](#storyblok)
   - [Strapi](#strapi)
   - [WordPress](#wordpress-jetpack-ai--plugins)
   - [Adobe Experience Manager](#adobe-experience-manager-aem)
   - [Optimizely Opal](#optimizely-opal)
   - [Kentico AIRA](#kentico-aira)
   - [Kontent.ai](#kontentai)
   - [Webflow AI](#webflow-ai)
   - [Wix AI](#wix-ai)
   - [Notion AI](#notion-ai)
   - [Shopify Magic](#shopify-magic)
4. [Emerging Patterns (2025+)](#emerging-patterns-2025)
5. [UX Pattern Comparison](#ux-pattern-comparison)

### Part 2: Umbraco Strategy
6. [Technical Considerations](#technical-considerations)
7. [Two Execution Models](#two-execution-models)
8. [Package Architecture](#package-architecture)
9. [The Editor State Problem](#the-editor-state-problem)
10. [Copilot Integration Approach](#copilot-integration-approach)
11. [Industry Features Mapped](#industry-features-mapped)
12. [Competitive Positioning & Future](#competitive-positioning--future)

### Appendix
13. [Sources](#sources)

---

# PART 1: INDUSTRY RESEARCH

---

## The Core Challenge

### Frontend vs Backend Tools

When building a Copilot/Agent system for a CMS, a fundamental question arises:

**Where should content changes happen?**

| Scenario | Server Knowledge | Editor State | Problem |
|----------|------------------|--------------|---------|
| New document (unsaved) | None | Has content | Server can't modify what it doesn't know |
| Saved doc with local changes | Stale version | Current version | Sync conflict risk |
| Saved doc, no local changes | Current | Current | Clean - server changes work |

Only the third scenario works cleanly with server-side tools.

### The Sync Problem

If an AI agent makes changes server-side to content that's open in an editor:
- User loses unsaved changes
- Or user sees stale content
- Or complex merge/sync logic required

**Industry solution:** Avoid the problem entirely through architectural separation.

---

## Industry Patterns

### The AI Interaction Spectrum

```
Simple <----------------------------------------------------------------------> Complex

[AI Buttons]    [Instructions]    [Assistive AI]    [Copilot]    [Agentic AI]
 Fixed prompts   Custom, reusable  In-editor help    Conversational Autonomous
 One action      field-aware       suggestions       multi-turn     multi-step
 Per-field       Single execution  contextual        context memory workflows
```

### Evolution of AI Integration (2024-2025)

| Era | Characteristic | Examples |
|-----|----------------|----------|
| **AI Buttons** | Predefined, one-shot, per-field | Storyblok, Jetpack |
| **Custom Instructions** | User-written, reusable, field-aware | Sanity AI Assist |
| **Assistive AI** | In-editor suggestions, contextual | Kontent.ai (2024), Kentico |
| **Agent Orchestration** | Multiple specialized agents | Optimizely Opal, Notion 3.0 |
| **Agentic CMS** | Autonomous multi-step workflows | Kontent.ai (2025), Contentstack |
| **Adaptive Content** | Runtime personalization | Wix |
| **Prompt-to-Production** | Full site/app generation | Webflow |

### Three Levels of AI Integration

#### Level 1: Field/Editor Assistance
- Inline in editor, current document only
- Examples: Sanity AI Assist, Storyblok, Jetpack AI, Sitecore Stream
- Pattern: AI buttons or custom instructions attached to fields

#### Level 2: Batch Operations
- Separate UI from editor
- Queue -> Review -> Approve workflow
- Examples: Contentful AI Actions, Sanity Agent Actions

#### Level 3: Autonomous Agents
- Background processes, periodic audits
- Multi-step workflows without human handoffs
- Examples: Acquia Governance Agent, Contentstack Agent OS, Optimizely Opal

---

## Platform-Specific Analysis

### Drupal AI

**Approach:** Split into two separate modules

| Module | Layer | Purpose | Touches Open Editor? |
|--------|-------|---------|---------------------|
| CKEditor AI | Frontend | Rich text editing assistance | Yes (directly) |
| AI Agents | Backend | Config/structure manipulation | No (via chatbot) |

**Key insight:** CKEditor AI is client-side focused - changes stream directly into the editor. AI Agents work through a separate conversational/chatbot interface, not inline in the editor.

**Sources:**
- [CKEditor AI Writing Agent](https://www.drupal.org/project/ckeditor_ai_agent)
- [AI Agents](https://www.drupal.org/project/ai_agents)

---

### Sanity AI Assist

**Approach:** Custom "Instructions" - a middle ground between buttons and copilot

**How it works:**
1. Instructions are user-written prompts with field references
2. Syntax: `"Given <Document field: Body>, extract all ingredients"`
3. Attached at schema level to document/field types
4. Saved and reusable across team
5. Single execution (not conversational)

**Key features:**
- Field reference syntax: `<Document field: FieldName>`
- Can read from one field, write to another
- Can create array items, references, etc.
- Temperature setting for output consistency
- Locale/timezone awareness

**Capabilities:**
- Writing, summarizing, translating content
- Generating alternatives for A/B testing
- Populating "intelligent fields" via AI
- Meta descriptions, taxonomy suggestions

**2025 Updates:**
- Canvas: AI-assisted freeform writing environment (separate from structured editing)
- Agent Actions: Programmatic AI for auditing, suggesting updates, creating work packages

**Sources:**
- [Sanity AI Assist Announcement](https://www.sanity.io/blog/sanity-ai-assist-announcement)
- [Sanity AI Assist Plugin](https://www.sanity.io/plugins/ai-assist)
- [Install and Configure Guide](https://www.sanity.io/docs/install-and-configure-sanity-ai-assist)
- [What's New May 2025](https://www.sanity.io/blog/what-s-new-may-2025)

---

### Contentful

**Approach:** AI Actions for bulk operations

**Key features:**
- Run AI across hundreds of entries at once
- Review, approve, or refine changes before go-live
- Pre-built templates or custom AI actions
- Uses content from Contentful and external sources as context

**Use cases:**
- Translation
- SEO optimization
- Brand governance
- Copywriting at scale

**MCP Integration:** Provides MCP server for connecting external AI assistants

**Sources:**
- [AI Actions](https://www.contentful.com/products/ai-actions/)
- [Contentful and AI](https://www.contentful.com/products/ai/)
- [Model Context Protocol Introduction](https://www.contentful.com/blog/model-context-protocol-introduction/)

---

### Sitecore Stream

**Approach:** Multiple specialized AI Copilots

**Five Copilots:**
1. **Brand Copilot** - Brand-aware chats, brainstorming, brief generation
2. **Campaign Copilot** - Campaign planning assistance
3. **Content Copilot** - Content creation, refinement, translation
4. **Experience Copilot** - Visual search, Q&A generation
5. **Optimization Copilot** - Performance optimization

**Key features:**
- Content Assistant embedded in platform for text fields
- AI-assisted page translation (entire page at once)
- AI-assisted content extraction (raw input -> structured content)
- Brand kit integration for on-brand generation

**Sources:**
- [Copilots and Agents Documentation](https://doc.sitecore.com/stream/en/users/sitecore-stream/copilots-and-agents.html)
- [AI in Experience Platform](https://doc.sitecore.com/xp/en/users/latest/sitecore-experience-platform/ai-in-experience-platform.html)
- [Sitecore Stream Gets Smarter](https://www.cmswire.com/digital-experience/sitecore-stream-gets-smarter-with-ai-copilots-and-agentic-workflows/)

---

### Storyblok

**Approach:** Native AI tools + separate innovation spaces

**Native AI Features:**
- AI Branding
- AI Translation (translate entire pages with one click)
- AI Alt-text
- Richtext Assistant
- Pluggable AI provider (OpenAI, Gemini, Claude, etc.)

**Innovation Features (Storyblok Labs):**
- Ideation Room: Collaborative AI-assisted content creation space
- Concept Room: Visual project structure design
- Strata: Vector data layer for RAG workflows and AI-native search

**Sources:**
- [AI Features in Storyblok](https://www.storyblok.com/mp/ai-features)
- [Storyblok AI Updates 2025](https://sengo.com/resources/news/article/storybloks-next-chapter/)
- [Storyblok CMS Innovations](https://www.storyblok.com/mp/storyblok-unveils-cms-innovations)

---

### Strapi

**Approach:** AI for Content Type Building (schema, not content)

**Strapi AI (Private Beta 2025):**
- Conversational assistant for schema creation
- Transforms project briefs, code repos, or Figma designs into content models
- Generates Collection Types, Components, relationships
- Visual change-tracking and drag-and-drop reordering

**Media Library AI:**
- Auto-create image alt text and captions

**Notable:** One of the few true "conversational" AI features, but focused on schema building rather than content editing.

**Sources:**
- [Introducing Strapi AI](https://strapi.io/blog/introducing-strapi-ai)
- [StrapiConf 2025 Announcements](https://strapi.io/blog/strapi-conf-2025-announcements)
- [AI-Powered Automations](https://strapi.io/ai)

---

### WordPress (Jetpack AI & Plugins)

**Approach:** Gutenberg block integration

**Jetpack AI Assistant:**
- Integrated into WordPress editor
- Creates text, forms, tables, lists
- Works with Gutenberg blocks

**WP AI CoPilot:**
- Compatible with Gutenberg, Classic Editor, Elementor
- Templates for blog post creation
- 150+ languages
- OpenAI API integration

**Sources:**
- [Jetpack AI Assistant](https://jetpack.com/ai/)
- [WP AI CoPilot](https://wordpress.org/plugins/ai-co-pilot-for-wp/)
- [AI Copilot Plugin](https://wordpress.org/plugins/ai-copilot/)

---

### Adobe Experience Manager (AEM)

**Approach:** Multi-modal AI with brand awareness and Adobe ecosystem integration

**Key Features:**
- **Generate Variations** - Copy and image generation within editing interface
- **Smart Suggestions** (AEM Guides) - Reuse content from existing repository
- **AI Assistant** - Available across Experience Hub, Author UI, Cloud Manager
- **Adobe Firefly integration** - Image generation and editing within DAM

**Capabilities:**
- Brand-aware content generation (tone of voice, style guidelines)
- Personalization by audience using content performance insights
- Region-specific adaptation beyond simple translation
- Convert paragraphs to lists, generate summaries, translate content

**Privacy:** No personal data used for training, data not shared between customers

**Unique aspect:** Deep integration with Adobe Express and Firefly for visual content generation alongside text.

**Sources:**
- [AI in AEM](https://experienceleague.adobe.com/en/docs/experience-manager-cloud-service/content/ai-in-aem/overview)
- [AI Assistant in AEM](https://experienceleague.adobe.com/en/docs/experience-manager-cloud-service/content/ai-in-aem/ai-assistant/ai-assistant-in-aem)
- [Generative AI for AEM Sites](https://experienceleague.adobe.com/en/docs/experience-manager-learn/cloud-service/expert-resources/cloud-5/season-3/cloud5-generative-ai-for-aem-sites)

---

### Optimizely Opal

**Approach:** Full agent orchestration platform (evolved from assistant)

**Evolution:**
- Started as AI assistant
- Evolved into **agent orchestration platform** (2025)
- Now offers **28+ purpose-built marketing agents**

**Specialized Agents Include:**
- GEO Recommendations (Generative Engine Optimization)
- Web Accessibility Evaluation
- Competitive Insights
- GA4 Traffic Report Generation
- Content Translation
- Heatmap Analysis

**Key Features:**
- Credit-based usage model (May 2025)
- **First "GEO-ready" CMS** - optimizing for AI search results
- Auto-generating Q&A fields, GEO-specific metadata
- llms.txt files, topic templates
- Bulk-applied metadata powered by Opal

**Unique aspect:** Moving from "assistant" to "agent orchestration at scale" - agents can plan, create, and optimize across the entire digital lifecycle.

**Sources:**
- [Optimizely Opal Benchmark Report](https://www.optimizely.com/insights/the-2025-optimizely-opal-ai-benchmark-report/)
- [2025 CMS Release Notes](https://support.optimizely.com/hc/en-us/articles/27677034133645-2025-CMS-SaaS-release-notes)
- [Optimizely 2025 Advancements](https://www.optimizely.com/company/press/optiwrapped/)

---

### Kentico AIRA

**Approach:** Built-in AI agent with mobile companion

**Key Features:**
- Drafting/refining copy, proposing headlines
- **One-click translation** with configurable translation rules
- **Image formatting, tagging, alt-text automation**
- Smart cropping for images
- Rich text AI transformations (emails, pages, headless items)

**AIRA Companion App:**
- Mobile extension for marketers
- Monitor performance and KPIs
- Receive alerts on the go

**Kentico Copilot (Developer-focused):**
- Code generation capabilities
- Kentico-supported AI agents for development

**Unique aspect:** Mobile companion app for marketers to monitor AI-driven insights on the go.

**Sources:**
- [AIRA](https://www.kentico.com/platform/aira)
- [Kentico 2025 Milestones](https://cmscritic.com/kentico-achieves-big-milestones-in-2025-citing-ai-innovation-and-xperience-by-kentico-as-key-drivers)
- [Xperience February 2024 Refresh](https://community.kentico.com/blog/xperience-by-kentico-refresh-february-22,-2024)

---

### Kontent.ai

**Approach:** Native AI -> Agentic CMS evolution

**2024 Features (Assistive AI):**
- **Author Assist / Content Assist** - In-the-moment support for ideation and writing
- **AI Taxonomy Tagging** - Auto-tag content based on content analysis
- **AI Translation** - Localization for global audiences
- Tone of voice adjustment
- Built natively into the platform (not bolted on)

**2025 Evolution (Agentic CMS):**

> "In 2024, AI inside the CMS meant faster authoring. In 2025, it means the CMS itself can act."

- AI as "operator" not just assistant
- Multi-step workflows without human handoffs
- Content audits, campaign localization, multimedia asset generation

**Security:** NIST AI Risk Management Framework, EU AI Act compliance, data sandboxed from model training.

**Unique aspect:** Clear articulation of the "assistive -> agentic" evolution in CMS AI.

**Sources:**
- [Agentic CMS](https://kontent.ai/blog/agentic-cms-redefining-content-management-for-the-future/)
- [From Assistive to Agentic](https://kontent.ai/blog/assistive-to-agentic-redefining-future-of-content/)
- [Native AI Capabilities](https://kontent.ai/features/introducing-native-ai-capabilities/)

---

### Webflow AI

**Approach:** Prompt-to-production (design + content + code)

**AI Site Builder:**
- Generate entire sites from description
- Creates complete, responsive website with design system
- Available in beta

**AI Assistant Capabilities:**
1. Build entire site and design system from scratch
2. Generate and refine contextually relevant copy
3. Modify page designs by creating new sections
4. Generate CMS collection items (individually and in bulk)
5. Provide CRO optimization suggestions
6. Audit and improve SEO
7. Provide contextual help from documentation

**2025 Updates (Webflow Conf):**
- **AI Code Gen** - Generate production-grade React components from prompts
- Revamped AI Assistant as conversational interface
- Generate, refine, and deploy full-stack web apps within Webflow
- Connect apps to CMS content and variables

**Unique aspect:** True prompt-to-production - generates production-grade apps, not just content.

**Sources:**
- [Webflow AI](https://webflow.com/feature/ai)
- [AI Site Builder](https://webflow.com/ai-site-builder)
- [Webflow AI Overview](https://help.webflow.com/hc/en-us/articles/34297897805715-Webflow-AI-overview)

---

### Wix AI

**Approach:** Most comprehensive AI toolkit with adaptive content

**Core AI Features:**
- **AI Website Builder** - Complete site generation from prompts
- **AI Text Creator** - Headlines, descriptions, blog posts, product descriptions
- **AI Theme Assistant** - Design customization
- **AI Section Creator** - Generate page sections
- **AI Chatbot Setup** - Automated customer support

**Adaptive Content (April 2025):**
- Dynamic content based on visitor characteristics
- Adapts based on:
  - Device type
  - Geographic location
  - Language
  - Returning visitor status
- No manual rules needed - AI-powered personalization

**Scale:** Hundreds of thousands of sites created since 2024 launch.

**Unique aspect:** Adaptive content that personalizes at runtime based on visitor context without manual configuration.

**Sources:**
- [Wix AI Features](https://www.wix.com/features/ai)
- [AI Website Builder](https://www.wix.com/ai-website-builder)
- [Adaptive Content Announcement](https://www.globenewswire.com/news-release/2025/04/23/3066430/0/en/Wix-Introduces-Adaptive-Content-Feature-with-AI-to-Personalize-Web-Experiences-for-Site-Visitors.html)

---

### Notion AI

**Approach:** AI that executes, not just suggests

**Core AI Features:**
- **AI Database Properties** - Auto-fill, keywords, summaries, translations
- **Formula AI** - Generate complex formulas from natural language
- **AI Meeting Notes** - Auto-capture and summarize conversations
- **Natural Language Search** - Find information using natural queries
- **Connected app search** - Search across Slack, Google Drive, Teams, etc.

**Notion 3.0 AI Agents (September 2025):**
- **Autonomous execution for up to 20 minutes**
- Multi-step workflows
- Deep personalization with instruction pages
- Cross-platform context from connected tools
- Teach AI your work style, company terminology, preferences

**2024-2025 Timeline:**
- Sept 2024: AI connectors for Google Docs, Sheets, Slides
- Oct 2024: Notion Forms with AI
- April 2025: Notion Mail with AI auto-labeling
- May 2025: AI Meeting Notes
- Aug 2025: Offline Mode
- Sept 2025: Notion 3.0 with AI Agents

**Unique aspect:** AI agents that can execute multi-step workflows autonomously for extended periods, pulling context from multiple connected tools.

**Sources:**
- [Notion AI](https://www.notion.com/product/ai)
- [Notion AI Features & Capabilities](https://kipwise.com/blog/notion-ai-features-capabilities)
- [Notion 3.0 AI Agents](https://max-productive.ai/ai-tools/notion-ai/)

---

### Shopify Magic

**Approach:** Commerce-focused AI with product description generation

**Native Features (Shopify Magic):**
- AI product description generation
- Included with Shopify plan
- Generates high-quality descriptions in seconds
- Commerce-focused (understands e-commerce context)

**Third-Party Apps:**
- **ChatGPT-AI Product Description** - Bulk generation, SEO optimization
- **Smartli AI** - SEO-friendly descriptions
- **Fiidom** - GPT 5.0 powered, brand voice matching
- **Hypotenuse AI** - Bulk publishing, Shopify attribute import

**Unique aspect:** Commerce-specific AI that understands product context and e-commerce best practices.

**Sources:**
- [Shopify Magic - AI Product Descriptions](https://www.shopify.com/blog/ai-product-descriptions)
- [How to Use AI for Ecommerce](https://www.shopify.com/blog/how-to-use-ai)

---

### Emerging: Agentic CMS (2025+)

**The trend:** Autonomous AI agents for governance, compliance, content operations

**Acquia Source (Dec 2024):**
- Site Builder Agent: Create multi-page campaign sites from briefs
- AI Writing Assistant Agent: SEO-optimized content generation
- Web Governance Agent (Q1 2026): Scan and fix accessibility/compliance issues

**Contentstack Agent OS (Sept 2025):**
- Shift from content management to "context management"
- Real-time personalization
- Workflow automation
- Context-driven customer experiences

**Sources:**
- [Acquia AI Agents Launch](https://www.cmswire.com/digital-experience/acquia-launches-ai-agents-in-saas-cms-for-content-automation/)
- [The Rise of Agentic CMS](https://www.boye-co.com/blog/2025/9/the-rise-of-agentic-cms)
- [Contentstack Agent OS](https://www.vktr.com/ai-news/contentstack-agent-os-ai-powered-cms-for-context-driven-digital-experiences/)

---

## Emerging Patterns (2025+)

### 1. Agent Orchestration
Not just one AI assistant, but **multiple specialized agents** that can be orchestrated.

| Platform | # of Agents | Capability |
|----------|-------------|------------|
| Optimizely Opal | 28+ | Marketing-specific agents |
| Notion 3.0 | Configurable | Execute for up to 20 minutes |
| Sitecore Stream | 5 | Domain-specific copilots |

### 2. Adaptive/Dynamic Content
AI that **adapts content at runtime** based on visitor context.

- **Wix Adaptive Content** - Device, location, language, return visitor
- No manual rules needed
- AI-powered personalization at scale

### 3. Connected Context
AI that pulls context from **multiple connected tools**.

| Platform | Connected Tools |
|----------|----------------|
| Notion | Slack, Google Drive, Teams, email |
| Optimizely | GA4, heatmaps, competitive data |
| Contentful | External sources via MCP |

### 4. GEO (Generative Engine Optimization)
New category: optimizing content for **AI search results**, not just traditional SEO.

- Optimizely: First "GEO-ready" CMS
- Auto-generating Q&A fields
- llms.txt files
- Topic templates

### 5. Mobile Companion Apps
AI insights and monitoring **available on mobile**.

- Kentico AIRA Companion App
- Monitor performance, receive alerts
- Access KPIs on the go

### 6. Prompt-to-Production
Full site/app generation from prompts.

- Webflow AI Code Gen
- Wix AI Website Builder
- Production-grade output, not just drafts

---

## UX Pattern Comparison

### AI Interaction Models

| Aspect | AI Buttons | Instructions (Sanity) | Assistive AI | Copilot | Agentic AI |
|--------|------------|----------------------|--------------|---------|------------|
| Prompts | Predefined | User-written, saved | Contextual | Free-form | Goal-based |
| Conversation | No | No | Limited | Yes | Yes |
| Context memory | No | No | Session | Yes | Extended |
| Field awareness | Single field | Cross-field | Document | Document-wide | Multi-document |
| Autonomy | None | None | Low | Medium | High |
| Duration | Instant | Instant | Instant | Minutes | Up to 20 min |

### Context Scope Patterns

| Pattern | Example | Scope |
|---------|---------|-------|
| Per-field buttons | Storyblok Alt-text | Single field |
| Per-document instructions | Sanity AI Assist | Current document |
| Bulk actions UI | Contentful AI Actions | Selected entries |
| Background agents | Acquia Governance | Entire site |
| Connected agents | Notion 3.0 | Cross-platform |

### Cross-Document Operations

**Nobody does "navigate and show"** - no CMS has the AI visibly navigate the UI for the user.

All cross-document operations are either:
1. **Refused** (out of scope for contextual tools)
2. **Silent with reporting** ("Done. Changed 15 pages. [View report]")
3. **Queue-based** (batch -> review -> approve)

---

# PART 2: UMBRACO STRATEGY

---

## Technical Considerations

### The Property Editor Data Format Problem

For Umbraco specifically, AI needs to understand complex property editor data formats:

```
User: "Extract ingredients from the body text"

AI needs to produce Block List format:
{
  "contentData": [{
    "contentTypeKey": "abc-123-guid",     // How does AI know?
    "key": "generated-guid",
    "values": {
      "name": "flour",
      "amount": "2",
      "unit": ["cups"]                    // String? Array? GUID?
    }
  }]
}
```

### Potential Solutions

#### 1. Schema in Prompt
Include data schema in system prompt. AI generates correct format directly.
- Pro: Direct
- Con: Prompt bloat, fragile for complex editors

#### 2. Tool-Based Abstraction
Give AI tools like `addIngredientBlock(name, amount, unit)` - tool handles format.
- Pro: Format encapsulated, AI just calls functions
- Con: Need tools per doc type/property

#### 3. Two-Stage Transform
AI generates semantic JSON, transformer converts to property editor format.
- Pro: AI stays simple
- Con: Need transformers per property editor type

#### 4. Property Editor AI Contracts
Each property editor exposes `GetAiInputSchema()` and `TransformAiOutput()`.
- Pro: Property editors own their AI integration
- Con: Requires updating all editors, third-party issues

#### 5. JSON Schema Generation
Auto-generate JSON Schema from doc type + property editor configs at runtime.
- Pro: Automated, stays in sync
- Con: Complex editors may not map cleanly

**Recommended:** Combination of Tool-Based (for complex editors) + JSON Schema (for simple fields)

---

## Two Execution Models

A key insight from this research is that there are two fundamentally different execution models:

### Model A: Template-Based (One-Shot)
```
1. Resolve template (fill in field values)
2. Call LLM once
3. Parse response
4. System applies result to target field(s)

LLM generates output -> System handles application
```

**Characteristics:**
- Deterministic
- System controls input/output transformation
- No tools needed
- Predictable results

### Model B: Agentic (Tool Loop)
```
1. Give agent a goal
2. Agent decides what tools to call
3. Agent calls tools, sees results
4. Agent decides next step
5. Repeat until goal achieved

LLM is in control -> Agent applies via tools
```

**Characteristics:**
- Non-deterministic
- LLM decides what to do
- Requires tool infrastructure
- Flexible but less predictable

---

## Package Architecture

Based on this research, the Umbraco.AI ecosystem can be cleanly separated:

```
+------------------------------------------------------------------+
|  LAYER 1: Foundation                                              |
+------------------------------------------------------------------+
|  Umbraco.AI (Core)                                                |
|  +-- Connections (API keys, endpoints)                            |
|  +-- Profiles (model settings)                                    |
|  +-- Simple Chat API (for basic integrations)                     |
+------------------------------------------------------------------+
                              |
              +---------------+---------------+
              v                               v
+----------------------------+    +------------------------------+
|  LAYER 2A: One-Shot        |    |  LAYER 2B: Agentic           |
+----------------------------+    +------------------------------+
|  Umbraco.AI.Prompt         |    |  Umbraco.AI.Agent            |
|                            |    |                              |
|  * Fixed prompts           |    |  * Agent definitions         |
|  * Custom prompts          |    |  * Tool registry             |
|  * Saved prompts           |    |  * Tool permissions          |
|  * One-shot execution      |    |  * Agentic execution         |
|  * System applies result   |    |  * Agent applies via tools   |
|  * Schema awareness for    |    |  * Conversational support    |
|    output transformation   |    |  * Frontend + backend tools  |
|                            |    |  * (+ Copilot UI)            |
|  Uses: Model A             |    |  Uses: Model B               |
+----------------------------+    +------------------------------+
                              |
                              v
              +-------------------------------+
              |  LAYER 3: Orchestration       |
              +-------------------------------+
              |  Umbraco.AI.Workflow          |
              |                               |
              |  * Chain prompt executions    |
              |  * Chain agent executions     |
              |  * Batch processing           |
              |  * Review/approve workflows   |
              |  * Conditional logic          |
              |                               |
              |  Orchestrates: Model A & B    |
              +-------------------------------+
```

### Package Responsibilities

| Package | Execution Model | Who Applies Result | Use Case |
|---------|-----------------|-------------------|----------|
| **Umbraco.AI** | N/A (foundation) | N/A | Connect to AI providers |
| **Umbraco.AI.Prompt** | Model A (one-shot) | System | "Do this specific thing" |
| **Umbraco.AI.Agent** | Model B (agentic) | Agent via tools | "Help me with this" |
| **Umbraco.AI.Workflow** | Orchestrates A & B | Varies | Complex multi-step tasks |

### Prompt vs Agent: Clear Boundaries

| Aspect | Umbraco.AI.Prompt | Umbraco.AI.Agent |
|--------|-------------------|------------------|
| **Prompts** | Fixed, custom, saved | Agent system prompts |
| **Execution** | Single LLM call | Tool loop until complete |
| **Result application** | System transforms & applies | Agent calls tools |
| **Conversation** | No | Yes |
| **Tools** | No | Yes (frontend + backend) |
| **Predictability** | High | Lower |
| **Complexity** | Simple | Complex |

---

## The Editor State Problem

For agentic execution (Model B), the original challenge remains:

| Scenario | Problem | Solution |
|----------|---------|----------|
| New document (unsaved) | Server doesn't know content | Frontend tools |
| Saved doc with local changes | Server has stale version | Frontend tools |
| Saved doc, no changes | Clean state | Backend tools OK |
| Cross-document operations | Not in editor context | Backend tools + reporting |

**For Prompt (Model A):** This problem doesn't exist - the system IS the editor, so it applies changes directly to local state.

**For Agent (Model B):** The Copilot UI must be context-aware:
- In editor -> Use frontend tools for local changes
- Outside editor -> Use backend tools with results reporting

---

## Copilot Integration Approach

### The IDE Copilot Pattern

Modern IDE Copilots (GitHub Copilot, Cursor, Claude Code) have established a pattern that users are becoming familiar with:

```
+------------------------------------------------------------------+
|  IDE COPILOT PATTERN                                              |
+------------------------------------------------------------------+
|  * Single global interface (not per-file)                         |
|  * Context-aware (knows current file, open tabs, workspace)       |
|  * Scope inferred from request, not explicit selection            |
|  * "Fix this function" -> current file                            |
|  * "Find all usages of X" -> workspace-wide                       |
|  * No mode switching required                                     |
+------------------------------------------------------------------+
```

This is becoming the mental model users expect from AI assistants.

### Recommended Approach for Umbraco

**Single Global Copilot with Smart Context Awareness**

```
+------------------------------------------------------------------+
|  COPILOT (always available via header button)                     |
+------------------------------------------------------------------+
|  Context bar: "D Editing: About Us (unsaved changes)"             |
|  ---------------------------------------------------------------- |
|                                                                   |
|  User: "Improve the intro paragraph"                              |
|  Copilot: [Shows proposed change to Body field]                   |
|           [Apply to editor] <- stays unsaved                      |
|                                                                   |
|  User: "Do the same for all blog posts"                           |
|  Copilot: "This will update 24 blog posts. Run as background      |
|           task? [Start Task] [Cancel]"                            |
|                                                                   |
+------------------------------------------------------------------+
```

**Key Design Elements:**

1. **Context bar** - Shows what Copilot "sees" (current document, unsaved state, current section)

2. **Smart scope inference** - Copilot determines scope from:
   - The request itself ("this" vs "all")
   - Current context (which editor is open, if any)
   - Operation type (query vs mutation)

3. **Clear execution distinction**:
   - **Local operations** -> Immediate, changes editor state, stays unsaved
   - **Global operations** -> Background task, confirmation required, results reporting

4. **Confirmation for global operations** - Any operation affecting multiple items requires explicit confirmation

5. **No separate "Agent section"** - All interactions through single Copilot UI

### Scope Inference Examples

| User Request | Context | Inferred Scope | Execution |
|--------------|---------|----------------|-----------|
| "Improve this title" | In editor | Local (current doc) | Frontend tool |
| "Translate this page" | In editor | Local (current doc) | Frontend tool |
| "Update all product SEO" | Anywhere | Global (multiple docs) | Background task |
| "Find pages without meta desc" | Anywhere | Global (query) | Instant query |
| "Improve the title" | In editor | Local (current doc) | Frontend tool |
| "Improve the title" | On dashboard | Ambiguous -> Ask user | Clarify first |

### Handling the Editor State Problem

| Copilot Location | Data Source | Result Application |
|------------------|-------------|-------------------|
| In content editor | Editor state (including unsaved) | Frontend tools -> editor state |
| In tree/dashboard | Server state | Backend tools -> server |
| Anywhere (global op) | Server state | Background task -> server |

**Principle:** Copilot always works with what the user is currently "looking at":
- In editor -> Works with editor buffer (like IDE)
- Not in editor -> Works with server state

---

## Industry Features Mapped

### Feature Coverage Matrix

| Industry Feature | Example Platforms | Umbraco.AI Package | Notes |
|------------------|-------------------|-------------------|-------|
| **AI Buttons (per-field)** | Storyblok, Jetpack, Sitecore | **Prompt** | Fixed prompts, one-shot |
| **Custom Instructions** | Sanity AI Assist | **Prompt** | Custom prompts with field refs |
| **Saved/Reusable Prompts** | Sanity | **Prompt** | Saved prompts |
| **Schema-aware generation** | Sanity, AEM | **Prompt** | Output transformation |
| **Conversational Copilot** | Strapi (schema), Notion | **Agent** | Multi-turn, tool-based |
| **Tool execution** | Drupal AI Agents | **Agent** | Frontend + backend tools |
| **Background tasks** | Contentful AI Actions | **Agent** | Via Copilot global scope |
| **Bulk operations** | Contentful, Sanity Agent Actions | **Workflow** | Batch processing |
| **Multi-step workflows** | Acquia, Contentstack | **Workflow** | Orchestration |
| **Queue -> Review -> Approve** | Contentful | **Workflow** | Review patterns |
| **Agent orchestration** | Optimizely Opal | **Workflow** | Chain agents |
| **Governance agents** | Acquia, Kontent.ai | **Workflow** | Scheduled/triggered |

### Package Capability Summary

#### Umbraco.AI.Prompt
Covers industry patterns:
- AI Buttons (Storyblok, Jetpack, Sitecore style)
- Custom Instructions (Sanity style)
- Saved/Reusable prompts
- Field-level AI assistance
- One-shot text generation
- Translation assistance
- Alt-text generation
- SEO description generation

**Execution:** Model A (template-based, system applies result)

#### Umbraco.AI.Agent
Covers industry patterns:
- Conversational Copilot (rare in CMS - differentiator)
- Tool-based execution
- Context-aware assistance
- Cross-field operations
- Background task execution
- Frontend tools (editor state)
- Backend tools (server state)

**Execution:** Model B (agentic, agent applies via tools)

#### Umbraco.AI.Workflow
Covers industry patterns:
- Bulk operations (Contentful AI Actions style)
- Multi-step workflows (Contentstack Agent OS style)
- Agent orchestration (Optimizely Opal style)
- Queue -> Review -> Approve patterns
- Scheduled/triggered tasks
- Governance and audit workflows
- Cross-document operations at scale

**Execution:** Orchestrates Model A and Model B

### Industry Pattern Alignment

```
+------------------------------------------------------------------+
|  INDUSTRY PATTERNS              ->    UMBRACO.AI COVERAGE         |
+------------------------------------------------------------------+
|                                                                   |
|  AI Buttons (most CMS)          ->    Umbraco.AI.Prompt           |
|  Custom Instructions (Sanity)   ->    Umbraco.AI.Prompt           |
|                                                                   |
|  Assistive AI (Kontent.ai '24)  ->    Umbraco.AI.Prompt           |
|  Content Copilot (Sitecore)     ->    Umbraco.AI.Agent            |
|                                                                   |
|  AI Actions (Contentful)        ->    Umbraco.AI.Workflow         |
|  Agent Actions (Sanity '25)     ->    Umbraco.AI.Workflow         |
|  Agentic CMS (Kontent.ai '25)   ->    Umbraco.AI.Workflow         |
|                                                                   |
|  Agent Orchestration (Optimizely) ->  Umbraco.AI.Workflow         |
|  Governance Agents (Acquia)     ->    Umbraco.AI.Workflow         |
|                                                                   |
+------------------------------------------------------------------+
```

---

## Competitive Positioning & Future

### Differentiation Opportunities

| Opportunity | Industry Status | Umbraco Potential |
|-------------|-----------------|-------------------|
| True conversational Copilot | Rare (most use buttons) | Differentiator |
| Property editor awareness | None solve this well | Innovation area |
| Clean Model A/B separation | Not articulated elsewhere | Architectural clarity |
| Agent orchestration | Enterprise only | Could democratize |
| GEO optimization | Optimizely only | Early mover opportunity |

### Competitive Positioning

| Capability | Industry Status | Umbraco.AI Coverage |
|------------|-----------------|---------------------|
| Per-field AI buttons | Common | Prompt |
| Custom instructions | Sanity only | Prompt |
| True conversational Copilot | Rare | Agent (differentiator) |
| IDE-style global Copilot | None in CMS | Agent (differentiator) |
| Bulk AI operations | Enterprise CMS | Workflow |
| Agent orchestration | Enterprise only | Workflow (democratize) |
| Clean execution model separation | Not articulated | Model A/B clarity |

### Future Opportunities (Not Currently Covered)

| Industry Feature | Platform | Potential Package |
|------------------|----------|-------------------|
| Adaptive Content (runtime) | Wix | Future: Umbraco.AI.Personalization? |
| GEO Optimization | Optimizely | Future: Workflow recipes? |
| Mobile Companion | Kentico | Future: Mobile app? |
| Connected Context (Slack, etc.) | Notion | Future: Integrations? |
| AI Image Generation | AEM + Firefly | Future: Media integration? |

### Challenges to Solve

1. **Property editor data formats** - How does the system (Model A) or agent (Model B) know the correct format for Block Lists, etc.?
   - Model A: Schema awareness + output transformation
   - Model B: Tools abstract the complexity

2. **Frontend vs backend tools** - Agent package needs both:
   - Frontend tools for editor context
   - Backend tools for cross-document operations

3. **Scope boundaries** - Clear UX signals for what Copilot can do in each context

4. **Workflow integration** - How Workflow orchestrates both Prompt and Agent executions

---

# APPENDIX: Sources

---

## Drupal
- [CKEditor AI Writing Agent](https://www.drupal.org/project/ckeditor_ai_agent)
- [AI Agents](https://www.drupal.org/project/ai_agents)

## Sanity
- [Sanity AI Assist Announcement](https://www.sanity.io/blog/sanity-ai-assist-announcement)
- [Sanity AI Assist Plugin](https://www.sanity.io/plugins/ai-assist)
- [Install and Configure Guide](https://www.sanity.io/docs/install-and-configure-sanity-ai-assist)
- [What's New May 2025](https://www.sanity.io/blog/what-s-new-may-2025)

## Contentful
- [AI Actions](https://www.contentful.com/products/ai-actions/)
- [Contentful and AI](https://www.contentful.com/products/ai/)
- [Model Context Protocol Introduction](https://www.contentful.com/blog/model-context-protocol-introduction/)

## Sitecore
- [Copilots and Agents Documentation](https://doc.sitecore.com/stream/en/users/sitecore-stream/copilots-and-agents.html)
- [AI in Experience Platform](https://doc.sitecore.com/xp/en/users/latest/sitecore-experience-platform/ai-in-experience-platform.html)
- [Sitecore Stream Gets Smarter](https://www.cmswire.com/digital-experience/sitecore-stream-gets-smarter-with-ai-copilots-and-agentic-workflows/)

## Storyblok
- [AI Features in Storyblok](https://www.storyblok.com/mp/ai-features)
- [Storyblok AI Updates 2025](https://sengo.com/resources/news/article/storybloks-next-chapter/)
- [Storyblok CMS Innovations](https://www.storyblok.com/mp/storyblok-unveils-cms-innovations)

## Strapi
- [Introducing Strapi AI](https://strapi.io/blog/introducing-strapi-ai)
- [StrapiConf 2025 Announcements](https://strapi.io/blog/strapi-conf-2025-announcements)
- [AI-Powered Automations](https://strapi.io/ai)

## WordPress
- [Jetpack AI Assistant](https://jetpack.com/ai/)
- [WP AI CoPilot](https://wordpress.org/plugins/ai-co-pilot-for-wp/)
- [AI Copilot Plugin](https://wordpress.org/plugins/ai-copilot/)

## Adobe Experience Manager
- [AI in AEM](https://experienceleague.adobe.com/en/docs/experience-manager-cloud-service/content/ai-in-aem/overview)
- [AI Assistant in AEM](https://experienceleague.adobe.com/en/docs/experience-manager-cloud-service/content/ai-in-aem/ai-assistant/ai-assistant-in-aem)
- [Generative AI for AEM Sites](https://experienceleague.adobe.com/en/docs/experience-manager-learn/cloud-service/expert-resources/cloud-5/season-3/cloud5-generative-ai-for-aem-sites)

## Optimizely
- [Optimizely Opal Benchmark Report](https://www.optimizely.com/insights/the-2025-optimizely-opal-ai-benchmark-report/)
- [2025 CMS Release Notes](https://support.optimizely.com/hc/en-us/articles/27677034133645-2025-CMS-SaaS-release-notes)
- [Optimizely 2025 Advancements](https://www.optimizely.com/company/press/optiwrapped/)

## Kentico
- [AIRA](https://www.kentico.com/platform/aira)
- [Kentico 2025 Milestones](https://cmscritic.com/kentico-achieves-big-milestones-in-2025-citing-ai-innovation-and-xperience-by-kentico-as-key-drivers)
- [Xperience February 2024 Refresh](https://community.kentico.com/blog/xperience-by-kentico-refresh-february-22,-2024)

## Kontent.ai
- [Agentic CMS](https://kontent.ai/blog/agentic-cms-redefining-content-management-for-the-future/)
- [From Assistive to Agentic](https://kontent.ai/blog/assistive-to-agentic-redefining-future-of-content/)
- [Native AI Capabilities](https://kontent.ai/features/introducing-native-ai-capabilities/)

## Webflow
- [Webflow AI](https://webflow.com/feature/ai)
- [AI Site Builder](https://webflow.com/ai-site-builder)
- [Webflow AI Overview](https://help.webflow.com/hc/en-us/articles/34297897805715-Webflow-AI-overview)

## Wix
- [Wix AI Features](https://www.wix.com/features/ai)
- [AI Website Builder](https://www.wix.com/ai-website-builder)
- [Adaptive Content Announcement](https://www.globenewswire.com/news-release/2025/04/23/3066430/0/en/Wix-Introduces-Adaptive-Content-Feature-with-AI-to-Personalize-Web-Experiences-for-Site-Visitors.html)

## Notion
- [Notion AI](https://www.notion.com/product/ai)
- [Notion AI Features & Capabilities](https://kipwise.com/blog/notion-ai-features-capabilities)
- [Notion 3.0 AI Agents](https://max-productive.ai/ai-tools/notion-ai/)

## Shopify
- [Shopify Magic - AI Product Descriptions](https://www.shopify.com/blog/ai-product-descriptions)
- [How to Use AI for Ecommerce](https://www.shopify.com/blog/how-to-use-ai)

## Emerging/Agentic
- [Acquia AI Agents Launch](https://www.cmswire.com/digital-experience/acquia-launches-ai-agents-in-saas-cms-for-content-automation/)
- [The Rise of Agentic CMS](https://www.boye-co.com/blog/2025/9/the-rise-of-agentic-cms)
- [Contentstack Agent OS](https://www.vktr.com/ai-news/contentstack-agent-os-ai-powered-cms-for-context-driven-digital-experiences/)
- [The Rise of AI Agents in Content Management](https://www.uxopian.com/blog/the-rise-of-ai-agents-a-peek-at-the-future-of-content-management)

---

*Document created: December 2025*
*Last updated: December 2025*
