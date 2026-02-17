# Changelog - Umbraco.AI.Agent.UI

All notable changes to Umbraco.AI.Agent.UI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
