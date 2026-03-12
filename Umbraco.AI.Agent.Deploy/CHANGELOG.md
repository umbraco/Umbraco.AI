# Changelog - Umbraco.AI.Agent.Deploy

All notable changes to Umbraco.AI.Agent.Deploy will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-beta2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Deploy@1.0.0-beta1...Umbraco.AI.Agent.Deploy@1.0.0-beta2) (2026-03-12)

### feat

* **agent-deploy:** Add orchestrated agent deployment support ([5f57d90](https://github.com/umbraco/Umbraco.AI/commit/5f57d902))
  - Replace ContextIds/AllowedToolIds/Instructions with AgentType and polymorphic Config
  - Support Standard and Orchestrated agent type serialization

## [1.0.0-beta1](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Agent.Deploy@1.0.0-beta1) (2026-03-02)

### Added
- Initial release of Umbraco.AI.Agent.Deploy
- Agent deployment with tool permissions
- Profile dependency resolution
- User group permission deployment with validation
- Scope deployment (sections, entity types)
- Surface configuration deployment
- AllowedToolIds and AllowedToolScopeIds support
