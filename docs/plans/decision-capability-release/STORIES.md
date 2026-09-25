# Stories

Derived from `SPEC.md`. Story ids `DR-n` ("decision release") are referenced by specs and
`PLAN.md`. Don't renumber.

> ASSUMPTION: Definition of Ready/Done below are proposed defaults, not yet confirmed.
>
> **Ready:** role, capability, value stated; Given/When/Then covers the happy path;
> out-of-scope explicit.
> **Done:** every AC passes as an executable spec (happy and sad path); product builds
> (`dotnet build <Product>.slnx`, `npm run build:<target>` where touched); any real entry
> point proven through a wire task; reviewer PASS; v17 port tracked (DR-12).

"Flag on/off" = `Umbraco:AI:Experimental:Decision`.

---

## DR-1 — Ask typed decisions from C#

As a **package or site developer**,
I want to ask a yes/no, pick-one, or score question through `IAIDecisionService` and get
back an answer of the matching type,
so that I can branch on a typed answer without prompting a chat model and parsing text.

**Happy path**

- AC1 — binary returns a binary answer
  Given a Decision profile and a provider that returns probability 0.97
  When I call `AskAsync(new AIBinaryDecisionQuestion { Instructions = "..." })`
  Then I get an `AIBinaryDecisionResponse` with `Answer = true`
- AC2 — binary confidence follows the answer
  Given a provider that returns probability 0.2
  When I ask a binary question
  Then `Answer` is false and `Confidence` is 0.8
- AC3 — choice returns the chosen key
  Given a provider that picks option `"b"` with confidence 0.9
  When I ask an `AIChoiceDecisionQuestion` with options `a`, `b`
  Then I get an `AIChoiceDecisionResponse` with `Choice = "b"` and `Confidence = 0.9`
- AC4 — score returns score and level
  Given levels `poor`, `ok`, `good` and a provider that returns score 1.8
  When I ask an `AIScoreDecisionQuestion`
  Then I get an `AIScoreDecisionResponse` with `Score = 1.8` and `Level = "good"`
- AC5 — profile by alias
  Given a Decision profile with alias `spam-check`
  When I call `AskAsync("spam-check", question)`
  Then the question runs against that profile
- AC6 — default profile
  Given `DefaultDecisionProfileId` is set
  When I call `AskAsync(question)` with no profile
  Then the question runs against the default Decision profile
- AC7 — default profile from config alias
  Given no stored default but `AIOptions.DefaultDecisionProfileAlias` names a Decision profile
  When I call `AskAsync(question)` with no profile
  Then the question runs against that profile

**Sad path**

- AC8 — blank instructions rejected
  Given a question with blank `Instructions`
  When I ask it
  Then `ArgumentException` is thrown and no provider is called
- AC9 — choice option count enforced
  Given a choice question with 1 option (or 256)
  When I ask it
  Then `ArgumentException` is thrown
- AC10 — duplicate choice keys rejected
  Given two options with key `"a"`
  When I ask it
  Then `ArgumentException` is thrown
- AC11 — score level count enforced
  Given a score question with 1 level (or 11)
  When I ask it
  Then `ArgumentException` is thrown
- AC12 — no default configured
  Given no default Decision profile (stored or config)
  When I ask without a profile
  Then `InvalidOperationException` says no default Decision profile is set
- AC13 — mismatched provider response
  Given a provider that returns a choice response to a binary question
  When I ask it
  Then `AIProviderException` is thrown
- AC14 — validation is not a provider failure
  Given an invalid question
  When I ask it through the real client factory
  Then the exception is `ArgumentException`, not `AIProviderException`, and no usage is
  recorded

---

## DR-2 — Connect to TypeSafe AI

As a **site developer**,
I want to install `Umbraco.AI.TypeSafe`, add my API key as a connection, and have Decision
questions answered by Jev,
so that I get real typed decisions without writing HTTP code.

**Happy path**

- AC1 — provider exposes Decision only
  Given the flag is on
  When I list providers
  Then TypeSafe is listed with capabilities `["Decision"]`
- AC2 — binary wire shape
  Given a binary question with no criteria
  When the client sends it
  Then the request is `POST {Endpoint}/v1/systemone` with bearer auth, `type: "noul"`, and
  no `criteria` property at all
- AC3 — binary criteria sent when set
  Given `TrueCriteria` or `FalseCriteria` is set
  When the client sends it
  Then `criteria` is `{ "true": ..., "false": ... }`
- AC4 — choice wire shape
  Given options `a` (description "A") and `b` (no description)
  When the client sends it
  Then `criteria` is `{ "a": "A", "b": "b" }`
- AC5 — score wire shape
  Given levels `poor`, `ok`, `good`
  When the client sends it
  Then `criteria` is `["poor","ok","good"]`
- AC6 — context sent as state
  Given `Context` is set
  When the client sends it
  Then `state` is the context and `instructions` is the instructions
- AC7 — instructions fall back to state
  Given `Context` is null
  When the client sends it
  Then `state` is the instructions
- AC8 — model id
  Given `options.ModelId = "jev-latest"`
  When the client sends it
  Then `model` is `"jev-latest"`
- AC9 — score probabilities keyed by label
  Given Jev returns probabilities `{ "0": 0.1, "2": 0.8 }`
  When the response is mapped
  Then `Probabilities` is `{ "poor": 0.1, "good": 0.8 }`
- AC10 — usage mapped
  Given Jev returns `usage { input_tokens: 42, output_tokens: 1 }`
  When the response is mapped
  Then `Usage.InputTokenCount = 42` and `OutputTokenCount = 1`
- AC11 — busy is retried
  Given Jev returns 429 then 200
  When the client sends a question
  Then it succeeds after one retry

**Sad path**

- AC12 — retries are bounded
  Given Jev returns 529 three times
  When the client sends a question
  Then it fails after exactly 3 attempts
- AC13 — auth not retried
  Given Jev returns 401
  When the client sends a question
  Then it fails after 1 attempt with an authentication error
- AC14 — validation not retried
  Given Jev returns 422
  When the client sends a question
  Then it fails after 1 attempt with a validation error
- AC15 — bad JSON
  Given Jev returns malformed JSON
  When the response is mapped
  Then an `AIProviderException`-classified failure is raised
- AC16 — flag off hides provider
  Given the flag is off
  When I list providers
  Then TypeSafe is not listed

**Wire**

- AC17 — live round trip (manual, real key)
  Given a real TypeSafe connection on the demo site with the flag on
  When each of the three kinds is asked through `IAIDecisionService`
  Then each returns a typed answer from Jev

---

## DR-3 — Set a default Decision profile

As a **backoffice admin setting up Umbraco AI**,
I want to pick a default Decision profile in the Settings section,
so that code calling Decision without a profile just works.

**Happy path**

- AC1 — settings round trip
  Given a Decision profile
  When I `PUT settings` with `defaultDecisionProfileId` = its id
  Then `GET settings` returns that id
- AC2 — picker shows when enabled
  Given the flag is on
  When I open the Settings editor
  Then a "Default Decision Profile" picker filtered to Decision profiles is shown
- AC3 — picker saves
  Given I pick a Decision profile in that picker
  When I save
  Then `defaultDecisionProfileId` is stored

---

## DR-4 — Ask decisions over the Management API

As a **developer calling Umbraco AI from backoffice code**,
I want a `POST decision/ask` endpoint,
so that Decision is reachable outside in-process C#.

**Happy path**

- AC1 — binary over HTTP
  Given the flag is on and a default Decision profile
  When I POST a `$type: "binary"` question
  Then I get 200 with `$type: "binary"`, `answer`, `probability`, `confidence`
- AC2 — choice over HTTP
  When I POST a `$type: "choice"` question
  Then I get 200 with `$type: "choice"`, `choice`, `confidence`, `probabilities`
- AC3 — score over HTTP
  When I POST a `$type: "score"` question
  Then I get 200 with `$type: "score"`, `score`, `level`, `confidence`, `probabilities`
- AC4 — profile by alias
  Given `profileIdOrAlias` = an existing Decision profile alias
  When I POST
  Then that profile is used
- AC5 — usage recorded
  When a POST succeeds
  Then one Decision usage record exists

**Sad path**

- AC6 — flag off is 404
  Given the flag is off
  When I POST anything, even an invalid body
  Then I get 404 with an empty body
- AC7 — invalid question is 400
  Given blank instructions, bad option/level counts, duplicate keys, or an unknown `$type`
  When I POST
  Then I get 400 ProblemDetails and no provider call is made
- AC8 — unknown profile is 404
  Given `profileIdOrAlias` that doesn't exist
  When I POST
  Then I get 404 ProblemDetails
- AC9 — non-Decision profile is 400
  Given a Chat profile's alias
  When I POST
  Then I get 400 ProblemDetails
- AC10 — no default is 400
  Given no default Decision profile and no `profileIdOrAlias`
  When I POST
  Then I get 400 ProblemDetails naming the missing default
- AC11 — provider validation is 400
  Given the provider rejects the question as invalid (Jev 422)
  When I POST
  Then I get 400 ProblemDetails

**Wire**

- AC12 — real request through the demo site
  Given the demo site with the flag on and a TypeSafe profile
  When I POST a real binary question with a backoffice token
  Then I get 200 with a real answer

---

## DR-5 — Ask decisions from TypeScript

As a **developer writing backoffice extensions**,
I want `UaiDecisionController.ask()` exported from `@umbraco-ai/core`,
so that I can ask decisions with typed results, like `UaiChatController`.

**Happy path**

- AC1 — binary result typed
  Given a `{ kind: "binary", ... }` question
  When I call `ask`
  Then `data` is a `UaiBinaryDecisionResult` with `answer`, `probability`, `confidence`
- AC2 — choice result typed
  Given a `{ kind: "choice", ... }` question
  When I call `ask`
  Then `data` is a `UaiChoiceDecisionResult`
- AC3 — score result typed
  Given a `{ kind: "score", ... }` question
  When I call `ask`
  Then `data` is a `UaiScoreDecisionResult`
- AC4 — kind maps to `$type`
  When I call `ask`
  Then the request body's `question.$type` equals the question's `kind`
- AC5 — profile option forwarded
  Given `options.profileIdOrAlias`
  When I call `ask`
  Then it's sent as `profileIdOrAlias`
- AC6 — public export
  When `npm run build:core` runs
  Then `UaiDecisionController` and the question/result types are in the public types rollup

**Sad path**

- AC7 — 404 returned, not thrown
  Given the server returns 404 (flag off)
  When I call `ask`
  Then the promise resolves with `error` set and `data` undefined

---

## DR-6 — Hide disabled experimental capabilities

As a **backoffice admin on a site with experimental features off**,
I want experimental capability options to disappear,
so that I don't see pickers and providers that can't do anything.

**Happy path**

- AC1 — enabled list, flags off
  Given both experimental flags off
  When I `GET capabilities/enabled`
  Then the list has `Chat`, `Embedding`, `SpeechToText` and not `ImageGeneration`,
  `Decision`, `Moderation`, `Media`
- AC2 — enabled list, Decision on
  Given the Decision flag on
  When I `GET capabilities/enabled`
  Then the list includes `Decision`
- AC3 — pickers hidden
  Given both flags off
  When I open the Settings editor
  Then the ImageGeneration and Decision pickers are not rendered, and the others are
- AC4 — hidden value kept
  Given a stored `defaultImageGenerationProfileId` and the flag off
  When I save Settings
  Then the stored value is unchanged
- AC5 — capability-less providers hidden
  Given a provider whose only capability is disabled
  When I `GET providers`
  Then it's not in the list
- AC6 — other providers untouched
  Given OpenAI (Chat + Embedding + ImageGeneration) and the ImageGeneration flag off
  When I `GET providers`
  Then OpenAI is listed with Chat and Embedding

---

## DR-7 — Manage Decision profiles in the backoffice

As a **backoffice admin**,
I want to create and open a Decision profile like any other,
so that I can point code and automations at it.

**Happy path**

- AC1 — capability label
  Given a TypeSafe connection and the flag on
  When I create a profile
  Then "Decision" appears as a capability option
- AC2 — workspace opens
  Given a Decision profile
  When I open it
  Then the Decision settings view renders its "no settings" message, not a blank area
- AC3 — settings serialize
  Given a Decision profile
  When its settings are serialized and deserialized via `AIProfileSettingsSerializer`
  Then the result is a non-null `AIDecisionProfileSettings`
- AC4 — API settings type
  When I `GET` a Decision profile
  Then `settings.$type` is `"decision"`

---

## DR-8 — Deploy Decision setup between environments

As a **developer promoting a site with Umbraco Deploy**,
I want the default Decision profile setting and Decision profiles to deploy,
so that a new environment doesn't silently lose them.

**Happy path**

- AC1 — default exported
  Given `DefaultDecisionProfileId` is set
  When the settings artifact is exported
  Then `DefaultDecisionProfileUdi` is that profile's UDI
- AC2 — default is a dependency
  When the settings artifact is exported
  Then the Decision profile is listed as a dependency
- AC3 — default imported
  Given an artifact with `DefaultDecisionProfileUdi`
  When it's imported
  Then `DefaultDecisionProfileId` is set
- AC4 — profile settings survive import
  Given a Decision profile artifact
  When it's imported
  Then its settings are a non-null `AIDecisionProfileSettings`

**Sad path**

- AC5 — no default, no field
  Given no default Decision profile
  When settings are exported
  Then `DefaultDecisionProfileUdi` is null and no dependency is added

---

## DR-9 — Branch automations on a decision

As an **automation builder in Umbraco Automate**,
I want "Ask yes/no", "Ask pick-one" and "Ask score" actions,
so that an automation can branch on a typed AI answer.

**Happy path**

- AC1 — yes/no output
  Given an "Ask yes/no" step and a provider answering probability 0.9
  When the step runs
  Then its output has `Answer = true`, `Probability = 0.9`, `Confidence = 0.9`
- AC2 — pick-one output
  Given an "Ask pick-one" step with options `a`, `b`
  When the provider picks `b`
  Then its output has `Choice = "b"`
- AC3 — score output
  Given an "Ask score" step with levels `low`, `high`
  When the provider returns 1.0
  Then its output has `Level = "high"`
- AC4 — empty profile uses default
  Given `ProfileId` empty and a default Decision profile
  When the step runs
  Then the default profile is used
- AC5 — bindings resolve
  Given `Instructions` bound to an earlier step's output
  When the step runs
  Then the bound value is sent as instructions

**Sad path**

- AC6 — hidden at startup when off
  Given the flag is off at startup
  When I open the action picker
  Then none of the three actions are listed
- AC7 — refused at run time when off
  Given the flag is turned off after startup
  When a step runs
  Then it fails with category `Validation` and no provider call
- AC8 — invalid settings
  Given an "Ask pick-one" step with 1 option
  When it runs
  Then it fails with category `Validation`
- AC9 — provider error
  Given the provider throws
  When the step runs
  Then it fails with category `Unknown` and the provider's message

**Wire**

- AC10 — real automation on the demo site
  Given an automation with "Ask yes/no" feeding an If step
  When it runs against a real TypeSafe profile
  Then the If step takes the branch matching the answer

---

## DR-10 — Copilot auto mode routes with Decision

As a **backoffice user in Copilot auto mode**,
I want agent routing to use Decision when it's set up,
so that routing is cheaper and doesn't depend on parsing a GUID out of chat text.

**Happy path**

- AC1 — Decision picks the agent
  Given 3 available agents, the flag on, a default Decision profile, and a provider picking
  agent 2's id
  When `SelectAgentForPromptAsync` runs
  Then agent 2 is returned and no chat call is made
- AC2 — options describe agents
  When the Decision question is built
  Then each option key is an agent id and its description contains the agent's name and
  description, and `Context` is the user's message
- AC3 — single agent unchanged
  Given 1 available agent
  When it runs
  Then that agent is returned with no Decision or chat call

**Sad path**

- AC4 — flag off falls back
  Given the flag off
  When it runs with 3 agents
  Then the chat classifier path runs and no Decision call is made
- AC5 — no default falls back
  Given the flag on but no default Decision profile
  When it runs
  Then the chat classifier path runs
- AC6 — provider error falls back
  Given Decision throws
  When it runs
  Then the chat classifier path runs and a warning is logged
- AC7 — unknown key falls back
  Given Decision returns a key that isn't an available agent
  When it runs
  Then the chat classifier path runs
- AC8 — too many agents falls back
  Given 256 available agents
  When it runs
  Then no Decision call is made and the chat classifier path runs

**Wire**

- AC9 — real Copilot session
  Given the demo site with 2+ agents, the flag on, and a real TypeSafe default profile
  When I send a message in Copilot auto mode
  Then an `agent_selected` event arrives, and usage shows a Decision call and no
  classifier chat call

---

## DR-11 — No spike code ships

As a **maintainer of Umbraco AI**,
I want every trace of the throwaway spike gone,
so that nothing temporary ships or confuses the next person.

- AC1 — spike folder gone
  When I look under `Umbraco.AI/tests/Umbraco.AI.Tests.Common/Decision/`
  Then there is no `Spike/` folder
- AC2 — no spike names
  When I search `src/` and `tests/` across the repo
  Then no file or type name contains "Spike" or "JevSpike"
- AC3 — gating proofs kept
  When Core's unit tests run
  Then the real-provider-inert tests (formerly T12) still pass using `FakeDecisionCapability`
- AC4 — spike plan archived
  When I look in `docs/archive/decision-capability/`
  Then the spike's plan files are there with a `> **Status:**` line

---

## DR-12 — Same feature on v17

As a **site developer on Umbraco 17 LTS**,
I want the same Decision feature,
so that I don't have to upgrade to CMS 18 to use it.

- AC1 — builds and tests pass
  Given a `v17/feature/decision-capability` branch with every DR-1..DR-11 change ported
  When each touched product's `.slnx` builds and tests run
  Then all pass
- AC2 — versions line up
  When I read `Umbraco.AI.TypeSafe/version.json` on v17
  Then it's `17.0.0`
- AC3 — wire on v17
  Given the v17 demo site with the flag on and a real TypeSafe key
  When a binary question is asked through `POST decision/ask`
  Then it returns a real answer
- AC4 — PR, not merge
  When the port is done
  Then it's an open PR into `v17/dev`, cross-linked with the v18 PR

---

## DR-13 — Public docs

As a **developer evaluating or adopting Decision**,
I want docs for the capability, provider, APIs and Automate actions,
so that I can use it without reading the source.

- AC1 — pages exist on both versions
  When I open `17/ai-in-umbraco/` and `18/ai-in-umbraco/` in Umbraco.Docs
  Then every "New" page listed in `SPEC.md` exists and is in `SUMMARY.md`
- AC2 — experimental marked
  When I open any Decision page
  Then it has the experimental warning hint with the flag JSON and the diagnostic id
- AC3 — edits made
  When I check every "Edited" page in `SPEC.md`
  Then each mentions Decision where relevant
- AC4 — held as draft
  When the docs are pushed
  Then they're a draft PR on branch `ai/decision-docs`, not merged
