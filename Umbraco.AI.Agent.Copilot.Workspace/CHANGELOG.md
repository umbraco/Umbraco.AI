# Changelog - Umbraco.AI.Agent.Copilot.Workspace

All notable changes to Umbraco.AI.Agent.Copilot.Workspace will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.2.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot.Workspace@17.1.1...Umbraco.AI.Agent.Copilot.Workspace@17.2.0) (2026-10-19)

### feat

* **agent-ui,copilot,copilot-workspace:** Aggregate starter prompts across agents in Auto mode ([3fc1518](https://github.com/umbraco/Umbraco.AI/commit/3fc151852b3a015d402ddf3b09f1f3efb4546de0))
* **agent-ui,copilot,copilot-workspace:** Render starter prompts in the empty chat state ([9a6723a](https://github.com/umbraco/Umbraco.AI/commit/9a6723a4971e60e9cd0656a31276eaab8c7ddc98))

### fix

* **copilot-workspace:** Depend on Umbraco.AI.Agent.UI so New chat loads ([7107460](https://github.com/umbraco/Umbraco.AI/commit/7107460a023dec286d32325fe98f8a39ae045fc6)), closes [#515](https://github.com/umbraco/Umbraco.AI/issues/515)
* **core,agent,copilot-workspace:** Show the real reason when an AI management request fails ([91aeaa6](https://github.com/umbraco/Umbraco.AI/commit/91aeaa679d3aa7c3c99021b02d87e3d29f44b3b2))

## [17.1.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot.Workspace@17.1.0...Umbraco.AI.Agent.Copilot.Workspace@17.1.1) (2026-10-06)

### fix

* **copilot-workspace:** Send the backoffice token on conversation and project calls ([828ddf6](https://github.com/umbraco/Umbraco.AI/commit/828ddf67b85a3d8c7f5ed5361eac684810d61be7))

## [17.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot.Workspace@17.0.0...Umbraco.AI.Agent.Copilot.Workspace@17.1.0) (2026-10-05)

### feat

* **copilot-workspace:** Route Copilot Workspace auto selection through IAIAgentSelectionService ([d8acdb6](https://github.com/umbraco/Umbraco.AI/commit/d8acdb6e6864854bac369259fa6f007c80829b0c))
* **copilot-workspace:** Show agent names on reopened Copilot Workspace replies ([801c0bb](https://github.com/umbraco/Umbraco.AI/commit/801c0bbc3e29c60f16301f141bd6451b03fed90d))
* **copilot-workspace:** Store the producing agent on Copilot Workspace assistant messages ([6bed8b5](https://github.com/umbraco/Umbraco.AI/commit/6bed8b5fd8b47b926b40d7c254f04928a80addb2))

### fix

* **agent,copilot-workspace:** Require surface opt-in for explicitly named agents ([39bdc71](https://github.com/umbraco/Umbraco.AI/commit/39bdc715dded343fee2b575680ca262491e4a849))
* **copilot-workspace:** Give GetLastAssistantAgentIdAsync a default body ([68aaf96](https://github.com/umbraco/Umbraco.AI/commit/68aaf9629f3687d8bee5413acd2564ff942baf6d))
* **copilot-workspace:** Use the v18 migration IDs for MessageAgentId ([6f05776](https://github.com/umbraco/Umbraco.AI/commit/6f057767fb61d4d5e4d4815b670813d9af6889d5))

## [17.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot.Workspace@17.0.0-rc.4...Umbraco.AI.Agent.Copilot.Workspace@17.0.0) (2026-10-01)

### fix

* **copilot-workspace:** Keep a conversation's tool calls when it is reopened ([5f192f3](https://github.com/umbraco/Umbraco.AI/commit/5f192f30549b68019cc392b35eedf9e85e250d12))

### refactor

* **copilot-workspace,agent:** Simplify the restored-message mapper ([3ca894d](https://github.com/umbraco/Umbraco.AI/commit/3ca894df44cd810a72df146be9f8a21ad448376e))

## [17.0.0-rc.4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot.Workspace@17.0.0-rc.3...Umbraco.AI.Agent.Copilot.Workspace@17.0.0-rc.4) (2026-09-16)

### fix

* **agent,copilot-workspace:** Load persisted session state before other scoped DB work ([9f0f457](https://github.com/umbraco/Umbraco.AI/commit/9f0f4570ddea48c352de52143d333218e1858e69)), closes [umbraco/Umbraco.AI#375](https://github.com/umbraco/Umbraco.AI/issues/375)
* **agent,copilot-workspace:** Stop duplicate/stale conversation history on reconnect ([96ac859](https://github.com/umbraco/Umbraco.AI/commit/96ac8590b6cf394dadeb774c54f0d8d53c2cac5d)), closes [umbraco/Umbraco.AI#375](https://github.com/umbraco/Umbraco.AI/issues/375)

## [17.0.0-rc.3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot.Workspace@17.0.0-rc.2...Umbraco.AI.Agent.Copilot.Workspace@17.0.0-rc.3) (2026-09-10)

### fix

* **agent-ui,copilot-workspace:** Stop chat auto-scroll from overriding a manual scroll ([03c381f](https://github.com/umbraco/Umbraco.AI/commit/03c381f9c622f8682e4b08ce0cd2a3fd857022bb)), closes [#348](https://github.com/umbraco/Umbraco.AI/issues/348)
* **agent,conversations:** Dedupe persisted messages by client id, not text ([2b8f276](https://github.com/umbraco/Umbraco.AI/commit/2b8f276c5d6526aee3ddaab873f60408bf4aa0a0))
* **conversations,agent-ui:** Stop returning to a conversation duplicating user messages ([5cde524](https://github.com/umbraco/Umbraco.AI/commit/5cde524c75c485ad12a191540c40d96794ac2be5))
* **copilot-workspace:** Show an error and fix wording when a project delete is rejected ([f72221b](https://github.com/umbraco/Umbraco.AI/commit/f72221bdc9f7437bb0bae3899d0accb2b49dfe22))

## [17.0.0-rc.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot.Workspace@17.0.0-rc.1...Umbraco.AI.Agent.Copilot.Workspace@17.0.0-rc.2) (2026-08-22)

### fix

* **agent,copilot-workspace:** Auto-deny an approval left dangling by a reload ([5f91fb9](https://github.com/umbraco/Umbraco.AI/commit/5f91fb9fa7a55e6942a56ffa798019bd7d1fabb7))

## [17.0.0-rc.1](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Agent.Copilot.Workspace@17.0.0-rc.1) (2026-08-18)

### feat

* **conversations:** Add entity lifecycle notifications and block in-use project deletes ([19cd38e](https://github.com/umbraco/Umbraco.AI/commit/19cd38e54d6e4ad1ebfbace6cd6f6755ceaee25f))
* **copilot-workspace:** Add standalone persisted AI chat section (conversations + projects) ([80228f5](https://github.com/umbraco/Umbraco.AI/commit/80228f5c9ce689d63609565ca7af8e36f47deeec)), closes [#255](https://github.com/umbraco/Umbraco.AI/issues/255)
* **copilot-workspace:** Let an unsaved chat hold its own contexts and resources ([3b949ad](https://github.com/umbraco/Umbraco.AI/commit/3b949ad8ea04b3a9a2364903149f3e8d17d0feae))

### fix

* **agent,conversations:** Lead every agent request with the runtime-context system message ([b262204](https://github.com/umbraco/Umbraco.AI/commit/b2622049e08d6b332d227809794995e13c8887b0))
* **agent,copilot-workspace:** Stop double-storing attachment bytes in conversation history ([ff8106c](https://github.com/umbraco/Umbraco.AI/commit/ff8106c950eb83064d2029f7a8f47a77068fd576))
* **agent,copilot-workspace:** Stop the file retention sweep aging out live persisted conversations ([01757fe](https://github.com/umbraco/Umbraco.AI/commit/01757feb51b307b61c7970096fd83eeac9133e9a))
* **conversations,agent-ui:** Replace the regenerated answer instead of appending it ([17fcb03](https://github.com/umbraco/Umbraco.AI/commit/17fcb03cd11feea224ab8c38401f013faa46f400))
* **copilot-workspace:** Match the new-chat route to the path the buttons generate ([c7a0e74](https://github.com/umbraco/Umbraco.AI/commit/c7a0e74287e1a1a522d203b3031612197eac546f))
* **copilot-workspace:** Remove the redundant conversation file endpoint ([54e03d0](https://github.com/umbraco/Umbraco.AI/commit/54e03d0764035c992d327a0fbaf5e177b721c950))
* **core,agent,agent-ui,copilot,copilot-workspace,ci:** Stop leaking internal components into the shared entry file (#328) ([0431da5](https://github.com/umbraco/Umbraco.AI/commit/0431da59443dfcf2ebbadf3d59f9d1709bcd2177)), closes [#328](https://github.com/umbraco/Umbraco.AI/issues/328) [#324](https://github.com/umbraco/Umbraco.AI/issues/324) [#324](https://github.com/umbraco/Umbraco.AI/issues/324)
* **core,copilot-workspace:** Stop the workspace store refetching its own write ([0924e94](https://github.com/umbraco/Umbraco.AI/commit/0924e94a444325846e1d3dcf6a51a1c0d44f8857))