# Changelog - Umbraco.AI

All notable changes to Umbraco.AI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.1.0...Umbraco.AI@1.2.0) (2026-02-17)

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

* **frontend:** UaiSerializedEntity structure changed for entity adapters

Changes:
- UaiSerializedEntity now has data field instead of contentType and properties
- Mark UaiSerializedProperty as deprecated (kept for reference)
- Update document adapter to nest structure inside data field:
  Before: { entityType, contentType, properties[] }
  After: { entityType, data: { contentType, properties[] } }

Allows third-party adapters to use domain-appropriate JSON structures
instead of being forced into rigid property arrays.
* **core:** AISerializedEntity.Properties replaced with Data field

Changes:
- AISerializedEntity now uses JsonElement Data instead of Properties collection
- Remove ContentType from top level (moved to data field)
- Mark AISerializedProperty as obsolete (kept for reference)
- Refactor AIEntityContextHelper to use formatter collection
- Update SerializedEntityContributor to check for data field
- BuildContextDictionary extracts data from JsonElement structure
- FormatForLlm delegates to entity-type-specific formatters

Adapters must now nest data inside the data field:
Before: { entityType, contentType, properties[] }
After: { entityType, data: { contentType, properties[] } }
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

* hide/show model property depending on loadingModels ([1c68091](https://github.com/umbraco/Umbraco.AI/commit/1c68091778d63a68888c667ec7c4b1ef6e2a232c))
* removing wrapper class from div ([1cf1ece](https://github.com/umbraco/Umbraco.AI/commit/1cf1ece58f3e3974a70f6cd75d5b836a70dfb06d))
* duplicated validation messages fix for model property ([ac0245e](https://github.com/umbraco/Umbraco.AI/commit/ac0245e9ffdf9ac8a340b441df399e5923f791fd))

### feat

* **agent,core:** Register surface context contributor at startup level ([ef7a576](https://github.com/umbraco/Umbraco.AI/commit/ef7a5769f465d479a0de241f29a431a14b8aefc4))
* **core,agent:** Add runtime context-based tool filtering to agent factory ([8f8c5ef](https://github.com/umbraco/Umbraco.AI/commit/8f8c5ef0bb7cd925f4ae9202e890771323296138))
* **core,agent:** Add section context and tool metadata to LLM ([c56e727](https://github.com/umbraco/Umbraco.AI/commit/c56e727fa0113540ef32c9cdab5c591f548605b2))
* **core,copilot:** Add manifest-driven request context contributor system ([ec3f985](https://github.com/umbraco/Umbraco.AI/commit/ec3f98503ed98b96e86f3a6c78f9320922eccb3b))
* **core,frontend:** Add Group property to AIField for visual field grouping ([16ec4bc](https://github.com/umbraco/Umbraco.AI/commit/16ec4bc503a2d5913eadb40b185ef4d597c11acb))
* **core,frontend:** Make model editor default group configurable ([ba16b58](https://github.com/umbraco/Umbraco.AI/commit/ba16b58170883e8e1848386e81cb9bd7e494f73d))
* **core,prompt,agent:** Add persistent menu highlighting for entity containers ([dd9cf3a](https://github.com/umbraco/Umbraco.AI/commit/dd9cf3a6aac91278a8003befc5c7719e4c0e7fd9))
* **core:** Add "Select All" checkbox to tool scope permissions ([0488e63](https://github.com/umbraco/Umbraco.AI/commit/0488e630c6192d0b31cb8adf8fa6db4744337f21))
* **core:** Add AIConnection and AIContext lifecycle notifications ([00176af](https://github.com/umbraco/Umbraco.AI/commit/00176afb445c1feba5b73fbf52f8fe9d6eda39ad))
* **core:** Add AIProfile lifecycle notifications ([7846fd6](https://github.com/umbraco/Umbraco.AI/commit/7846fd69f7253f73ccaf1ef6de1bfc721b5e8a10))
* **core:** Add base notification classes for entity lifecycle events ([e6fb38a](https://github.com/umbraco/Umbraco.AI/commit/e6fb38ad9465263f7c38d204d46de1fd0a80310a))
* **core:** Add entity formatter infrastructure ([3e3a09c](https://github.com/umbraco/Umbraco.AI/commit/3e3a09c5b60e276961c45843809e00a6df0236d5))
* **core:** Add ForEntityTypes to tool scopes for context filtering ([2cb9939](https://github.com/umbraco/Umbraco.AI/commit/2cb993961a97bf952532cabaa0afb77fad477a81))
* **core:** Add group labels for provider settings UI ([9809df5](https://github.com/umbraco/Umbraco.AI/commit/9809df51366c1bd642b6fb37c910306a8a0c4fbf))
* **core:** Add tool counts and empty scope filter to tool scope components ([222db17](https://github.com/umbraco/Umbraco.AI/commit/222db17a80e7af9bc343244a2fd527bf4c1063b8))
* **core:** Add welcome dashboard for AI section ([9a5318b](https://github.com/umbraco/Umbraco.AI/commit/9a5318b9ea3bf0960e86f6906823c97f3cca3bd6))
* **core:** Improve welcome dashboard styling and content ([92c91e2](https://github.com/umbraco/Umbraco.AI/commit/92c91e224fe8839e0ac4637b2e420ebce307fa65))
* **core:** Integrate notification publishing into AIConnection and AIContext services ([3202194](https://github.com/umbraco/Umbraco.AI/commit/3202194b5a5ca941b2d9a70ab98eeb97aea0a447))
* **core:** Integrate notification publishing into AIProfileService ([595deb8](https://github.com/umbraco/Umbraco.AI/commit/595deb81ab2fc891b4bd94a85d959f77d2a7fd4b))
* **core:** Integrate rollback notifications into Core services ([fd157e8](https://github.com/umbraco/Umbraco.AI/commit/fd157e8981c46e132046fcee2de204a82e3bb13e))
* **frontend:** Add tool count tags to scope picker modals ([c9680ef](https://github.com/umbraco/Umbraco.AI/commit/c9680ef5314667c824116e484c0fc98c9f2fef60))
* **frontend:** Localize tool count display using function-based localization ([f8fd5ae](https://github.com/umbraco/Umbraco.AI/commit/f8fd5aeb5621c42f86b746f7da625e70e13e3bda))

### fix

* **core,agent,prompt,copilot:** Add client ready promises to prevent race conditions ([8b961db](https://github.com/umbraco/Umbraco.AI/commit/8b961dbf5c0c8e74198772c6ff44e570238e7c1d))
* **core,agent,prompt:** Migrate authorization to custom AI section ([c5b4503](https://github.com/umbraco/Umbraco.AI/commit/c5b4503031fa5612f636409cc876659e4632fe82))
* **core,agent:** Only filter context-bound tools (those with ForEntityTypes) ([8322ad0](https://github.com/umbraco/Umbraco.AI/commit/8322ad0f3e27a93b91ca295c3cc774f0694fc252))
* **core,prompt,agent:** Use IEventAggregator instead of INotificationPublisher for Umbraco v17 ([0385280](https://github.com/umbraco/Umbraco.AI/commit/03852809bb696a4eaeaf8150f3e30deb6f9377bb))
* **core:** Add IEventAggregator mocks to service unit tests ([30544ec](https://github.com/umbraco/Umbraco.AI/commit/30544ec7f8a215ec51f62ba3a3fb17f01c3fdc16))
* **core:** Add missing IEventAggregator to integration test setup ([312f67f](https://github.com/umbraco/Umbraco.AI/commit/312f67f6b0c6f7ad81500b14928d6dad375ca5d0))
* **core:** Fix AIFunctionFactoryTests to work with sealed AIToolScopeCollection ([6a0aa50](https://github.com/umbraco/Umbraco.AI/commit/6a0aa50fc5d5e63f2fdb187af097c33b850ab08b))
* **core:** Fix compilation errors in entity adapter tests ([8386a24](https://github.com/umbraco/Umbraco.AI/commit/8386a245aced4d8dc6c8a0fb89eddc940e7d9741))
* **core:** Fix entity contributor race condition ([83a99a4](https://github.com/umbraco/Umbraco.AI/commit/83a99a407e79582b43f92a9160ce64432457e62b))
* **core:** Fix race condition causing empty tool scope list ([8585c72](https://github.com/umbraco/Umbraco.AI/commit/8585c72fc30451b21a238bb8ec822a117f1aa803))
* **core:** Fix StatefulNotification constructor and update tests with IEventAggregator ([97eeceb](https://github.com/umbraco/Umbraco.AI/commit/97eeceb1125b05b1fa6d64a631d136735d75121b))
* **core:** Fix timezone detection in timestamp formatter ([0734deb](https://github.com/umbraco/Umbraco.AI/commit/0734deb46b96b7f4778a287ba40f72655f355c96))
* **core:** Fix timezone handling in AI Logs timestamps ([e6bfc33](https://github.com/umbraco/Umbraco.AI/commit/e6bfc33e30cc8f58114a86722c21f846e165ff93)), closes [#49](https://github.com/umbraco/Umbraco.AI/issues/49)
* **core:** Handle JsonElement deserialization in typed tools ([9f2772b](https://github.com/umbraco/Umbraco.AI/commit/9f2772b2fb1fa4e46dd24c4d8bb4b056b7ec5977)), closes [#48](https://github.com/umbraco/Umbraco.AI/issues/48)
* **core:** Skip encryption for configuration references in sensitive fields ([b425567](https://github.com/umbraco/Umbraco.AI/commit/b425567a04146e54ff50c945aa514be6123d0f36))
* **core:** Use correct extension API for frontend tool repository lookup ([2903ac8](https://github.com/umbraco/Umbraco.AI/commit/2903ac852a9538f4317abc7b15e8a16a5b6a998d))
* **core:** Use relative import to avoid circular dependency in tool controller ([dfb03fd](https://github.com/umbraco/Umbraco.AI/commit/dfb03fd240570abf56a271941655759eed593784))

### refactor

* **core,agent-ui,copilot:** Extract surface contributor into reusable kind ([7e2100c](https://github.com/umbraco/Umbraco.AI/commit/7e2100c5c3a69f81b71988378fc6997f4beaa5c5))
* **core,agent-ui,copilot:** Rename surface kind to agentSurface ([c578ef3](https://github.com/umbraco/Umbraco.AI/commit/c578ef3aa9a88f925241b2ea811dc83563d19a5c))
* **core,agent,copilot:** Rename SectionAlias to Section ([353e167](https://github.com/umbraco/Umbraco.AI/commit/353e1670fdcdc09f88c9055d81d7b688b693bccc))
* **core,frontend:** Construct group localization keys by convention ([c1806d0](https://github.com/umbraco/Umbraco.AI/commit/c1806d0816e56903d1a9e754ef5921a5c62e9251))
* **core,frontend:** Move group localization key construction to frontend ([dc9afd5](https://github.com/umbraco/Umbraco.AI/commit/dc9afd57f8534c495776d42dff40b0ac5b74067d))
* **core,prompt:** Rename PropertyChange to ValueChange ([bcc58fd](https://github.com/umbraco/Umbraco.AI/commit/bcc58fdc44178839d78ec4daf7f1a52828827720))
* **core:** Improve version history pagination layout ([22b7272](https://github.com/umbraco/Umbraco.AI/commit/22b7272e3b19760f783f1b9638c9eb63c3e90a9a))
* **core:** Remove documentType and mediaType from built-in scopes ([f0c4f16](https://github.com/umbraco/Umbraco.AI/commit/f0c4f166662835a8186c66b4ede7f339b3d62240))
* **core:** Remove pagination wrapper div and styles ([2360b12](https://github.com/umbraco/Umbraco.AI/commit/2360b1214e2879bbff2b301970324284caf6a20c))
* **core:** Replace custom pagination with uui-pagination component ([814bdb9](https://github.com/umbraco/Umbraco.AI/commit/814bdb90579e4813af71e050a4d660d26be14812))
* **core:** Replace Properties with Data field in AISerializedEntity ([d3edbbb](https://github.com/umbraco/Umbraco.AI/commit/d3edbbbf5a501b491628afb06b7f709a8ff6eb0c))
* **core:** Simplify field group localization key generation ([4f75f27](https://github.com/umbraco/Umbraco.AI/commit/4f75f27eda7871d18720f058705b2d2dc818041c))
* **core:** Update version diff column header from Alias to Name ([2a0eee0](https://github.com/umbraco/Umbraco.AI/commit/2a0eee0853c70cb1791b3d7a77fa275f060f3ffb))
* **core:** Use C# property initializers for field default values ([1687d95](https://github.com/umbraco/Umbraco.AI/commit/1687d9566416ec377cf45af5c3bc6c068b531621))
* **frontend:** Simplify model editor to always group fields ([4ae056e](https://github.com/umbraco/Umbraco.AI/commit/4ae056e6daf5d2b6907458838c8f77418a31c591))
* **frontend:** Update entity adapter types to use data field ([ae1fbdb](https://github.com/umbraco/Umbraco.AI/commit/ae1fbdbd0666ec207a85b5fb90596c8018bb4809))

## [1.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.0.0...Umbraco.AI@1.1.0) (2026-02-10)

### ⚠ BREAKING CHANGE

* **core:** Removes the DetailLevel setting from audit logs and changes
default persistence behavior.

- Remove AIAuditLogDetailLevel enum (was never checked in code)
- Remove DetailLevel from AIAuditLog model and AIAuditLogEntity
- Remove DetailLevel from API response models
- Change PersistPrompts default from false to true
- Change PersistResponses default from false to true
- Keep PersistFailureDetails default as true
- Create database migrations to drop DetailLevel column (SQL Server & SQLite)
- Update docs to reference PersistPrompts/PersistResponses

The three boolean flags provide clearer, more flexible control than abstract
detail levels. Users can still disable any persistence option in
appsettings.json if needed for privacy.

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>

### feat

* add missing properties to uaiProfile ([701c738](https://github.com/umbraco/Umbraco.AI/commit/701c7383cc26e1fdc7c2d81c7bb17d0797435b54))
* **core,agent,copilot:** Add tool metadata flow for scope and destructive permissions ([6e9efbc](https://github.com/umbraco/Umbraco.AI/commit/6e9efbc5cd01b4fb79ec3731a47e4ca64b05efb7))
* **core,agent,prompt:** Add alias existence API endpoints ([f48d7e9](https://github.com/umbraco/Umbraco.AI/commit/f48d7e9eb8218fee27adbb8bec00ed74c0c897ff))
* **core,agent,prompt:** Add visual error indicators for duplicate alias validation ([cdc56f0](https://github.com/umbraco/Umbraco.AI/commit/cdc56f0ea9f394a6acbbe7e8f43cd12bfbee4a65))
* **core:** Add ConnectionAliasExistsAsync and ProfileAliasExistsAsync methods ([b1d1f95](https://github.com/umbraco/Umbraco.AI/commit/b1d1f95fc835ead5cb69d2f6181c143183eabf7e))
* **core:** Add context alias uniqueness validation backend ([76e85ff](https://github.com/umbraco/Umbraco.AI/commit/76e85ffe8bc9df86a37d5aac67f89955a396c2e3))
* **core:** Add generic user-group-settings-list component ([6978289](https://github.com/umbraco/Umbraco.AI/commit/697828987fbdcabdd355ab19f699a4293d905db1))
* **core:** Add localization entries for built-in tools ([9c69a2a](https://github.com/umbraco/Umbraco.AI/commit/9c69a2a1a73592048bcbcc9ea2fcb4ba7717d0fd))
* **core:** Add localization for get_umbraco_media tool ([41d9647](https://github.com/umbraco/Umbraco.AI/commit/41d96478c4b1f13e4f76c8fa394d95e8f085791e))
* **core:** Add localization support for tool names in picker ([bfb1390](https://github.com/umbraco/Umbraco.AI/commit/bfb1390f6ef03b2f32a6787f183c4d96d9d355d5))
* **core:** Add tool scope API endpoint ([2c2f913](https://github.com/umbraco/Umbraco.AI/commit/2c2f9135a710c0f2ca1c0e6a7affe5c280f4da17))
* **core:** Add validation to Profile workspace ([3f9f3f4](https://github.com/umbraco/Umbraco.AI/commit/3f9f3f4719ddc9604d40da04ac9764c94d86a5fd))
* **core:** Display full scope/tool info in permission override components ([e014db6](https://github.com/umbraco/Umbraco.AI/commit/e014db66b9ae199c8e7a54f5a6f0661fb75436de))
* **core:** Make user group list items clickable to open editor ([0b06162](https://github.com/umbraco/Umbraco.AI/commit/0b06162cdb5a0a1d5f7491e8b03d00e28baa7ee6))
* **core:** Replace text input with tool picker modal in tool permissions override ([02d50ed](https://github.com/umbraco/Umbraco.AI/commit/02d50eda4812d46f94fef11a4b59e04bedb2090a))
* **core:** Update built-in tool scope icons ([8639ba7](https://github.com/umbraco/Umbraco.AI/commit/8639ba72850d7724bb5ebbc323ef9f8f10b8e89a))
* **frontend:** Add validation localization strings ([a5094f9](https://github.com/umbraco/Umbraco.AI/commit/a5094f9bf70064025f6967bf3d2592f74a229925))
* **frontend:** Implement Connection workspace validation ([a4c655d](https://github.com/umbraco/Umbraco.AI/commit/a4c655d3a3e3c3ecb54183b63ba0733fe96950aa))

### fix

* **core,agent,prompt:** Add checkValidity() to trigger visual validation state ([b1dc012](https://github.com/umbraco/Umbraco.AI/commit/b1dc0129791129e28becc7b8ff9165d72e628000))
* **core,agent,prompt:** Escape hyphen in regex pattern ([49092af](https://github.com/umbraco/Umbraco.AI/commit/49092afef2ce95239bbc33cd748df9d5fdb37fbd))
* **core,agent,prompt:** Fix validation blocking by calling checkValidity() method ([1766901](https://github.com/umbraco/Umbraco.AI/commit/1766901e5e308235aed721bdbfc90d84e3c07fd5))
* **core,agent,prompt:** Fix validation issues from code review ([5984276](https://github.com/umbraco/Umbraco.AI/commit/5984276e463b7dccd625edf1181c4c950b6555a7))
* **core,agent,prompt:** Properly block save on duplicate alias via validation messages ([908d1c8](https://github.com/umbraco/Umbraco.AI/commit/908d1c8592755e08a88e92ef0e246b0783479da5))
* **core,agent,prompt:** Use setCustomValidity for visual error display ([346f0e8](https://github.com/umbraco/Umbraco.AI/commit/346f0e8ad04776c0b39c67d39695ce00962c7520))
* **core,agent:** Fix breaking unit tests ([14f9c47](https://github.com/umbraco/Umbraco.AI/commit/14f9c4705fa50065501df3c4b88357db185d2858))
* **core,tools:** Fix TypeScript compilation errors ([feae61a](https://github.com/umbraco/Umbraco.AI/commit/feae61a279c8181e189c4206a29375309078cb7c))
* **core:** Actually write the entity key to the system prompt as context ([fb5cefd](https://github.com/umbraco/Umbraco.AI/commit/fb5cefd48c38ef9c0310d3cd62aaf0b7ac10d721))
* **core:** Add AISettingsEntity to migration designer files ([bc5cd43](https://github.com/umbraco/Umbraco.AI/commit/bc5cd4364ec701739e2266b5f521f75ef77178b0))
* **core:** Add missing localization keys for workspace labels ([77c3722](https://github.com/umbraco/Umbraco.AI/commit/77c37228fc36a87fa364b60b17e8cd9e32773337)), closes [#2](https://github.com/umbraco/Umbraco.AI/issues/2)
* **core:** Add null check for userGroupId from picker ([138ac4e](https://github.com/umbraco/Umbraco.AI/commit/138ac4ee0445943c3a720b0cda2dfe1bd89d4550))
* **core:** Fixed core package being too liberal with the umbraco-marketplace tag ([5c660a0](https://github.com/umbraco/Umbraco.AI/commit/5c660a07e6352df18265eac43f52bf47ea660cb9))
* **core:** Include AISettings table in GUID user ID migration ([96c76fa](https://github.com/umbraco/Umbraco.AI/commit/96c76fa2e867ca4f7b519f50afaba06daabe7942)), closes [#44](https://github.com/umbraco/Umbraco.AI/issues/44)
* **core:** Instantiate UmbUserGroupItemRepository directly ([90486e5](https://github.com/umbraco/Umbraco.AI/commit/90486e511dfd66c81d001382a884b6d113af36c7))
* **core:** Make ToolScopeMapDefinition public for auto-discovery ([a14e2ce](https://github.com/umbraco/Umbraco.AI/commit/a14e2cefbeed23bab1e9ad76334f30c3a51e0314))
* **core:** Register ToolScopeMapDefinition with DI container ([7d4b114](https://github.com/umbraco/Umbraco.AI/commit/7d4b11405c0088599b7e79a00092aadd4d3f6d85))
* **core:** Reload tool picker items when frontendTools property changes ([7babd11](https://github.com/umbraco/Umbraco.AI/commit/7babd114e5790991599278f7e83f1e543854fffc))
* **core:** Use relative imports in tool-scope-permissions component ([47bf187](https://github.com/umbraco/Umbraco.AI/commit/47bf187618469296ba79a2bffc8be5cd37f9ba65))
* **core:** Use user group repository instead of direct API access ([e8a2998](https://github.com/umbraco/Umbraco.AI/commit/e8a2998080e39fff85c8ff877800a8f693d4a95c))
* **frontend:** Fix TypeScript compilation errors for workspace validation ([277abc6](https://github.com/umbraco/Umbraco.AI/commit/277abc61b519655bd3c989d35e5850011d5e265f))
* **frontend:** Validate auto-generated aliases on name change ([35dbe84](https://github.com/umbraco/Umbraco.AI/commit/35dbe848759643e2176e241ff6bf05e8455d8573))

### refactor

* **core:** Extract toCamelCase to shared utility ([4ebdc74](https://github.com/umbraco/Umbraco.AI/commit/4ebdc740bf831ff8031db0b7d81c9c834e786836))
* **core:** Move tool scope permissions component to Core ([e084fab](https://github.com/umbraco/Umbraco.AI/commit/e084fab20d1d64a39fab1eb9041cff7f8d093bd6))
* **core:** Remove unused DetailLevel setting and set PersistX defaults to true ([d9c8a75](https://github.com/umbraco/Umbraco.AI/commit/d9c8a757756a52d4e07a330ef143af239999e74a))
* **core:** Update tool scope permissions export path ([644967f](https://github.com/umbraco/Umbraco.AI/commit/644967f8ed88b6d5b19748bfddba0b66bd1947bc))

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.0.0...Umbraco.AI@1.0.1) (2026-02-04)

### ⚠ BREAKING CHANGE

* **core:** Removes the DetailLevel setting from audit logs and changes
default persistence behavior.

- Remove AIAuditLogDetailLevel enum (was never checked in code)
- Remove DetailLevel from AIAuditLog model and AIAuditLogEntity
- Remove DetailLevel from API response models
- Change PersistPrompts default from false to true
- Change PersistResponses default from false to true
- Keep PersistFailureDetails default as true
- Create database migrations to drop DetailLevel column (SQL Server & SQLite)
- Update docs to reference PersistPrompts/PersistResponses

The three boolean flags provide clearer, more flexible control than abstract
detail levels. Users can still disable any persistence option in
appsettings.json if needed for privacy.

### feat

* add missing properties to uaiProfile ([701c738](https://github.com/umbraco/Umbraco.AI/commit/701c7383cc26e1fdc7c2d81c7bb17d0797435b54))

### fix

* **core:** Fixed core package being too liberal with the umbraco-marketplace tag ([5c660a0](https://github.com/umbraco/Umbraco.AI/commit/5c660a07e6352df18265eac43f52bf47ea660cb9))

### refactor

* **core:** Remove unused DetailLevel setting and set PersistX defaults to true ([d9c8a75](https://github.com/umbraco/Umbraco.AI/commit/d9c8a757756a52d4e07a330ef143af239999e74a))

## [1.0.0] - 2026-02-03

Initial release.

[1.0.0]: https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI@1.0.0
