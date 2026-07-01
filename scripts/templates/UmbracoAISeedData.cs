// UmbracoAISeedData.cs
//
// Drop this file into an Umbraco site project that has Umbraco.AI packages installed.
// It seeds demo data (connection, profiles, context, prompts, agents, guardrails, tests) on first startup.
//
// Prerequisites:
// - Umbraco.AI, Umbraco.AI.OpenAI, Umbraco.AI.Prompt, Umbraco.AI.Agent packages installed
// - OpenAI API key configured in connection settings
//
// The seeder is idempotent — it checks for existing data by alias and skips if already seeded.

using System.Text.Json;
using Umbraco.AI.Core.Connections;
using Umbraco.AI.Core.Contexts;
using Umbraco.AI.Core.Guardrails;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Profiles;
using Umbraco.AI.Core.Settings;
using Umbraco.AI.Core.Tests;
using Umbraco.AI.Core.Tools.Scopes;
using Umbraco.AI.Agent.Core.Agents;
using Umbraco.AI.OpenAI;
using Umbraco.AI.Prompt.Core.Prompts;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Umbraco.AI.Demo;

public class UmbracoAISeedDataComposer : IComposer
{
    public void Compose(IUmbracoBuilder builder)
        => builder.AddNotificationAsyncHandler<UmbracoApplicationStartedNotification, UmbracoAISeedDataHandler>();
}

public class UmbracoAISeedDataHandler(
    IAIConnectionService connectionService,
    IAIProfileService profileService,
    IAIContextService contextService,
    IAIPromptService promptService,
    IAIAgentService agentService,
    IAISettingsService settingsService,
    IAIGuardrailService guardrailService,
    IAITestService testService,
    AIToolScopeCollection toolScopes,
    ILogger<UmbracoAISeedDataHandler> logger)
    : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly JsonSerializerOptions JsonOptions = AI.Core.Constants.DefaultJsonSerializerOptions;

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken ct)
    {
        // Skip if already seeded (check for our connection alias)
        if (await connectionService.GetConnectionByAliasAsync("openai-demo", ct) is not null)
        {
            logger.LogInformation("Demo data already seeded, skipping.");
            return;
        }

        logger.LogInformation("Seeding Umbraco.AI demo data...");

        // Resolve all available tool scope IDs for full agent permissions
        var allToolScopeIds = toolScopes.Select(s => s.Id).ToList();

        // 1. Context
        var context = await contextService.SaveContextAsync(new AIContext
        {
            Alias = "brand-voice",
            Name = "Brand Voice",
            Resources =
            [
                new AIContextResource
                {
                    ResourceTypeId = "brand-voice",
                    Name = "Brand Guidelines",
                    Description = "Core brand voice and tone guidelines",
                    SortOrder = 0,
                    Settings = new
                    {
                        ToneDescription = "We are friendly, professional, and approachable. Use clear, simple language. Avoid jargon. Speak directly to the reader using 'you'. Keep sentences short and paragraphs focused.",
                        TargetAudience = "Web developers and content editors using Umbraco CMS. Our audience ranges from technical developers building sites to non-technical content editors managing day-to-day content. Write so both groups can understand.",
                        StyleGuidelines = "Use active voice. Lead with the benefit or outcome, not the feature. Use sentence case for headings. Prefer short paragraphs (2-3 sentences). Use bullet points for lists of three or more items. Write at a secondary school reading level.",
                        AvoidPatterns = "Marketing buzzwords (leverage, synergy, cutting-edge). Exclamation marks. Overly casual language (gonna, wanna). Passive voice where active is possible. Filler phrases (in order to, it is important to note that). Starting sentences with 'So' or 'Basically'."
                    },
                    InjectionMode = AIContextResourceInjectionMode.Always
                }
            ]
        }, ct);

        // 2. Connection (API key resolved from IConfiguration via $-prefix)
        var connection = await connectionService.SaveConnectionAsync(new AIConnection
        {
            Alias = "openai-demo",
            Name = "OpenAI",
            ProviderId = "openai",
            Settings = new OpenAIProviderSettings { ApiKey = "$Umbraco:AI:Secrets:OpenAIApiKey" },
            IsActive = true
        }, ct);

        // 3. Guardrails
        var contentSafetyGuardrail = await guardrailService.SaveGuardrailAsync(new AIGuardrail
        {
            Alias = "content-safety",
            Name = "Content Safety Policy",
            Rules =
            [
                new AIGuardrailRule
                {
                    EvaluatorId = "contains",
                    Name = "Block competitor brand mentions",
                    Phase = AIGuardrailPhase.PostGenerate,
                    Action = AIGuardrailAction.Redact,
                    SortOrder = 0,
                    Config = ToJsonElement(new
                    {
                        searchPattern = "WordPress",
                        ignoreCase = true
                    })
                },
                new AIGuardrailRule
                {
                    EvaluatorId = "regex",
                    Name = "Block profanity",
                    Phase = AIGuardrailPhase.PreGenerate,
                    Action = AIGuardrailAction.Block,
                    SortOrder = 1,
                    Config = ToJsonElement(new
                    {
                        pattern = @"\b(damn|hell|crap)\b",
                        ignoreCase = true,
                        multiline = false
                    })
                }
            ]
        }, ct);

        var piiProtectionGuardrail = await guardrailService.SaveGuardrailAsync(new AIGuardrail
        {
            Alias = "pii-protection",
            Name = "PII Protection",
            Rules =
            [
                new AIGuardrailRule
                {
                    EvaluatorId = "regex",
                    Name = "Redact email addresses",
                    Phase = AIGuardrailPhase.PostGenerate,
                    Action = AIGuardrailAction.Redact,
                    SortOrder = 0,
                    Config = ToJsonElement(new
                    {
                        pattern = @"[a-zA-Z0-9._\%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
                        ignoreCase = true,
                        multiline = false
                    })
                },
                new AIGuardrailRule
                {
                    EvaluatorId = "regex",
                    Name = "Redact phone numbers",
                    Phase = AIGuardrailPhase.PostGenerate,
                    Action = AIGuardrailAction.Redact,
                    SortOrder = 1,
                    Config = ToJsonElement(new
                    {
                        pattern = @"(\+?\d{1,3}[-.\\s]?)?\(?\d{3}\)?[-.\\s]?\d{3}[-.\\s]?\d{4}",
                        ignoreCase = false,
                        multiline = false
                    })
                },
                new AIGuardrailRule
                {
                    EvaluatorId = "regex",
                    Name = "Block SSN patterns",
                    Phase = AIGuardrailPhase.PostGenerate,
                    Action = AIGuardrailAction.Block,
                    SortOrder = 2,
                    Config = ToJsonElement(new
                    {
                        pattern = @"\b\d{3}-\d{2}-\d{4}\b",
                        ignoreCase = false,
                        multiline = false
                    })
                }
            ]
        }, ct);

        // 4. Profiles
        var profile = await profileService.SaveProfileAsync(new AIProfile
        {
            Alias = "default-chat",
            Name = "Default Chat",
            Capability = AICapability.Chat,
            ConnectionId = connection.Id,
            Model = new AIModelRef("openai", "gpt-4o"),
            Settings = new AIChatProfileSettings
            {
                Temperature = 0.7f,
                ContextIds = [context.Id],
                GuardrailIds = [contentSafetyGuardrail.Id, piiProtectionGuardrail.Id]
            }
        }, ct);

        var embeddingProfile = await profileService.SaveProfileAsync(new AIProfile
        {
            Alias = "default-embedding",
            Name = "Default Embedding",
            Capability = AICapability.Embedding,
            ConnectionId = connection.Id,
            Model = new AIModelRef("openai", "text-embedding-3-small"),
            Settings = new AIEmbeddingProfileSettings()
        }, ct);

        var speechToTextProfile = await profileService.SaveProfileAsync(new AIProfile
        {
            Alias = "default-speech-to-text",
            Name = "Default Speech to Text",
            Capability = AICapability.SpeechToText,
            ConnectionId = connection.Id,
            Model = new AIModelRef("openai", "gpt-4o-transcribe"),
            Settings = new AISpeechToTextProfileSettings { Language = "en" }
        }, ct);

        // 5. Set profiles as defaults in settings
        var settings = await settingsService.GetSettingsAsync(ct);
        settings.DefaultChatProfileId = profile.Id;
        settings.DefaultEmbeddingProfileId = embeddingProfile.Id;
        settings.DefaultSpeechToTextProfileId = speechToTextProfile.Id;
        await settingsService.SaveSettingsAsync(settings, ct);

        // 6. Prompts
        var summarizePrompt = await promptService.SavePromptAsync(new AIPrompt
        {
            Alias = "summarize",
            Name = "Summarize",
            Description = "Summarize the current content into a concise paragraph",
            Instructions = "Summarize the following content in a single, concise paragraph that captures the key points:\n\n{{currentValue}}\n\nReturn just the result.",
            ProfileId = profile.Id,
            IsActive = true,
            IncludeEntityContext = false,
            OptionCount = 3,
            Scope = new AIPromptScope
            {
                AllowRules = [new AIPromptScopeRule { PropertyEditorUiAliases = ["Umb.PropertyEditorUi.TextArea", "Umb.PropertyEditorUi.TextBox"] }]
            }
        }, ct);

        var seoPrompt = await promptService.SavePromptAsync(new AIPrompt
        {
            Alias = "seo-description",
            Name = "SEO Description",
            Description = "Generate an SEO-friendly meta description",
            Instructions = "Write an SEO-optimized meta description (150-160 characters) for this content. Include relevant keywords naturally. Return just the result.",
            ProfileId = profile.Id,
            IsActive = true,
            IncludeEntityContext = true,
            OptionCount = 1,
            Scope = new AIPromptScope
            {
                AllowRules = [new AIPromptScopeRule { PropertyEditorUiAliases = ["Umb.PropertyEditorUi.TextArea", "Umb.PropertyEditorUi.TextBox"] }]
            }
        }, ct);

        // 7. Agents
        var contentAssistant = await agentService.SaveAgentAsync(new AIAgent
        {
            Alias = "content-assistant",
            Name = "Content Assistant",
            Description = "Helps create and edit content across the site",
            ProfileId = profile.Id,
            SurfaceIds = ["copilot"],
            Scope = new AIAgentScope
            {
                AllowRules = [new AIAgentScopeRule { Sections = ["content"] }]
            },
            Config = new AIStandardAgentConfig
            {
                ContextIds = [context.Id],
                Instructions = "You are a helpful content assistant for an Umbraco CMS website. Help users create, edit, and improve their content. Be concise and practical.",
                AllowedToolScopeIds = allToolScopeIds,
            },
            IsActive = true
        }, ct);

        await agentService.SaveAgentAsync(new AIAgent
        {
            Alias = "media-assistant",
            Name = "Media Assistant",
            Description = "Helps manage and describe media assets",
            ProfileId = profile.Id,
            SurfaceIds = ["copilot"],
            Scope = new AIAgentScope
            {
                AllowRules = [new AIAgentScopeRule { Sections = ["media"] }]
            },
            Config = new AIStandardAgentConfig
            {
                ContextIds = [context.Id],
                Instructions = "You are a media assistant for an Umbraco CMS website. Help users write alt text, captions, and descriptions for their media assets. Focus on accessibility and SEO.",
                AllowedToolScopeIds = allToolScopeIds,
            },
            IsActive = true
        }, ct);

        var legalSpecialist = await agentService.SaveAgentAsync(new AIAgent
        {
            Alias = "legal-specialist",
            Name = "Legal Specialist",
            Description = "Helps draft and review legal content like terms and conditions, privacy policies, and disclaimers",
            ProfileId = profile.Id,
            SurfaceIds = ["copilot"],
            Scope = new AIAgentScope
            {
                AllowRules = [new AIAgentScopeRule { Sections = ["content"] }]
            },
            Config = new AIStandardAgentConfig
            {
                ContextIds = [context.Id],
                Instructions = "You are a legal content specialist for an Umbraco CMS website. Help users draft and review legal content such as terms and conditions, privacy policies, cookie policies, and disclaimers. Write in clear, plain language that is accessible to non-lawyers while remaining legally sound. Always recommend professional legal review for final versions.",
                AllowedToolScopeIds = allToolScopeIds,
            },
            IsActive = true
        }, ct);

        // 8. Tests
        // Test: Summarize prompt produces a concise single-paragraph summary
        await testService.SaveTestAsync(new AITest
        {
            Alias = "test-summarize-quality",
            Name = "Summarize - Output Quality",
            Description = "Validates that the Summarize prompt produces a concise, single-paragraph summary capturing key points from the input content.",
            TestFeatureId = "prompt",
            TestTargetId = summarizePrompt.Id,
            ProfileId = profile.Id,
            ContextIds = [context.Id],
            TestFeatureConfig = ToJsonElement(new
            {
                propertyAlias = "bodyText",
                entityContext = new
                {
                    contentType = "article",
                    properties = new
                    {
                        bodyText = "Umbraco is an open-source content management system built on Microsoft .NET. It provides a flexible and extensible platform for building websites, intranets, and digital experiences. With a friendly editor interface, developers can create custom content types and templates while editors manage content without technical knowledge. The platform supports multi-language content, media management, and a rich ecosystem of packages and integrations. Umbraco has been trusted by thousands of organisations worldwide for over two decades."
                    }
                }
            }),
            Graders =
            [
                new AITestGraderConfig
                {
                    GraderTypeId = "llm-judge",
                    Name = "Summary quality check",
                    Description = "Evaluates whether the summary is concise, accurate, and captures the key points",
                    Severity = AITestGraderSeverity.Error,
                    Weight = 1.0,
                    Config = ToJsonElement(new
                    {
                        evaluationCriteria = "Evaluate whether this summary: (1) captures the key points about Umbraco being an open-source .NET CMS, (2) mentions the editor-friendly interface, (3) is a single concise paragraph under 100 words, (4) does not introduce information not in the original text.",
                        passThreshold = 0.7
                    })
                },
                new AITestGraderConfig
                {
                    GraderTypeId = "regex",
                    Name = "Single paragraph format",
                    Description = "Ensures the output is a single paragraph without line breaks",
                    Severity = AITestGraderSeverity.Error,
                    Negate = true,
                    Config = ToJsonElement(new
                    {
                        pattern = @"\n\s*\n",
                        ignoreCase = false,
                        multiline = true
                    })
                }
            ],
            RunCount = 3,
            Tags = ["prompt", "quality", "summarize"],
            IsActive = true
        }, ct);

        // Test: SEO description meets length and format requirements
        await testService.SaveTestAsync(new AITest
        {
            Alias = "test-seo-length",
            Name = "SEO Description - Length Validation",
            Description = "Validates that the SEO Description prompt generates meta descriptions within the optimal 120-170 character range without HTML tags.",
            TestFeatureId = "prompt",
            TestTargetId = seoPrompt.Id,
            ProfileId = profile.Id,
            ContextIds = [context.Id],
            TestFeatureConfig = ToJsonElement(new
            {
                propertyAlias = "metaDescription",
                entityContext = new
                {
                    contentType = "article",
                    properties = new
                    {
                        title = "Getting Started with Umbraco CMS",
                        bodyText = "Learn how to build modern websites with Umbraco, the open-source CMS built on .NET. This guide covers installation, content modelling, templating, and deployment."
                    }
                }
            }),
            Graders =
            [
                new AITestGraderConfig
                {
                    GraderTypeId = "regex",
                    Name = "Character length check",
                    Description = "Output should be between 120-170 characters for optimal SEO",
                    Severity = AITestGraderSeverity.Error,
                    Config = ToJsonElement(new
                    {
                        pattern = @"^.{120,170}$",
                        ignoreCase = false,
                        multiline = false
                    })
                },
                new AITestGraderConfig
                {
                    GraderTypeId = "regex",
                    Name = "No HTML tags",
                    Description = "Output must not contain HTML markup",
                    Severity = AITestGraderSeverity.Error,
                    Negate = true,
                    Config = ToJsonElement(new
                    {
                        pattern = @"<[^>]+>",
                        ignoreCase = true,
                        multiline = false
                    })
                },
                new AITestGraderConfig
                {
                    GraderTypeId = "contains",
                    Name = "Contains relevant keyword",
                    Description = "SEO description should mention Umbraco",
                    Severity = AITestGraderSeverity.Warning,
                    Config = ToJsonElement(new
                    {
                        searchPattern = "Umbraco",
                        ignoreCase = true
                    })
                }
            ],
            RunCount = 3,
            Tags = ["prompt", "seo", "validation"],
            IsActive = true
        }, ct);

        // Test: Content Assistant agent responds helpfully and uses tools
        await testService.SaveTestAsync(new AITest
        {
            Alias = "test-content-assistant",
            Name = "Content Assistant - Helpfulness",
            Description = "Validates that the Content Assistant agent provides helpful, on-topic responses when asked to help with content creation.",
            TestFeatureId = "agent",
            TestTargetId = contentAssistant.Id,
            ProfileId = profile.Id,
            ContextIds = [context.Id],
            TestFeatureConfig = ToJsonElement(new
            {
                message = "Help me write a short introduction paragraph for our About Us page. We're a digital agency specialising in Umbraco CMS solutions."
            }),
            Graders =
            [
                new AITestGraderConfig
                {
                    GraderTypeId = "llm-judge",
                    Name = "Response helpfulness",
                    Description = "Evaluates whether the agent provided a helpful, relevant introduction paragraph",
                    Severity = AITestGraderSeverity.Error,
                    Weight = 1.0,
                    Config = ToJsonElement(new
                    {
                        evaluationCriteria = "Evaluate whether the agent: (1) provided a draft introduction paragraph (not just advice), (2) the content mentions the agency and Umbraco CMS, (3) the tone is professional and approachable, (4) the response is actionable and directly usable.",
                        passThreshold = 0.7
                    })
                },
                new AITestGraderConfig
                {
                    GraderTypeId = "contains",
                    Name = "Mentions Umbraco",
                    Description = "The response should reference Umbraco as requested",
                    Severity = AITestGraderSeverity.Warning,
                    Config = ToJsonElement(new
                    {
                        searchPattern = "Umbraco",
                        ignoreCase = true
                    })
                }
            ],
            RunCount = 1,
            Tags = ["agent", "quality", "content"],
            IsActive = true
        }, ct);

        // Test: Legal Specialist agent includes appropriate disclaimers
        await testService.SaveTestAsync(new AITest
        {
            Alias = "test-legal-disclaimer",
            Name = "Legal Specialist - Disclaimer Compliance",
            Description = "Validates that the Legal Specialist agent always recommends professional legal review when drafting legal content.",
            TestFeatureId = "agent",
            TestTargetId = legalSpecialist.Id,
            ProfileId = profile.Id,
            ContextIds = [context.Id],
            TestFeatureConfig = ToJsonElement(new
            {
                message = "Draft a simple privacy policy for our website that collects user email addresses for a newsletter."
            }),
            Graders =
            [
                new AITestGraderConfig
                {
                    GraderTypeId = "llm-judge",
                    Name = "Legal content quality",
                    Description = "Evaluates the quality and completeness of the drafted privacy policy",
                    Severity = AITestGraderSeverity.Error,
                    Weight = 1.0,
                    Config = ToJsonElement(new
                    {
                        evaluationCriteria = "Evaluate whether the response: (1) includes a draft privacy policy (not just advice), (2) covers data collection, usage, and user rights, (3) mentions email/newsletter data specifically, (4) is written in plain, accessible language.",
                        passThreshold = 0.7
                    })
                },
                new AITestGraderConfig
                {
                    GraderTypeId = "regex",
                    Name = "Professional review disclaimer",
                    Description = "Must recommend professional legal review",
                    Severity = AITestGraderSeverity.Error,
                    Config = ToJsonElement(new
                    {
                        pattern = @"(legal\s+(review|counsel|advice|professional|advisor)|consult\s+(a|an|your)\s+(lawyer|attorney|solicitor|legal)|professional\s+review)",
                        ignoreCase = true,
                        multiline = true
                    })
                }
            ],
            RunCount = 3,
            Tags = ["agent", "legal", "compliance"],
            IsActive = true
        }, ct);

        // Test: Guardrail compliance - clean content should pass content safety
        await testService.SaveTestAsync(new AITest
        {
            Alias = "test-guardrail-clean-content",
            Name = "Content Safety - Clean Output Passes",
            Description = "Validates that the content safety guardrail does not flag clean, on-brand content generated by the Summarize prompt.",
            TestFeatureId = "prompt",
            TestTargetId = summarizePrompt.Id,
            ProfileId = profile.Id,
            ContextIds = [context.Id],
            TestFeatureConfig = ToJsonElement(new
            {
                propertyAlias = "bodyText",
                entityContext = new
                {
                    contentType = "article",
                    properties = new
                    {
                        bodyText = "Umbraco CMS empowers content editors with an intuitive interface for managing website content. The platform supports structured content, media management, and multi-language publishing out of the box."
                    }
                }
            }),
            Graders =
            [
                new AITestGraderConfig
                {
                    GraderTypeId = "guardrail",
                    Name = "No competitor mentions",
                    Description = "Clean output should not mention competitors",
                    Severity = AITestGraderSeverity.Error,
                    Negate = true,
                    Config = ToJsonElement(new
                    {
                        evaluatorId = "contains",
                        evaluatorConfig = new
                        {
                            searchPattern = "WordPress",
                            ignoreCase = true
                        }
                    })
                },
                new AITestGraderConfig
                {
                    GraderTypeId = "guardrail",
                    Name = "No PII in output",
                    Description = "Generated content should not contain email addresses",
                    Severity = AITestGraderSeverity.Error,
                    Negate = true,
                    Config = ToJsonElement(new
                    {
                        evaluatorId = "regex",
                        evaluatorConfig = new
                        {
                            pattern = @"[a-zA-Z0-9._\%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
                            ignoreCase = true
                        }
                    })
                }
            ],
            RunCount = 3,
            Tags = ["guardrail", "safety", "regression"],
            IsActive = true
        }, ct);

        logger.LogInformation("Umbraco.AI demo data seeded successfully.");
    }

    private static JsonElement ToJsonElement(object value)
        => JsonSerializer.SerializeToElement(value, JsonOptions);
}
