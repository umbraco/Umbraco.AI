[Plan folder](https://github.com/umbraco/Umbraco.AI/tree/v18/feature/decision-capability/docs/plans/decision-capability-release) | v17 port: #428 | Stacked on this: #430 | Upstream: umbraco/Umbraco.Automate#343, umbraco/Umbraco.Automate#344 | M.E.AI direction: dotnet/extensions#7764, #7795

## Why the change

Developers can get cheap, typed yes/no, pick-one and score answers from decision models like
TypeSafe AI's Jev, one question or a batch at a time, in C#, TypeScript and Automate, instead of
prompting a chat model and parsing its text.

## Special things to note

- The API is shaped like Microsoft's proposed M.E.AI decision abstraction (dotnet/extensions#7764
  and #7795) where those two agree: shared `State`, id'd questions, answers keyed by id,
  `TrueProbability`, optional `Confidence`, score levels by position. Our provider contract
  (`IAIDecisionClient.GetResponseAsync`) mirrors their `IDecisionClient`, so when M.E.AI ships
  it the provider layer can wrap theirs. Option `Key`, `Choice` and text state stay ours, since
  their designs disagree there (DECISION-LOG 01-10 and 02-10-2026).
- Every question sent through `GetDecisionResponseAsync` needs a unique, non-blank `Id`, even a
  single one. `AskAsync` assigns one for you. A blank or duplicate id is an `ArgumentException`.
- Provider answers are checked inside tracking: each question answered once, probabilities in
  [0, 1] (NaN rejected), choice and score distributions complete and summing to 1 within
  rounding, the pick one of the offered keys. A failure is an `AIProviderException`, recorded as
  a failed call and returned as 400 by `decision/ask`. Real Jev returns dense, exact
  distributions (checked live at 255 options and 10 levels), so these never fire on it today.
- Copilot auto mode routes with a `DecisionAgentSelector` (selector id `decision`) plugged into
  Agent's new selector chain (#463), registered by default before `LLMAgentSelector`. Whenever
  Decision is on and a default Decision profile exists it answers, so a configured Classifier
  Chat Profile is then skipped; in any other case, or on any failure, it returns no opinion and
  the LLM selector runs as before. `StickyAgentSelector`'s guidance changes to
  `Insert<StickyAgentSelector>()` (first), since inserting it before the LLM selector would now
  put it after Decision.
- Everything under `Core/Decision/` is `[Experimental("UMBRACOAI_DECISION")]` and off by default
  (`Umbraco:AI:Experimental:Decision`). But `AICapability.Decision = 8`,
  `AISettings.DefaultDecisionProfileId`, `AIDecisionProfileSettings`, the Web models and the Deploy
  `DefaultDecisionProfileUdi` are permanent public API, following ImageGeneration.
  `AllProviderController` keeps its old constructor as `[Obsolete]` (removed in v20).
- `POST decision/ask` is open to any backoffice user and returns 400 for every provider failure
  (including a bad API key or Jev being busy), both as with Chat and ImageGeneration.
- Turning the flag off after publishing an automation with a Decision step makes Umbraco.Automate
  skip that step silently, so an If after it can take the wrong branch (Umbraco.Automate#343).
- Decision audit-log entries record token counts, like chat and embedding. Image generation has
  the same gap and is tracked separately in #473.
- #430 (Decision guardrail evaluator and test grader) is stacked on this branch and needs this
  rework merged in.
- Release prep must raise the `Umbraco.AI.Core` floor for Deploy, Automate and Agent, and
  Automate's `Umbraco.AI.Agent` floor. Otherwise a solo pack fails to compile against the old
  floor.
- Adds the test-only package `Microsoft.AspNetCore.TestHost` to the central
  `Directory.Packages.props`.

## Change outline

A request carries the content being judged and one or more typed questions. The response holds
one typed answer per question id, plus the model and usage for the whole call.

```csharp
// Umbraco.AI.Core/Decision — all [Experimental("UMBRACOAI_DECISION")]
sealed class AIDecisionRequest { string? State; IReadOnlyList<AIDecisionQuestion> Questions; }

abstract class AIDecisionQuestion { string? Id; string Instructions; }
abstract class AIDecisionQuestion<TAnswer> : AIDecisionQuestion where TAnswer : AIDecisionAnswer;

sealed class AIBinaryDecisionQuestion : AIDecisionQuestion<AIBinaryDecisionAnswer> { string? TrueCriteria, FalseCriteria; }
sealed class AIChoiceDecisionQuestion : AIDecisionQuestion<AIChoiceDecisionAnswer> { IReadOnlyList<AIDecisionOption> Options; }       // 2..255
sealed class AIScoreDecisionQuestion  : AIDecisionQuestion<AIScoreDecisionAnswer>  { IReadOnlyList<AIDecisionScoreLevel> Levels; }   // 2..10, position = level

class AIDecisionResponse { IReadOnlyDictionary<string, AIDecisionAnswer> Answers; ModelId; Usage; }
sealed class AIDecisionResponse<TAnswer> : AIDecisionResponse { TAnswer Answer; }

AIBinaryDecisionAnswer { TrueProbability; IsTrue(double threshold = 0.5) }
AIChoiceDecisionAnswer { Choice; Probabilities /* by key */;   double? Confidence }
AIScoreDecisionAnswer  { Score;  Probabilities /* by index */; double? Confidence }

interface IAIDecisionClient  { Task<AIDecisionResponse> GetResponseAsync(AIDecisionRequest, ...); }   // provider contract
interface IAIDecisionService {
    Task<AIDecisionResponse<TAnswer>> AskAsync<TAnswer>(AIDecisionQuestion<TAnswer> q, string? state = null, ...);
    Task<AIDecisionResponse>          GetDecisionResponseAsync(AIDecisionRequest request, ...);
    // each + (Guid profileId, ...), (string profileAlias, ...), (Action<AIDecisionBuilder>, ...)
}
```

A call goes through the same pipeline as the other capabilities. Caller input is checked
outermost, so it's never counted as a provider failure. Provider answers are checked inside
tracking, so a bad answer is recorded as a failure.

```diff
 IAIDecisionService.AskAsync / GetDecisionResponseAsync
   AIDecisionClientFactory
+    ValidatingDecisionClient            # ids, counts, bounds (DecisionQuestionValidator, shared with the API)
     ScopedProfileDecisionClient
     AITrackingDecisionMiddleware        # one usage + audit record per call, with token counts
     AIOpenTelemetryDecisionMiddleware   # gen_ai.decision.question_count, gen_ai.request.kind
+    AIErrorClassifyingDecisionClient    # DecisionAnswerChecker: complete, consistent answers
       TypeSafeDecisionClient            # one POST {Endpoint}/v1/systemone per request, questions keyed by id
```

What's new or changed, by product.

```diff
+Umbraco.AI.TypeSafe/                      # new provider package (18.0.0), tests in root slnx
+  TypeSafeProvider.cs                     # "typesafe"; cached key probe for Test connection
+  TypeSafeDecisionCapability.cs           # Decision only, model jev-latest
+  TypeSafeDecisionClient.cs               # batched noul/choice/score mapping, zero-fill, 429/529 retry (≤2, capped 30s)
 Umbraco.AI/src/
   Umbraco.AI.Core/
     Decision/                             # request/answer types, generic service, validator, answer checker
     Profiles/AIDecisionProfileSettings.cs # + serializer case
     Settings/AISettings.cs                # + DefaultDecisionProfileId
   Umbraco.AI.Web/Api/Management/
+    Decision/                             # POST decision/ask
+    Common/Filters/AICapabilityGate*.cs   # shared gate: Decision + ImageGeneration endpoints 404 when their flag is off
+    Capability/                           # GET capabilities/enabled
     Provider/AllProviderController.cs     # hides providers with no enabled capability
     Settings/, Profile/                   # defaultDecisionProfileId, "decision" settings $type
   Umbraco.AI.Web.StaticAssets/Client/src/
+    decision/                             # public UaiDecisionController
+    capability/                           # internal enabled-capabilities repository (cached)
+    property-editors/key-value-list/      # Uai.PropertyEditorUi.KeyValueList (pick-one options)
+    property-editors/decision-question-list/ # Uai.PropertyEditorUi.DecisionQuestionList + config modal (Ask questions)
     settings/, profile/                   # Decision picker, hidden pickers, Decision settings view
 Umbraco.AI.Deploy/                        # + DefaultDecisionProfileUdi export/import
 Umbraco.AI.Automate/Actions/              # + Ask yes/no (Threshold), Ask pick-one, Ask score, Ask questions (batch)
 Umbraco.AI.Agent/.../Selection/           # + DecisionAgentSelector, registered before LLMAgentSelector
-Umbraco.AI/tests/.../Decision/Spike/      # throwaway Jev spike provider
```

The endpoint asks one question per call, and returns 404 before model binding when the flag is
off and 400 before any provider call on bad input.

```json
POST /umbraco/ai/management/api/v1/decision/ask
{ "profileIdOrAlias": "spam-check",
  "state": "Buy cheap watches at …",
  "question": { "$type": "score", "instructions": "How spammy is this?",
                "levels": [{ "description": "not" }, { "description": "a bit" }, { "description": "very" }] } }

200 { "$type": "score", "score": 1.8, "confidence": 0.8,
      "probabilities": { "0": 0.05, "1": 0.15, "2": 0.8 }, "modelId": "jev-1.13.0",
      "usage": { "inputTokens": 330, "outputTokens": 41, "totalTokens": 371 } }
// binary → { "$type": "binary", "trueProbability": 0.97, ... }; confidence is left out when the provider gives none

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

Automate gets four actions. "Ask questions" asks a list of questions about one Context in a
single call; its outputs, and the output schema If/Switch steps bind to, are keyed by each
question's alias.

```diff
 Umbraco.AI.Automate/Actions/
+  AskYesNoDecisionAction    # Instructions, Context, criteria (bindable), Threshold → { answer, probability }
+  AskChoiceDecisionAction   # options via key/value editor → { choice, confidence }
+  AskScoreDecisionAction    # levels → { score, level, confidence }
+  AskDecisionsAction        # Questions via ref-node list → pick kind → config modal; Context bindable
+                            #   → { refund: { answer, probability }, category: { choice, confidence }, ... }

 // UmbracoAIAutomateComposer (+ run-time guard in each action → Validation)
+  if (!builder.Config.GetValue<bool>("Umbraco:AI:Experimental:Decision"))
+      builder.AutomateActions().Exclude<AskYesNoDecisionAction>().Exclude<AskChoiceDecisionAction>()
+                               .Exclude<AskScoreDecisionAction>().Exclude<AskDecisionsAction>();
```

Copilot auto mode routing goes through Agent's selector chain. The Decision selector runs
first and steps aside when it can't answer.

```diff
 IAIAgentSelectionService.SelectAgentAsync(input)
   candidates = active, scope-available agents in the surface
   if candidates.Count == 1: return it ("only-candidate")
   AIAgentSelectorCollection (default order):
+    DecisionAgentSelector     # 2..255 candidates, Decision on, default profile →
+                              #   AskAsync(Choice{ options = agents by id }, state: last user message)
+                              #   known agent id → result "decision"; otherwise null
     LLMAgentSelector          # classifier chat prompt → parse GUID → "llm" (unchanged)
   none answered → first candidate ("fallback")
```

Verified live against the real Jev API on the v18 and v17 demo sites: C# batches (one Jev call,
one usage and audit record), HTTP, the backoffice TypeScript client, the Automate designer
(including building and running an "Ask questions" step in a browser), Deploy connectors and
Copilot auto mode, each with the flag on and off. Details in `BUILD-LOG.md`.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

https://claude.ai/code/session_01Y1GDVK9CCVUwfdvEiFwNwN
