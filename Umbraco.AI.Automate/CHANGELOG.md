# Changelog - Umbraco.AI.Automate

All notable changes to Umbraco.AI.Automate will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Automate@17.0.1...Umbraco.AI.Automate@17.1.0) (2026-10-19)

### feat

* **automate:** Add a yes/no threshold and send Context as Decision state ([d4da04a](https://github.com/umbraco/Umbraco.AI/commit/d4da04a62ff895de75017c4b62b2918c1435c7b3))
* **automate:** Add an Ask questions Decision action ([ffa9c67](https://github.com/umbraco/Umbraco.AI/commit/ffa9c6724bf8d459b1eee8bea4c4b97ff8b7988f))
* **automate:** Let yes/no criteria take values from earlier steps ([6cf6c77](https://github.com/umbraco/Umbraco.AI/commit/6cf6c7735f68e6d385859d8f2c830a08883f5e91))
* **core,automate:** Add a key/value list editor for Ask pick-one options ([379b9d2](https://github.com/umbraco/Umbraco.AI/commit/379b9d2b9e6715c5f308c2336e27326d81dd004e))
* **core,typesafe,deploy,automate,agent:** Add experimental Decision capability ([6c338c2](https://github.com/umbraco/Umbraco.AI/commit/6c338c247273031ba7677db632729d2789fc0157)), closes [#419](https://github.com/umbraco/Umbraco.AI/issues/419)

### fix

* **automate:** Report AI provider failures with a matching step error category ([e882587](https://github.com/umbraco/Umbraco.AI/commit/e8825874e1dbca913d0a7039d6adb08c9c1f7979))

## [17.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Automate@17.0.1-rc.4...Umbraco.AI.Automate@17.0.1) (2026-10-01)

### feat

* **agent,automate:** Let Run Agent steps make changes that don't need approval ([90dd4c1](https://github.com/umbraco/Umbraco.AI/commit/90dd4c132ffe4b5306c4f24acff05e6f64b7c78c))
* **automate:** Pass media attachments to the Run AI Agent action ([f72ded1](https://github.com/umbraco/Umbraco.AI/commit/f72ded12e6f605433d01d321a31d532292a3be72)), closes [#443](https://github.com/umbraco/Umbraco.AI/issues/443)

### fix

* **automate:** Honour the Agent filter on agent run triggers ([0446e63](https://github.com/umbraco/Umbraco.AI/commit/0446e635de8cc8e9ab896d4fb04eb2ad4fc5f4e8))

## [17.0.1-rc.4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Automate@17.0.1-rc.3...Umbraco.AI.Automate@17.0.1-rc.4) (2026-09-16)

## [17.0.1-rc.3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Automate@17.0.1-rc.2...Umbraco.AI.Automate@17.0.1-rc.3) (2026-09-08)

## [17.0.1-rc.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Automate@17.0.1-rc.1...Umbraco.AI.Automate@17.0.1-rc.2) (2026-08-21)

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
