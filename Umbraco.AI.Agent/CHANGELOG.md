# Changelog - Umbraco.AI.Agent

All notable changes to Umbraco.AI.Agent will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent@1.1.0...Umbraco.AI.Agent@1.2.0) (2026-02-17)

### ⚠ Breaking change

* **core,agent:** from previous approach: Now distinguishes between context-bound
and cross-context tools based on ForEntityTypes declaration.

**Context-bound tools (filtered by entity type):**
- Backend tools with ForEntityTypes declared (e.g., content-read, media-write)
- Frontend tools with scopes that declare ForEntityTypes
- Only sent to LLM when editing matching entity types

**Cross-context tools (always available):**
- Backend tools without ForEntityTypes (e.g., search, navigation)
- Frontend tools without scopes or with scopes lacking ForEntityTypes
- Always sent to LLM regardless of current context

**Rationale:**
User could be editing a document but ask "show me media" - media tools should
be available. But frontend tools operating on current entity (e.g., set property)
should only appear when editing compatible entity types.

**LLM Context Awareness:**
Runtime context is already injected via AIRuntimeContextInjectingChatMiddleware,
providing entity type, section, user info. LLM uses this to make informed tool
choices without hard filtering cross-context tools.

Updated:
- AIToolContextFilter: Only filters by entity type (removed section filtering)
- AIAgentFactory: Added FilterFrontendToolsByContext for frontend tools
- Comments clarify context-bound vs cross-context distinction

### ⚠ BREAKING CHANGE

* **core,agent:** Backend tools no longer filtered by context

**Section Context:**
- SectionContextContributor now adds system message to inform LLM
- Format: "## Current Section\nThe user is currently in the 'content' section."

**Tool Metadata:**
- AIFunctionFactory enriches tool descriptions with ForEntityTypes metadata
- Example: "Retrieves content documents. [Suitable for entity types: document, documentType]"
- Helps LLM choose appropriate tools based on entity type

**Backend Tool Filtering Removed:**
- Backend tools are NO LONGER filtered by context (cross-context by design)
- LLM uses runtime context (section, entity) + tool metadata to make informed decisions
- Frontend tools STILL filtered (operate on currently open entity)

**How it works:**
1. Runtime context injects section + entity info via system messages
2. Tool descriptions include ForEntityTypes metadata
3. Backend tools always available (LLM decides based on context + metadata)
4. Frontend tools filtered to current entity type only

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
* **agent:** API endpoint changed from /agents/scopes to /agents/surfaces.
Database column ScopeIds renamed to SurfaceIds (migration required).

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>

### build

* **agent:** Regenerate API clients and build outputs ([7e1b29d](https://github.com/umbraco/Umbraco.AI/commit/7e1b29d4b3087ef493085d7b91e8400a9b2299fe))

### feat

* **agent,agent-ui:** Add agent attribution to chat messages ([6a55d65](https://github.com/umbraco/Umbraco.AI/commit/6a55d652dce5088f5b54f14ee188149ecea16d0a))
* **agent,copilot:** Add surface context contributor and frontend integration ([017aca1](https://github.com/umbraco/Umbraco.AI/commit/017aca1cb889a068035a771cd765360e03ed33e3))
* **agent,core:** Register surface context contributor at startup level ([ef7a576](https://github.com/umbraco/Umbraco.AI/commit/ef7a5769f465d479a0de241f29a431a14b8aefc4))
* **agent:** Add agent execution notifications ([edb6edb](https://github.com/umbraco/Umbraco.AI/commit/edb6edb2248018df86a505f5162e7603fd2d8467))
* **agent:** Add AIAgent lifecycle notifications and service integration ([c53327c](https://github.com/umbraco/Umbraco.AI/commit/c53327cc77062f891411a4d0e763e5fe67ff2e35))
* **agent:** Add auto mode with agent selection to RunAgentController ([3e02b0f](https://github.com/umbraco/Umbraco.AI/commit/3e02b0fada4a88e36941436c3594e9cd29381c9d))
* **agent:** Add context scope API models and migrations (Phase 2 - Part 2) ([b965af4](https://github.com/umbraco/Umbraco.AI/commit/b965af48cf2c0e79ac0f9876f46e883cd7d34258))
* **agent:** Add context scope domain models (Phase 2 - Part 1) ([cdeb5b0](https://github.com/umbraco/Umbraco.AI/commit/cdeb5b05cf1980b88bfee3ac186265554b301109))
* **agent:** Add context scope rule editor UI ([e058d85](https://github.com/umbraco/Umbraco.AI/commit/e058d859a54be9365e54e8a090108fc73d31987d))
* **agent:** Add context-aware agent filtering with scope validation ([9b8d0a9](https://github.com/umbraco/Umbraco.AI/commit/9b8d0a9124734ff85a1196cf8eaf7e426e1a93d0))
* **agent:** Add onCustomEvent callback to transport layer ([f76b586](https://github.com/umbraco/Umbraco.AI/commit/f76b586e699ceab5e9c01f8003904f4ee0292287))
* **agent:** Add scope-aware dimension filtering ([3149f7a](https://github.com/umbraco/Umbraco.AI/commit/3149f7af06434051c36dcfc2ca2816df220d2dfe))
* **agent:** Add section and entity type tag inputs for scope rules ([bdd0b34](https://github.com/umbraco/Umbraco.AI/commit/bdd0b3473067c32db560b241593d4180f94dc943))
* **agent:** Add SelectAgentForPromptAsync method ([e1ad397](https://github.com/umbraco/Umbraco.AI/commit/e1ad39710919139f0f2180ee7149bef87248b5ad))
* **agent:** Create dedicated Availability workspace view ([29ceb8e](https://github.com/umbraco/Umbraco.AI/commit/29ceb8e8206f573908cc35dadee2b1c867aecdf2))
* **agent:** Hide empty tool scopes in agent workspace and user group permissions ([f11c453](https://github.com/umbraco/Umbraco.AI/commit/f11c4538da5c16989db677cc0e3bd6c597eab76d))
* **core,agent:** Add runtime context-based tool filtering to agent factory ([8f8c5ef](https://github.com/umbraco/Umbraco.AI/commit/8f8c5ef0bb7cd925f4ae9202e890771323296138))
* **core,agent:** Add section context and tool metadata to LLM ([c56e727](https://github.com/umbraco/Umbraco.AI/commit/c56e727fa0113540ef32c9cdab5c591f548605b2))
* **core,prompt,agent:** Add persistent menu highlighting for entity containers ([dd9cf3a](https://github.com/umbraco/Umbraco.AI/commit/dd9cf3a6aac91278a8003befc5c7719e4c0e7fd9))

### fix

* **agent:** Add missing Scope property configuration to DbContext ([f550079](https://github.com/umbraco/Umbraco.AI/commit/f5500792e5c30a15e040b83ec46f4e0336cb995e))
* **agent:** Fix build errors in auto mode implementation ([432ef45](https://github.com/umbraco/Umbraco.AI/commit/432ef45ed7f67501e91a82b1762c8cdfb48e25df))
* **agent:** Generate CallId when provider returns empty string ([8edbf6f](https://github.com/umbraco/Umbraco.AI/commit/8edbf6f90e3ecb1a76ecc05115d82d0c0ad52956))
* **agent:** Regenerate migrations with proper Designer files ([0815e64](https://github.com/umbraco/Umbraco.AI/commit/0815e6400262a54a1ce0b9e87cc6435080690734))
* **agent:** Register AIAgentScopeValidator in DI container ([04aa65f](https://github.com/umbraco/Umbraco.AI/commit/04aa65f810514f3de19eba1929ef93b94187fc5c))
* **agent:** Update remaining Surface and Scope references ([06ee82d](https://github.com/umbraco/Umbraco.AI/commit/06ee82da46d2ceb3fe95bb9a507d52f0fd998aa1))
* **agent:** Update section tags input to use localization ([976cb7d](https://github.com/umbraco/Umbraco.AI/commit/976cb7d7b003ad33de05d027aaf3e06a87e6fa25))
* **core,agent,prompt,copilot:** Add client ready promises to prevent race conditions ([8b961db](https://github.com/umbraco/Umbraco.AI/commit/8b961dbf5c0c8e74198772c6ff44e570238e7c1d))
* **core,agent,prompt:** Migrate authorization to custom AI section ([c5b4503](https://github.com/umbraco/Umbraco.AI/commit/c5b4503031fa5612f636409cc876659e4632fe82))
* **core,agent:** Only filter context-bound tools (those with ForEntityTypes) ([8322ad0](https://github.com/umbraco/Umbraco.AI/commit/8322ad0f3e27a93b91ca295c3cc774f0694fc252))
* **core,prompt,agent:** Use IEventAggregator instead of INotificationPublisher for Umbraco v17 ([0385280](https://github.com/umbraco/Umbraco.AI/commit/03852809bb696a4eaeaf8150f3e30deb6f9377bb))

### perf

* **agent:** Use Stopwatch for accurate duration measurement in notifications ([0d93829](https://github.com/umbraco/Umbraco.AI/commit/0d938293ddf3d160cef46ef626f4520de894ec53))

### refactor

* **agent,copilot:** Rename context dimensions to scope dimensions ([a888be0](https://github.com/umbraco/Umbraco.AI/commit/a888be09565460ad1b41f3ace9fce3f696e9c948))
* **agent:** Extract surface from context instead of hardcoding ([f8bc70b](https://github.com/umbraco/Umbraco.AI/commit/f8bc70bc026640251fb21406d1afe4d16fdc2c67))
* **agent:** Refactor scope rule editor to use container pattern ([59bdb38](https://github.com/umbraco/Umbraco.AI/commit/59bdb386619585e2fb2e77d15ed41e7856c26a05))
* **agent:** Remove remaining "context" references from agent scope ([6943350](https://github.com/umbraco/Umbraco.AI/commit/6943350322fa256c1b200af8638f46f05d0a1aa1))
* **agent:** Remove WorkspaceAliases from agent scope rules ([4350f55](https://github.com/umbraco/Umbraco.AI/commit/4350f5515984beb85f650f043d1be8ddf0e660cc))
* **agent:** Rename agent availability scope (ContextScope → Scope) ([227893e](https://github.com/umbraco/Umbraco.AI/commit/227893ed52647e610ad860295910d293b61018d2))
* **agent:** Rename agent categorization Scope → Surface ([4709705](https://github.com/umbraco/Umbraco.AI/commit/470970592e3b2ac6c3a412a93451e492b210a812))
* **agent:** Rename EntityTypeAlias to EntityType in context ([ae094db](https://github.com/umbraco/Umbraco.AI/commit/ae094db8a27a7554f5af02ee082f8346863a3cea))
* **agent:** Rename EntityTypeAliases to EntityTypes ([6566ac7](https://github.com/umbraco/Umbraco.AI/commit/6566ac7c94843cf996f176aaacfe9e61d4b5f09f))
* **agent:** Rename scope rule files and folder for consistency ([d52e180](https://github.com/umbraco/Umbraco.AI/commit/d52e1805775ab73243adc40cdcb6b30844e9b1a7))
* **agent:** Simplify agent execution notification implementation ([bafb2ca](https://github.com/umbraco/Umbraco.AI/commit/bafb2ca68751076374f324462bb1c5652cb29665))
* **agent:** Update frontend for Surface terminology ([79dabb9](https://github.com/umbraco/Umbraco.AI/commit/79dabb93b439d53195f8496926718bd9c6d71cb7))
* **agent:** Use runtime context infrastructure for context extraction ([1474d1a](https://github.com/umbraco/Umbraco.AI/commit/1474d1a48c165bcd366e691b6f485e5481eca1e0))
* **core,agent,copilot:** Rename SectionAlias to Section ([353e167](https://github.com/umbraco/Umbraco.AI/commit/353e1670fdcdc09f88c9055d81d7b688b693bccc))
* **prompt,agent:** Rename scope rule components for consistency ([9115f0e](https://github.com/umbraco/Umbraco.AI/commit/9115f0eebc35cf06611d3792bad921d67038d6fb))

## [1.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent@1.0.0...Umbraco.AI.Agent@1.1.0) (2026-02-10)

### feat

* **agent:** Add database migrations for UserGroupPermissions column ([c1cf3d4](https://github.com/umbraco/Umbraco.AI/commit/c1cf3d4ad31863e889f9964e43c824bd794c0f09))
* **agent:** Add dedicated Permissions workspace view ([96f2481](https://github.com/umbraco/Umbraco.AI/commit/96f2481b16515979fa8a0111382f2d7fe9b84b28))
* **agent:** Add frontend tool repository pattern for cross-package communication ([8de3ab3](https://github.com/umbraco/Umbraco.AI/commit/8de3ab31ca63afa21f1b1cd5ef1c3f0d296a98eb))
* **agent:** Add tool permission columns to persistence layer ([cb7e4ac](https://github.com/umbraco/Umbraco.AI/commit/cb7e4ac1b1e6165d5b57906b44405ae1496836b3))
* **agent:** Add tool permission properties and service methods ([07a112d](https://github.com/umbraco/Umbraco.AI/commit/07a112d08c915781742d99c29f7f95dc4ef573ab))
* **agent:** Add tool permission properties to API models ([a825a03](https://github.com/umbraco/Umbraco.AI/commit/a825a032d689c9634c78bba8e32804857a4b048e))
* **agent:** Add tool permissions override modal and wrapper ([8c0317a](https://github.com/umbraco/Umbraco.AI/commit/8c0317a1ae8cd8b1a7f98f770d3ad9f3ca38f6e5))
* **agent:** Add tool scope picker and permissions UI ([c278119](https://github.com/umbraco/Umbraco.AI/commit/c278119a5c141a658570ab12f8184222667801c4))
* **agent:** Add user group permission overrides data model ([f98f799](https://github.com/umbraco/Umbraco.AI/commit/f98f799ff293561380a9a30dc166599492203b5b))
* **agent:** Add user group permissions to API layer ([bc2fca6](https://github.com/umbraco/Umbraco.AI/commit/bc2fca6519be58765517aabbd46479d63a2edc5d))
* **agent:** Add user group permissions to frontend types ([7fe7eb6](https://github.com/umbraco/Umbraco.AI/commit/7fe7eb6ea59c8a0211555c4aa5b97a81a48137bc))
* **agent:** Add validation to Agent workspace ([e8ca381](https://github.com/umbraco/Umbraco.AI/commit/e8ca3818ec7ea922a99e5d6b5bef1ac8641e605e))
* **agent:** Auto-cleanup orphaned user group overrides ([ed9eda5](https://github.com/umbraco/Umbraco.AI/commit/ed9eda5ec348f8c983276c3551ceaf5b5af194f4))
* **agent:** Implement tool picker for allowed tools ([ab7d043](https://github.com/umbraco/Umbraco.AI/commit/ab7d0435a829893c2044f1a1c665d3a2043742df))
* **agent:** Implement user group permission resolution ([41b891f](https://github.com/umbraco/Umbraco.AI/commit/41b891fa1a46e4f7a39bca47ddd7e83b99c92507))
* **agent:** Integrate tool permissions in agent factory and API ([58756f7](https://github.com/umbraco/Umbraco.AI/commit/58756f72498d6426139e6b2e7891e794cad77ac8))
* **agent:** Integrate user group tool permissions into permissions view ([0b89931](https://github.com/umbraco/Umbraco.AI/commit/0b899314546f424aac7ae3cf2d71831d743166e3))
* **agent:** Make UaiAgentRepository active with observable state ([13bc53f](https://github.com/umbraco/Umbraco.AI/commit/13bc53f89f5e09882a3d6fa4e9088b7b9b5a1a1a))
* **agent:** Refactor tool scope picker to toggle-based permissions UI ([e549242](https://github.com/umbraco/Umbraco.AI/commit/e54924225b32fd0e990350d616d0a05986ca0002))
* **core,agent,copilot:** Add tool metadata flow for scope and destructive permissions ([6e9efbc](https://github.com/umbraco/Umbraco.AI/commit/6e9efbc5cd01b4fb79ec3731a47e4ca64b05efb7))
* **core,agent,prompt:** Add alias existence API endpoints ([f48d7e9](https://github.com/umbraco/Umbraco.AI/commit/f48d7e9eb8218fee27adbb8bec00ed74c0c897ff))
* **core,agent,prompt:** Add visual error indicators for duplicate alias validation ([cdc56f0](https://github.com/umbraco/Umbraco.AI/commit/cdc56f0ea9f394a6acbbe7e8f43cd12bfbee4a65))
* **prompt,agent:** Add required validation to instructions fields ([c4716d2](https://github.com/umbraco/Umbraco.AI/commit/c4716d234900fdca6088447e7b9b2bc5ee16c6c6))

### fix

* **agent, prompt, openai:** fix validation states for required backoffice fields (#33) ([4c31e25](https://github.com/umbraco/Umbraco.AI/commit/4c31e255a68aa281787feb760d172586599cff3c)), closes [#33](https://github.com/umbraco/Umbraco.AI/issues/33)
* **agent:** Add missing AllowedToolIds and AllowedToolScopeIds to AgentItemResponseModel ([6114b07](https://github.com/umbraco/Umbraco.AI/commit/6114b070b93276ce061ea7e2e38d23a155198c3f))
* **agent:** Add missing migration designer files ([12dce0d](https://github.com/umbraco/Umbraco.AI/commit/12dce0de977bd46fcf8a7aaac11ae307a1e514db))
* **agent:** Add type assertions for userGroupPermissions ([ebbfeb8](https://github.com/umbraco/Umbraco.AI/commit/ebbfeb8d136bf23baed7b9d6ff7a28b7da77aac6))
* **agent:** Fix TypeScript compilation errors ([b508f63](https://github.com/umbraco/Umbraco.AI/commit/b508f6336cbb69a8bab3f8233a9df60447e71aa4))
* **agent:** Fix TypeScript compilation errors in repository ([5bd7c79](https://github.com/umbraco/Umbraco.AI/commit/5bd7c796fd53bb938e4384a35ed6a03c0636b2e3))
* **agent:** Fixed agent package being too liberal with the umbraco-marketplace tag ([4b3b3df](https://github.com/umbraco/Umbraco.AI/commit/4b3b3dfe29258b56dc5c19853e46290adc19b11c))
* **agent:** Fixed wrong localization string for agent bulk delete ([440fc28](https://github.com/umbraco/Umbraco.AI/commit/440fc2809923824ad495fff9e57d8e30e34a8c0d))
* **agent:** Remove translation scope from migration defaults ([ab05f8f](https://github.com/umbraco/Umbraco.AI/commit/ab05f8fabac4e42c62767e9991fcd0124291119f))
* **agent:** Resolve circular dependency in AIAgentFactory ([cf41854](https://github.com/umbraco/Umbraco.AI/commit/cf418540bfd361e95db5304ae8c657ae402c4f00))
* **agent:** Update tool-scope-picker to use AgentsService and correct types ([77e1bd4](https://github.com/umbraco/Umbraco.AI/commit/77e1bd4a18629d9c8b1fad8ab1ea74a80a3de4a7))
* **core,agent,prompt:** Add checkValidity() to trigger visual validation state ([b1dc012](https://github.com/umbraco/Umbraco.AI/commit/b1dc0129791129e28becc7b8ff9165d72e628000))
* **core,agent,prompt:** Escape hyphen in regex pattern ([49092af](https://github.com/umbraco/Umbraco.AI/commit/49092afef2ce95239bbc33cd748df9d5fdb37fbd))
* **core,agent,prompt:** Fix validation blocking by calling checkValidity() method ([1766901](https://github.com/umbraco/Umbraco.AI/commit/1766901e5e308235aed721bdbfc90d84e3c07fd5))
* **core,agent,prompt:** Fix validation issues from code review ([5984276](https://github.com/umbraco/Umbraco.AI/commit/5984276e463b7dccd625edf1181c4c950b6555a7))
* **core,agent,prompt:** Properly block save on duplicate alias via validation messages ([908d1c8](https://github.com/umbraco/Umbraco.AI/commit/908d1c8592755e08a88e92ef0e246b0783479da5))
* **core,agent,prompt:** Use setCustomValidity for visual error display ([346f0e8](https://github.com/umbraco/Umbraco.AI/commit/346f0e8ad04776c0b39c67d39695ce00962c7520))
* **core,agent:** Fix breaking unit tests ([14f9c47](https://github.com/umbraco/Umbraco.AI/commit/14f9c4705fa50065501df3c4b88357db185d2858))

### perf

* **agent:** Fetch single agent instead of all agents on update ([8fe5817](https://github.com/umbraco/Umbraco.AI/commit/8fe5817e86f48e00047eaa59e77dafed5e92fb14))

### refactor

* **agent,prompt:** Standardize alias pattern to lowercase only ([297513c](https://github.com/umbraco/Umbraco.AI/commit/297513c66d05cbf2516950086a281a8c7dac35cc))
* **agent:** Change tool scope picker to single column layout ([8f30edd](https://github.com/umbraco/Umbraco.AI/commit/8f30eddd91cbaaebcc5ad910b166868112c6c7b6))
* **agent:** Extract tool resolution logic to helper class ([61853c2](https://github.com/umbraco/Umbraco.AI/commit/61853c2be87e415d56d9c3e9aea782be43e21152))
* **agent:** Rename "Enabled" to "Allowed" for tool permissions ([3ee314c](https://github.com/umbraco/Umbraco.AI/commit/3ee314c03aef2a5bfe74bcdb4bcf7c59bc821bc2))
* **agent:** Rename scope-picker to agent-scope-picker ([a80bcf1](https://github.com/umbraco/Umbraco.AI/commit/a80bcf1a0d9bdadb250b02223681fcd0e3579e23))
* **agent:** Rename tool scope picker to permissions component ([a36a4a6](https://github.com/umbraco/Umbraco.AI/commit/a36a4a6155ebd58a2854f3804278a133831eb3f3))
* **agent:** Simplify tool permission metadata flow ([a0cf730](https://github.com/umbraco/Umbraco.AI/commit/a0cf730d79f702d75724b704d1a3517a4a259db2))
* **agent:** Use createExtensionApiByAlias for repository instantiation ([a895d84](https://github.com/umbraco/Umbraco.AI/commit/a895d84f115639068bace8ede099057f16b4b9f9))

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent@1.0.0...Umbraco.AI.Agent@1.0.1) (2026-02-04)

### Bug Fixes

* **agent:** Fixed agent package being too liberal with the umbraco-marketplace tag ([4b3b3df](https://github.com/umbraco/Umbraco.AI/commit/4b3b3dfe29258b56dc5c19853e46290adc19b11c))

## [1.0.0] - 2026-02-03

Initial release.

[1.0.0]: https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Agent@1.0.0
