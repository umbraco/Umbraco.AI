# Changelog - Umbraco.AI.Agent.Deploy

All notable changes to Umbraco.AI.Agent.Deploy will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.1.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@17.1.0...Umbraco.AI.Agent.Deploy@17.1.1) (2026-10-26)

### fix

* **agent-deploy:** Deploy agent user group permissions by user group alias ([53e0641](https://github.com/umbraco/Umbraco.AI/commit/53e06410ae3b4b300ff3951a56fb9f7377c0ae6b))

## [17.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@17.0.1...Umbraco.AI.Agent.Deploy@17.1.0) (2026-10-19)

### feat

* **agent,agent-deploy:** Add starter prompts to the agent definition ([e9efef8](https://github.com/umbraco/Umbraco.AI/commit/e9efef8e15eeb1abb8e58a4644cf6b269016130b))

### fix

* **deploy,prompt-deploy,agent-deploy:** Declare referenced contexts as deploy dependencies ([c6a6c36](https://github.com/umbraco/Umbraco.AI/commit/c6a6c36bfa4bf9649fa060d228233b91877553ea))

## [17.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@17.0.0...Umbraco.AI.Agent.Deploy@17.0.1) (2026-08-11)

### Internal

* Bump to align with Umbraco.AI.Deploy 17.1.0.

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
