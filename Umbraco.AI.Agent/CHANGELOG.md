# Changelog - Umbraco.AI.Agent

All notable changes to Umbraco.AI.Agent will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
* **core,agent,prompt:** Use setCustomValidity for visual error display ([346f0e8](https://github.com/umbraco/Umbraco.AI/commit/346f0e8ad04776c0b39c67d39695ce00962c7520)), closes [#checkAliasUniqueness](https://github.com/umbraco/Umbraco.AI/issues/checkAliasUniqueness)
* **core,agent:** Fix breaking unit tests ([14f9c47](https://github.com/umbraco/Umbraco.AI/commit/14f9c4705fa50065501df3c4b88357db185d2858))

### perf

* **agent:** Fetch single agent instead of all agents on update ([8fe5817](https://github.com/umbraco/Umbraco.AI/commit/8fe5817e86f48e00047eaa59e77dafed5e92fb14)), closes [#handleAgentUpdate](https://github.com/umbraco/Umbraco.AI/issues/handleAgentUpdate)

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
