# Changelog - Umbraco.AI.Automate

All notable changes to Umbraco.AI.Automate will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.0.1-rc.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Automate@17.0.0...Umbraco.AI.Automate@17.0.1-rc.1) (2026-08-19)

### fix

* **automate:** Show what a run_automation call will do on its approval card ([4835f8d](https://github.com/umbraco/Umbraco.AI/commit/4835f8d25190a7088d91e65ba5baa9e0b9b8643a))

## [17.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Automate@17.0.0-beta.2...Umbraco.AI.Automate@17.0.0) (2026-07-08)

### Internal

* Graduate to a stable release now that Umbraco.Automate v17 has shipped 17.0.0; bump the Umbraco.Automate.Core dependency floor from 17.0.0-beta to the stable 17.0.0.

## [17.0.0-beta.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Automate@17.0.0-beta...Umbraco.AI.Automate@17.0.0-beta.2) (2026-06-22)

### Internal

* Bump major version to align with Umbraco CMS v17.

## [17.0.0-beta](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Automate@17.0.0-beta) (2026-06-10)

Initial beta release of Umbraco.AI.Automate — Umbraco Automate integration for Umbraco.AI agents, exposing AI agents as workflow actions and AI events as workflow triggers.

### feat

* **automate:** Add Umbraco.AI.Automate package (#124) ([2709a3d](https://github.com/umbraco/Umbraco.AI/commit/2709a3dd64a05bf627a0845796490f34be4c8e4b)), closes [#124](https://github.com/umbraco/Umbraco.AI/issues/124)
* **automate:** Add Automate tools for AI agents ([44b4765](https://github.com/umbraco/Umbraco.AI/commit/44b47650d89a2da09ee5c3b3393396056d2eec8d))
* **agent, automate:** Add agent run completed/failed triggers ([88194e7](https://github.com/umbraco/Umbraco.AI/commit/88194e70e2ffcbd2bd5605d66215f59909cf71eb))
* **automate:** Annotate AI step types with section + node permissions ([3378338](https://github.com/umbraco/Umbraco.AI/commit/3378338c818699ca6d919b14fe36ad636498f48f)), closes [#49](https://github.com/umbraco/Umbraco.AI/issues/49)
* **automate:** Always expose raw agent response as a bindable property ([e94a730](https://github.com/umbraco/Umbraco.AI/commit/e94a730fb25ff6d9b97b23b31c6287a51bd78efb))
* **automate:** Give Transcribe Audio action an explicit output type ([df9ec53](https://github.com/umbraco/Umbraco.AI/commit/df9ec537f05cb6e96dca65d98047ef0e22b2608e))
