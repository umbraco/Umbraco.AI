[Plan folder](https://github.com/umbraco/Umbraco.AI/tree/v18/feature/decision-capability/docs/plans/decision-capability-release) | v17 port: #428 | Upstream: umbraco/Umbraco.Automate#343, umbraco/Umbraco.Automate#344

## Why the change

Developers can get cheap, typed yes/no, pick-one and score answers from decision models like
TypeSafe AI's Jev, in C#, TypeScript and Automate, instead of prompting a chat model and parsing
its text.

## Special things to note

- Copilot auto mode routes with Decision whenever Decision is on and a default Decision profile
  exists, so a configured Classifier Chat Profile is then skipped (`AIAgentService.cs:288`). Any
  other case, or any failure, uses the unchanged chat path.
- Everything under `Core/Decision/` is `[Experimental("UMBRACOAI_DECISION")]` and off by default
  (`Umbraco:AI:Experimental:Decision`). But `AICapability.Decision = 8`,
  `AISettings.DefaultDecisionProfileId`, `AIDecisionProfileSettings`, the Web models and the Deploy
  `DefaultDecisionProfileUdi` are permanent public API, following ImageGeneration.
  `AllProviderController` keeps its old constructor as `[Obsolete]` (removed in v20).
- `POST decision/ask` is open to any backoffice user and returns 400 for every provider failure
  (including a bad API key or Jev being busy), both as with Chat and ImageGeneration.
- Turning the flag off after publishing an automation with a Decision step makes Umbraco.Automate
  skip that step silently, so an If after it can take the wrong branch (Umbraco.Automate#343).
- Release prep must raise the `Umbraco.AI.Core` floor for Deploy, Automate and Agent, and
  Automate's `Umbraco.AI.Agent` floor. Otherwise a solo pack fails to compile against the old
  floor.
- Adds the test-only package `Microsoft.AspNetCore.TestHost` to the central
  `Directory.Packages.props`.

## Change outline

Each kind of question has its own type, and the question's type decides the answer's type. This
mirrors the shape proposed for M.E.AI (dotnet/extensions#7764).

```csharp
// Umbraco.AI.Core/Decision — all [Experimental("UMBRACOAI_DECISION")]
abstract class AIDecisionQuestion { string Instructions; string? Context; }
abstract class AIDecisionQuestion<TResponse> : AIDecisionQuestion where TResponse : AIDecisionResponse;

sealed class AIBinaryDecisionQuestion : AIDecisionQuestion<AIBinaryDecisionResponse>  { string? TrueCriteria, FalseCriteria; }
sealed class AIChoiceDecisionQuestion : AIDecisionQuestion<AIChoiceDecisionResponse>  { IReadOnlyList<AIDecisionOption> Options; } // 2..255
sealed class AIScoreDecisionQuestion  : AIDecisionQuestion<AIScoreDecisionResponse>   { IReadOnlyList<string> Levels; }            // 2..10

AIBinaryDecisionResponse { Probability; Answer => Probability >= 0.5; Confidence }
AIChoiceDecisionResponse { Choice; Confidence; Probabilities }
AIScoreDecisionResponse  { Score; Level; Confidence; Probabilities }   // + ModelId, Usage on all

interface IAIDecisionService {
    Task<TResponse> AskAsync<TResponse>(AIDecisionQuestion<TResponse> q, ...);   // default profile
    // + (Guid profileId, q), (string profileAlias, q), (Action<AIDecisionBuilder>, q)
}
```

A call goes through the same pipeline as the other capabilities. Caller input is checked
outermost, so it's never counted as a provider failure. Provider output is checked inside
tracking, so a bad answer is recorded as a failure.

```diff
 IAIDecisionService.AskAsync<TResponse>
   AIDecisionClientFactory
+    ValidatingDecisionClient            # DecisionQuestionValidator: shared with the API controller
     ScopedProfileDecisionClient
     AITrackingDecisionMiddleware        # usage + audit
     AIOpenTelemetryDecisionMiddleware
+    AIErrorClassifyingDecisionClient    # wrong answer type → AIProviderException, recorded as failure
       TypeSafeDecisionClient            # POST {Endpoint}/v1/systemone
```

What's new or changed, by product.

```diff
+Umbraco.AI.TypeSafe/                      # new provider package (18.0.0), tests in root slnx
+  TypeSafeProvider.cs                     # "typesafe"; cached key probe for Test connection
+  TypeSafeDecisionCapability.cs           # Decision only, model jev-latest
+  TypeSafeDecisionClient.cs               # noul/choice/score mapping, 429/529 retry (≤2, capped 30s)
 Umbraco.AI/src/
   Umbraco.AI.Core/
     Decision/                             # per-kind types, generic service, shared validator
     Profiles/AIDecisionProfileSettings.cs # + serializer case
     Settings/AISettings.cs                # + DefaultDecisionProfileId
   Umbraco.AI.Web/Api/Management/
+    Decision/                             # POST decision/ask (+ resource filter: 404 when off)
+    Capability/                           # GET capabilities/enabled
     Provider/AllProviderController.cs     # hides providers with no enabled capability
     Settings/, Profile/                   # defaultDecisionProfileId, "decision" settings $type
   Umbraco.AI.Web.StaticAssets/Client/src/
+    decision/                             # public UaiDecisionController
+    capability/                           # internal enabled-capabilities repository (cached)
+    property-editors/key-value-list/      # Uai.PropertyEditorUi.KeyValueList (used by Ask pick-one options)
     settings/, profile/                   # Decision picker, hidden pickers, Decision settings view
 Umbraco.AI.Deploy/                        # + DefaultDecisionProfileUdi export/import
 Umbraco.AI.Automate/Actions/              # + Ask yes/no, Ask pick-one, Ask score (instructions, context, criteria bindable)
 Umbraco.AI.Agent/.../AIAgentService.cs    # auto mode tries Decision first
-Umbraco.AI/tests/.../Decision/Spike/      # throwaway Jev spike provider
```

The new endpoint, which returns 404 before model binding when the flag is off and 400 before
any provider call on bad input.

```json
POST /umbraco/ai/management/api/v1/decision/ask
{ "profileIdOrAlias": "spam-check",
  "question": { "$type": "choice", "instructions": "Which agent?", "context": "…",
                "options": [{ "key": "seo", "description": "SEO help" }, { "key": "translate" }] } }

200 { "$type": "choice", "choice": "seo", "confidence": 0.91,
      "probabilities": { "seo": 0.91, "translate": 0.09 }, "modelId": "jev-1.13.0",
      "usage": { "inputTokens": 330, "outputTokens": 41, "totalTokens": 371 } }

GET /umbraco/ai/management/api/v1/capabilities/enabled   →   ["Chat","Embedding","SpeechToText","Decision"]
```

The Settings screen shows experimental pickers only when their capability is turned on. Saves
always send every default id, so a hidden one isn't wiped.

```diff
 <uai-settings-editor>
   <uai-profile-picker capability="Chat">            # default chat, classifier chat
   <uai-profile-picker capability="Embedding">
   <uai-profile-picker capability="SpeechToText">
-  <uai-profile-picker capability="ImageGeneration">
+  ${enabled("ImageGeneration") ? <uai-profile-picker capability="ImageGeneration"> : nothing}
+  ${enabled("Decision")        ? <uai-profile-picker capability="Decision">        : nothing}
```

How the flag switches things off where there's no runtime hook.

```diff
 // Core: AIExperimentalFeatures.IsCapabilityEnabled
   AICapability.ImageGeneration => _options.CurrentValue.ImageGeneration,
+  AICapability.Decision        => _options.CurrentValue.Decision,

 // Umbraco.AI.Automate: UmbracoAIAutomateComposer (+ run-time guard in each action → Validation)
+  if (!builder.Config.GetValue<bool>("Umbraco:AI:Experimental:Decision"))
+      builder.AutomateActions().Exclude<AskYesNoDecisionAction>()
+                               .Exclude<AskChoiceDecisionAction>()
+                               .Exclude<AskScoreDecisionAction>();
```

Copilot auto mode routing. The existing chat path is unchanged and is still the fallback for
everything else.

```diff
 SelectAgentForPromptAsync(prompt, surface)
   agents = available agents in surface
   if agents.Count <= 1: return agents.FirstOrDefault()
+  if 2..255 agents and Decision enabled and HasDefaultProfileAsync(Decision):
+    try: choice = AskAsync(Choice{ options = agents by id, context = prompt })
+         if choice is a known agent id: return that agent
+    catch (not cancellation): log warning, fall through
   classifier chat prompt → parse GUID → agent (unchanged)
```

Verified live on the demo site against the real Jev API: C#, HTTP, the backoffice (browser),
Deploy connectors, an Automate If-branch run and Copilot auto mode, each with the flag on and off.
Details in `BUILD-LOG.md`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_01Y1GDVK9CCVUwfdvEiFwNwN
