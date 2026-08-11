# Changelog - Umbraco.AI.Anthropic

All notable changes to Umbraco.AI.Anthropic will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@17.0.2...Umbraco.AI.Anthropic@17.1.0) (2026-08-11)

### feat

* **core,anthropic,frontend:** Add Anthropic prompt caching and cached-token reporting (v17 backport) (#296) ([42fe957](https://github.com/umbraco/Umbraco.AI/commit/42fe957f4e350ff7d83ea8e791d53ac54207c982)), closes [#296](https://github.com/umbraco/Umbraco.AI/issues/296) [#295](https://github.com/umbraco/Umbraco.AI/issues/295) [#291](https://github.com/umbraco/Umbraco.AI/issues/291)
* **core,openai,anthropic,amazon,frontend:** Enforce per-model declarations in core [v17 backport] (#284) ([6c6ff0e](https://github.com/umbraco/Umbraco.AI/commit/6c6ff0effa14b232b0126e7957c1a08d9ab2f49e)), closes [#284](https://github.com/umbraco/Umbraco.AI/issues/284)
* **core,openai,anthropic:** Add provider-declared, model-aware capability settings (#270) ([a86199d](https://github.com/umbraco/Umbraco.AI/commit/a86199da8612ca03939ec7297934177f201861ef)), closes [#270](https://github.com/umbraco/Umbraco.AI/issues/270) [#269](https://github.com/umbraco/Umbraco.AI/issues/269)

### fix

* **deps:** Raise the minimum Umbraco CMS version to 17.5.0.
* **core,anthropic,openai:** Read the cached input tokens adapters already report (v17 backport) ([40e0f34](https://github.com/umbraco/Umbraco.AI/commit/40e0f3443114bda9cd1082468df554d8e686b65e)), closes [#291](https://github.com/umbraco/Umbraco.AI/issues/291)
* **core,frontend,openai,anthropic,amazon:** Give temperature a real unset state (#274) ([38de95d](https://github.com/umbraco/Umbraco.AI/commit/38de95db9f4c29f702f122faa868e24c5cafa93b)), closes [#274](https://github.com/umbraco/Umbraco.AI/issues/274) [#256](https://github.com/umbraco/Umbraco.AI/issues/256) [#269](https://github.com/umbraco/Umbraco.AI/issues/269) [#266](https://github.com/umbraco/Umbraco.AI/issues/266)

## [17.0.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@17.0.1...Umbraco.AI.Anthropic@17.0.2) (2026-07-28)

### fix

* **anthropic,openai,amazon:** Drop sampling parameters on models that reject them (#267) ([6066b04](https://github.com/umbraco/Umbraco.AI/commit/6066b040ba1d6beb491c5daf04466dc4dcfa5c00)), closes [#267](https://github.com/umbraco/Umbraco.AI/issues/267)

## [17.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@17.0.0...Umbraco.AI.Anthropic@17.0.1) (2026-07-27)

### Internal

* Bump to align with Umbraco.AI 17.2.0.

## [17.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.3.6...Umbraco.AI.Anthropic@17.0.0) (2026-06-22)

### fix

* **providers, agent, anthropic:** Classify provider errors for friendly chat messages ([8f71209](https://github.com/umbraco/Umbraco.AI/commit/8f712099ca3479774e7d011152ed93276642e8dd)), closes [#174](https://github.com/umbraco/Umbraco.AI/issues/174) [#174](https://github.com/umbraco/Umbraco.AI/issues/174)
* **providers, agent, anthropic:** Route error classification through per-provider client decorators ([3c9b8d6](https://github.com/umbraco/Umbraco.AI/commit/3c9b8d655e87219927e33dee921e057392fd13e7)), closes [#174](https://github.com/umbraco/Umbraco.AI/issues/174)

## [1.3.6](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.3.5...Umbraco.AI.Anthropic@1.3.6) (2026-06-04)

### Internal

* Bump to align with Umbraco.AI 1.14.0.

## [1.3.5](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.3.4...Umbraco.AI.Anthropic@1.3.5) (2026-06-01)

### Internal

* Bump to align with Umbraco.AI 1.13.0.

## [1.3.4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.3.2...Umbraco.AI.Anthropic@1.3.4) (2026-05-20)

### Internal

* Bump to align with Umbraco.AI 1.12.0.

## [1.3.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.3.0...Umbraco.AI.Anthropic@1.3.2) (2026-05-14)

### Internal

* Bump to align with Umbraco.AI 1.11.0.
* Bump `Anthropic` SDK minimum to 12.20.1.

## [1.3.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.2.2...Umbraco.AI.Anthropic@1.3.0) (2026-04-08)

### feat

* **core,prompt,anthropic:** Add structured output via M.E.AI and fix prompt display ([d267d2d](https://github.com/umbraco/Umbraco.AI/commit/d267d2d8cbcd6c7e9d42efb6e8ae32092e5582f1))

### fix

* **core,openai,anthropic,google,microsoft-foundry:** Fix graders and update provider packages ([087a132](https://github.com/umbraco/Umbraco.AI/commit/087a1327d18412e64502c0ac780a8b4b2343cbec))

## [1.2.3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.2.2...Umbraco.AI.Anthropic@1.2.3) (2026-03-16)

### fix

* **core,openai,anthropic,google,microsoft-foundry:** Fix graders and update provider packages ([087a132](https://github.com/umbraco/Umbraco.AI/commit/087a1327d18412e64502c0ac780a8b4b2343cbec))

## [1.2.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.2.1...Umbraco.AI.Anthropic@1.2.2) (2026-03-04)

## [1.2.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.2.0...Umbraco.AI.Anthropic@1.2.1) (2026-03-02)

## [1.2.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.1.0...Umbraco.AI.Anthropic@1.2.0) (2026-02-17)

### build

* **openai,anthropic,google,microsoft-foundry,amazon:** Add version updates to umbraco-package.json ([46038a4](https://github.com/umbraco/Umbraco.AI/commit/46038a48f0e36c21f2fa50407466f96caec08f41))

## [1.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.0.0...Umbraco.AI.Anthropic@1.1.0) (2026-02-10)

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@1.0.0...Umbraco.AI.Anthropic@1.0.1) (2026-02-04)

### chore

* **ci:** Add umbraco-marketplace tags to provider packages ([ad8021d](https://github.com/umbraco/Umbraco.AI/commit/ad8021d0e2cd66d25e71d8fef9515f32f85fcf6c))

## [1.0.0] - 2026-02-03

Initial release.

[1.0.0]: https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Anthropic@1.0.0
