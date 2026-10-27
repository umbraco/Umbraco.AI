# Changelog - Umbraco.AI.Agent.Deploy

All notable changes to Umbraco.AI.Agent.Deploy will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [18.1.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@18.1.0...Umbraco.AI.Agent.Deploy@18.1.1) (2026-10-26)

### fix

* **agent-deploy:** Deploy agent user group permissions by user group alias ([5e6faa9](https://github.com/umbraco/Umbraco.AI/commit/5e6faa9c04b4fb8c5e45773c4859d15fbe9f3344))

## [18.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@18.0.2...Umbraco.AI.Agent.Deploy@18.1.0) (2026-10-19)

### feat

* **agent,agent-deploy:** Add starter prompts to the agent definition ([dce6fa1](https://github.com/umbraco/Umbraco.AI/commit/dce6fa1d6388b3dafad36eb11c4d3de5b5590071))

### fix

* **deploy,prompt-deploy,agent-deploy:** Declare referenced contexts as deploy dependencies ([ef4974d](https://github.com/umbraco/Umbraco.AI/commit/ef4974d7d8146cbceecac13510b3f9667fd4303f))

## [18.0.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@18.0.0...Umbraco.AI.Agent.Deploy@18.0.2) (2026-08-11)

### Internal

* Bump to align with Umbraco.AI.Deploy 18.1.0.

## [18.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@17.0.0...Umbraco.AI.Agent.Deploy@18.0.0) (2026-06-25)

### Internal

* Bump major version to align with Umbraco CMS v18.

## [17.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@1.0.1...Umbraco.AI.Agent.Deploy@17.0.0) (2026-06-22)

### Internal

* Bump major version to align with Umbraco CMS v17.

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@1.0.0-beta2...Umbraco.AI.Agent.Deploy@1.0.1) (2026-06-04)

### Internal

* Bump to align with Umbraco.AI 1.14.0.

## [1.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@1.0.0-beta2...Umbraco.AI.Agent.Deploy@1.0.0) (2026-05-14)

### Notes

First stable 1.0.0 release.

## [1.0.0-beta2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@1.0.0-beta1...Umbraco.AI.Agent.Deploy@1.0.0-beta2) (2026-04-16)

### feat

* **core,agent,agent-deploy:** Generalize guardrail evaluators and promote GuardrailIds to agent level ([8371869](https://github.com/umbraco/Umbraco.AI/commit/8371869be6d3d84d8f2451f111d7086dc07e4aa9))
* **agent-deploy:** Add orchestrated agent deployment support ([5f57d90](https://github.com/umbraco/Umbraco.AI/commit/5f57d902)), closes [#88](https://github.com/umbraco/Umbraco.AI/issues/88)
* **agent-deploy:** Add guardrail Deploy support to agent connector ([0527502](https://github.com/umbraco/Umbraco.AI/commit/05275020))

## [1.0.0-beta1](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Agent.Deploy@1.0.0-beta1) (2026-03-02)

### Added
- Initial release of Umbraco.AI.Agent.Deploy
- Agent deployment with tool permissions
- Profile dependency resolution
- User group permission deployment with validation
- Scope deployment (sections, entity types)
- Surface configuration deployment
- AllowedToolIds and AllowedToolScopeIds support
