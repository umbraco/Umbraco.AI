# Changelog - Umbraco.AI

All notable changes to Umbraco.AI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
