# Changelog - Umbraco.AI.Agent.Copilot.Workspace

All notable changes to Umbraco.AI.Agent.Copilot.Workspace will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


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