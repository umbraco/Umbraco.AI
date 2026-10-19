# Changelog - Umbraco.AI.Agent.UI

All notable changes to Umbraco.AI.Agent.UI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [18.3.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.2.0...Umbraco.AI.Agent.UI@18.3.0) (2026-10-19)

### feat

* **agent-ui,copilot,copilot-workspace:** Aggregate starter prompts across agents in Auto mode ([1ea742f](https://github.com/umbraco/Umbraco.AI/commit/1ea742f14139c7952c8dba69fdfb46a7437807d4))
* **agent-ui,copilot,copilot-workspace:** Render starter prompts in the empty chat state ([fa26b12](https://github.com/umbraco/Umbraco.AI/commit/fa26b12e23e576fb41111543a117535bba7419d4))

### fix

* **agent-ui:** Make starter prompt chips readable on narrow chat surfaces ([dfb4304](https://github.com/umbraco/Umbraco.AI/commit/dfb43048919acd9d85ac650bc4e916a9b9a4ecdb))

## [18.2.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.1.0...Umbraco.AI.Agent.UI@18.2.0) (2026-10-05)

### feat

* **agent-ui,agent:** Send the previous auto pick with each run ([aa41c24](https://github.com/umbraco/Umbraco.AI/commit/aa41c24b5d921e49eff22bd035fec5ae407385ae))

## [18.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.1.0-rc.4...Umbraco.AI.Agent.UI@18.1.0) (2026-10-01)

### feat

* **agent-ui,prompt:** Show an AI-generated notice on chat and prompt responses ([ad5bab8](https://github.com/umbraco/Umbraco.AI/commit/ad5bab80cbcebd50978c6da4184076a12025e365))
* **settings,agent-ui,prompt,deploy:** Add a setting to control the AI disclosure notice ([c292e59](https://github.com/umbraco/Umbraco.AI/commit/c292e59f84a17a88d5f2b6caf4c12b1e4e1e3817))
* **settings,agent-ui,prompt:** Make the AI disclosure notice dismissible per location ([6976bac](https://github.com/umbraco/Umbraco.AI/commit/6976bac026e9268fb0e715e6b1ea5cc0604c3c58))

### fix

* **agent-ui,copilot:** Stop the sidebar Copilot recursing when it first binds to an item ([56d58e6](https://github.com/umbraco/Umbraco.AI/commit/56d58e6a4b860552ef4d844df7a9f1044ca0ee91)), closes [#383](https://github.com/umbraco/Umbraco.AI/issues/383)
* **agent-ui:** Re-arm auto-follow when a consumer swaps the message source ([0adb51a](https://github.com/umbraco/Umbraco.AI/commit/0adb51a96200b95fb77f28e39d79c79a2096dabd))
* **agent-ui:** Stop chat auto-scroll from overriding a manual scroll during streaming ([87e06b1](https://github.com/umbraco/Umbraco.AI/commit/87e06b1a0bf07de8af91f40d50b06761277473c3))
* **frontend,agent,agent-ui:** Stop custom elements double-registering in the backoffice ([edadf41](https://github.com/umbraco/Umbraco.AI/commit/edadf41d9a20c38d16cbda44336a57d99edc0d4c))

### refactor

* **frontend,agent-ui,prompt:** Share one disclosure notice component ([ea2d0ba](https://github.com/umbraco/Umbraco.AI/commit/ea2d0babcd96f49c4e589bed282b240315cfbe67))

## [18.1.0-rc.4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.1.0-rc.3...Umbraco.AI.Agent.UI@18.1.0-rc.4) (2026-09-16)

## [18.1.0-rc.3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.1.0-rc.2...Umbraco.AI.Agent.UI@18.1.0-rc.3) (2026-09-10)

### fix

* **agent-ui:** Re-arm auto-follow when a consumer swaps the message source ([c819ed0](https://github.com/umbraco/Umbraco.AI/commit/c819ed0dc4be7a68425000732ae98656058dd2ef))
* **agent-ui:** Stop chat auto-scroll from overriding a manual scroll during streaming ([9434226](https://github.com/umbraco/Umbraco.AI/commit/9434226051d21c3c90b2a0f111a1128a7abb82ed))
* **conversations,agent-ui:** Stop returning to a conversation duplicating user messages ([f283a16](https://github.com/umbraco/Umbraco.AI/commit/f283a163787e540a816c2a8731eb1fd7a4118fa9))
* **frontend,agent,agent-ui:** Stop custom elements double-registering in the backoffice ([5e43987](https://github.com/umbraco/Umbraco.AI/commit/5e43987702f7e4f0cc3c3518b3be9cb7bfa1518e))

## [18.1.0-rc.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.1.0-rc.1...Umbraco.AI.Agent.UI@18.1.0-rc.2) (2026-08-21)

## [18.1.0-rc.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.0.1...Umbraco.AI.Agent.UI@18.1.0-rc.1) (2026-08-06)

### feat

* **agent-ui,copilot-workspace:** Add an archived-conversations recycle bin with read-only viewing ([af0e365](https://github.com/umbraco/Umbraco.AI/commit/af0e3659c67eda29f997049008c708a0fb4bd99c))
* **agent-ui,copilot-workspace:** Move chat scrollbar to the edge, keep centered content ([cd49784](https://github.com/umbraco/Umbraco.AI/commit/cd4978406da4b070ea02586b4a519b025d71b7c4))
* **agent, agent-ui:** Add pluggable conversation strategy to the run controller (B8) ([c13a55d](https://github.com/umbraco/Umbraco.AI/commit/c13a55d0c9b0af51e38251b4b7fadc69f14d4c09))
* **copilot-workspace, agent-ui:** Conversation polish — move to project, auto-title, focus ([7e003c5](https://github.com/umbraco/Umbraco.AI/commit/7e003c5a6fcac5c74b6d72dfa2ca39ed624646bc))
* **copilot,agent-ui:** Greet with the item name in the empty chat via an empty-state slot ([30b48b6](https://github.com/umbraco/Umbraco.AI/commit/30b48b65fc60aed2ccb97dac9454f524d7b5f576))
* **copilot,agent-ui:** Refocus the contextual copilot with context framing and per-node history ([36669e8](https://github.com/umbraco/Umbraco.AI/commit/36669e85cd67b6f7c03db0ac58a4d3d19b19a117))

### fix

* **agent-ui:** Focus the chat composer once the agents list resolves ([c44ff2b](https://github.com/umbraco/Umbraco.AI/commit/c44ff2bda4c34304ed459241ddf422fdab231dc0))

### refactor

* **agent-ui,copilot:** Rename the empty-state slot to empty-state-message ([7b7445a](https://github.com/umbraco/Umbraco.AI/commit/7b7445a7596686f1908273cbe4b7c1261f23117c))

## [18.0.5](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.0.4...Umbraco.AI.Agent.UI@18.0.5) (2026-09-24)

### fix

* **agent-ui:** Clear stale "Calling <tool>..." status once its result arrives ([e8b5a83](https://github.com/umbraco/Umbraco.AI/commit/e8b5a831835565ba0412af6cce5fed5286ae1c20))
* **agent-ui:** Never leave a dangling tool_use behind an aborted or cancelled tool call ([96ff118](https://github.com/umbraco/Umbraco.AI/commit/96ff1185249ccf0a172481b6be320d67f08abd16))

## [18.0.4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.0.2...Umbraco.AI.Agent.UI@18.0.4) (2026-09-10)

### fix

* **agent-ui:** Re-arm auto-follow when a consumer swaps the message source ([0adb51a](https://github.com/umbraco/Umbraco.AI/commit/0adb51a96200b95fb77f28e39d79c79a2096dabd))
* **agent-ui:** Stop chat auto-scroll from overriding a manual scroll during streaming ([87e06b1](https://github.com/umbraco/Umbraco.AI/commit/87e06b1a0bf07de8af91f40d50b06761277473c3))
* **frontend,agent,agent-ui:** Stop custom elements double-registering in the backoffice ([edadf41](https://github.com/umbraco/Umbraco.AI/commit/edadf41d9a20c38d16cbda44336a57d99edc0d4c))

## [18.0.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.0.1...Umbraco.AI.Agent.UI@18.0.2) (2026-08-17)

### fix

* **agent-ui:** Move internal-only components out of the shared entry file ([0d176e7](https://github.com/umbraco/Umbraco.AI/commit/0d176e77), closes [#325](https://github.com/umbraco/Umbraco.AI/pull/325) [#324](https://github.com/umbraco/Umbraco.AI/issues/324))

## [18.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@18.0.0...Umbraco.AI.Agent.UI@18.0.1) (2026-07-06)

### fix

* **agent-ui:** Correlate backend approval tool call to a single chat entry ([25bc2cb](https://github.com/umbraco/Umbraco.AI/commit/25bc2cb8831b3d63cdb8129b9593d70228a011f5))
* **agent-ui:** Show denied backend approval as errored, matching frontend tools ([b4e9c05](https://github.com/umbraco/Umbraco.AI/commit/b4e9c050f656d76c2632aabeede1431eb1dd6ed7))
* **agent,agent-ui:** Wire human_approval resume entries to backend ([f789e83](https://github.com/umbraco/Umbraco.AI/commit/f789e83b1c66f89d2d82903cb2156fd4f7a4f6c3))

## [18.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@17.0.0...Umbraco.AI.Agent.UI@18.0.0) (2026-06-25)

### Internal

* Bump major version to align with Umbraco CMS v18.

## [17.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.1...Umbraco.AI.Agent.UI@17.0.0) (2026-06-22)

### ⚠ BREAKING CHANGE

* **agent-ui:** Adopt AG-UI typed multimodal content (image/audio/video/document). The legacy `AGUIBinaryInputContent` shape has been replaced with four spec-defined typed variants (`image`, `audio`, `video`, `document`) with a nested `source: { type, value, mimeType }` discriminator. Custom tool renderers or frontend tools consuming binary content must migrate to the new shapes.

### fix

* **agent-ui,agent:** Align frontend-tool interrupt reason with server ([018b0d8](https://github.com/umbraco/Umbraco.AI/commit/018b0d800ce9c7701f40f07776b92be2af5396fb))

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.0...Umbraco.AI.Agent.UI@1.0.1) (2026-06-04)

### Internal

* Bump to align with Umbraco.AI 1.14.0.

## [1.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.0-alpha8...Umbraco.AI.Agent.UI@1.0.0) (2026-05-14)

### Notes

First stable 1.0.0 release of the reusable chat UI infrastructure.

## [1.0.0-alpha8](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.0-alpha7...Umbraco.AI.Agent.UI@1.0.0-alpha8) (2026-05-06)

### fix

* **agent-ui:** Constrain markdown images to chat message width ([5d7eb54](https://github.com/umbraco/Umbraco.AI/commit/5d7eb5480fd36dfca31a02cc09e9c1c7758cb001))

## [1.0.0-alpha7](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.0-alpha6...Umbraco.AI.Agent.UI@1.0.0-alpha7) (2026-04-30)

### fix

* **agent-ui:** Bump Umbraco CMS peer dependency minimum to 17.3.0 ([342d3e1](https://github.com/umbraco/Umbraco.AI/commit/342d3e1ff10bf9e22be17b5c441a030e0327d4ab))

## [1.0.0-alpha6](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.0-alpha5...Umbraco.AI.Agent.UI@1.0.0-alpha6) (2026-04-08)

### feat

* **agent-ui:** Add voice recording button for speech-to-text input ([5548942](https://github.com/umbraco/Umbraco.AI/commit/554894200817e345be510a08cb3c4ff3efd95af3))

## [1.0.0-alpha5](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.0-alpha4...Umbraco.AI.Agent.UI@1.0.0-alpha5) (2026-03-26)

### feat

* **agent-ui:** Add file upload support with drag-and-drop, image preview, and multimodal messages ([4dec0d8](https://github.com/umbraco/Umbraco.AI/commit/4dec0d84ecc7990643b9075cfe85317509dfc002))

## [1.0.0-alpha4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.0-alpha3...Umbraco.AI.Agent.UI@1.0.0-alpha4) (2026-03-16)

### fix

* **agent-ui:** Prevent duplicate error messages in chat run controller ([b1d6c20](https://github.com/umbraco/Umbraco.AI/commit/b1d6c20f8b2de2813b99bfaf1e09f46f3634415b))

## [1.0.0-alpha3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.0-alpha2...Umbraco.AI.Agent.UI@1.0.0-alpha3) (2026-03-04)

## [1.0.0-alpha2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.UI@1.0.0-alpha1...Umbraco.AI.Agent.UI@1.0.0-alpha2) (2026-02-17)

* Revert "fix(copilot): Fix agent filtering to handle null context gracefully" ([27e3dd8](https://github.com/umbraco/Umbraco.AI/commit/27e3dd85061a5d194f4371190b55461445c82ead))

### feat

* **agent-ui:** Add resolvedAgent$ observable for auto mode attribution ([964ae28](https://github.com/umbraco/Umbraco.AI/commit/964ae284fc386f5e279da6d2c2b93cc37d2eb256))
* **agent-ui:** Add visual agent attribution to chat messages ([0ff9b5f](https://github.com/umbraco/Umbraco.AI/commit/0ff9b5f2ad97841a1befea7625b49e5ff91c6be6))
* **agent,agent-ui:** Add agent attribution to chat messages ([6a55d65](https://github.com/umbraco/Umbraco.AI/commit/6a55d652dce5088f5b54f14ee188149ecea16d0a))

### refactor

* **core,agent-ui,copilot:** Extract surface contributor into reusable kind ([7e2100c](https://github.com/umbraco/Umbraco.AI/commit/7e2100c5c3a69f81b71988378fc6997f4beaa5c5))
* **core,agent-ui,copilot:** Rename surface kind to agentSurface ([c578ef3](https://github.com/umbraco/Umbraco.AI/commit/c578ef3aa9a88f925241b2ea811dc83563d19a5c))

## [1.0.0-alpha1](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Agent.UI@1.0.0-alpha1) (2026-02-10)

### feat

* **agent-ui,copilot:** Extract shared chat UI into @umbraco-ai/agent-ui package ([71ecf0c](https://github.com/umbraco/Umbraco.AI/commit/71ecf0cf411c2698534332b728bd8be87029fb42))
* **agent-ui,copilot:** Implement entity context contract and move frontend tool repository ([27a29ba](https://github.com/umbraco/Umbraco.AI/commit/27a29ba089bb79bfa12b6102bcae297c624d439f))

### fix

* **agent-ui:** Register custom elements to enable HITL approval rendering ([1d5e1b4](https://github.com/umbraco/Umbraco.AI/commit/1d5e1b4ab2bf8301fade604218924d1b608f4932))
