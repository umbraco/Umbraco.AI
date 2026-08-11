# Changelog - Umbraco.AI.Anthropic

All notable changes to Umbraco.AI.Anthropic will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [18.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@18.0.2...Umbraco.AI.Anthropic@18.1.0) (2026-08-11)

### feat

* **core,anthropic,frontend:** Add Anthropic prompt caching and cached-token reporting (#295) ([47547fb](https://github.com/umbraco/Umbraco.AI/commit/47547fbbb5d21c9c22b4edae2361dbf4bd3a35fa)), closes [#295](https://github.com/umbraco/Umbraco.AI/issues/295) [#291](https://github.com/umbraco/Umbraco.AI/issues/291)
* **core,openai,anthropic,amazon,frontend:** Enforce per-model declarations in core (#283) ([529f2a5](https://github.com/umbraco/Umbraco.AI/commit/529f2a57d1ad5c2b5a7b937d4ceb115125216180)), closes [#283](https://github.com/umbraco/Umbraco.AI/issues/283)
* **core,openai,anthropic:** Add provider-declared, model-aware capability settings (#269) ([3f370c5](https://github.com/umbraco/Umbraco.AI/commit/3f370c56e6428cda88f667d09b5807b9fe675b94)), closes [#269](https://github.com/umbraco/Umbraco.AI/issues/269)

### fix

* **core,anthropic,openai:** Read the cached input tokens every adapter already reports ([464d9f4](https://github.com/umbraco/Umbraco.AI/commit/464d9f47033ee6d0e6d78b633ce94a816c3e9328)), closes [#291](https://github.com/umbraco/Umbraco.AI/issues/291)
* **core,frontend,openai,anthropic,amazon:** Give temperature a real unset state (#273) ([f4a4d67](https://github.com/umbraco/Umbraco.AI/commit/f4a4d673810a8a96982eabe2328b0da5e6b69904)), closes [#273](https://github.com/umbraco/Umbraco.AI/issues/273) [#256](https://github.com/umbraco/Umbraco.AI/issues/256) [#269](https://github.com/umbraco/Umbraco.AI/issues/269) [#266](https://github.com/umbraco/Umbraco.AI/issues/266)

## [18.0.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@18.0.1...Umbraco.AI.Anthropic@18.0.2) (2026-07-28)

### fix

* **anthropic,openai,amazon:** Drop sampling parameters on models that reject them (#265) ([7a01ed7](https://github.com/umbraco/Umbraco.AI/commit/7a01ed7d8cc35bf35c59660a7ec1e3ff58035256)), closes [#265](https://github.com/umbraco/Umbraco.AI/issues/265)

## [18.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@18.0.0...Umbraco.AI.Anthropic@18.0.1) (2026-07-27)

### Internal

* Bump to align with Umbraco.AI 18.2.0.

## [18.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Anthropic@17.0.0...Umbraco.AI.Anthropic@18.0.0) (2026-06-25)

### Internal

* Bump major version to align with Umbraco CMS v18.

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
