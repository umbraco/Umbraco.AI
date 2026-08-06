# Changelog - Umbraco.AI.Agent.Copilot.Workspace

All notable changes to Umbraco.AI.Agent.Copilot.Workspace will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.0.0-rc.2](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Agent.Copilot.Workspace@17.0.0-rc.2) (2026-08-06)

### feat

* **conversations:** Add entity lifecycle notifications and block in-use project deletes ([19cd38e](https://github.com/umbraco/Umbraco.AI/commit/19cd38e54d6e4ad1ebfbace6cd6f6755ceaee25f))
* **copilot-workspace:** Add standalone persisted AI chat section (conversations + projects) ([80228f5](https://github.com/umbraco/Umbraco.AI/commit/80228f5c9ce689d63609565ca7af8e36f47deeec)), closes [#255](https://github.com/umbraco/Umbraco.AI/issues/255)
* **copilot-workspace:** Let an unsaved chat hold its own contexts and resources ([3b949ad](https://github.com/umbraco/Umbraco.AI/commit/3b949ad8ea04b3a9a2364903149f3e8d17d0feae))

### fix

* **copilot-workspace:** Match the new-chat route to the path the buttons generate ([c7a0e74](https://github.com/umbraco/Umbraco.AI/commit/c7a0e74287e1a1a522d203b3031612197eac546f))
* **core,copilot-workspace:** Stop the workspace store refetching its own write ([0924e94](https://github.com/umbraco/Umbraco.AI/commit/0924e94a444325846e1d3dcf6a51a1c0d44f8857))
