# Changelog - Umbraco.AI.Amazon

All notable changes to Umbraco.AI.Amazon will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@17.0.2...Umbraco.AI.Amazon@17.1.0) (2026-08-11)

### feat

* **core,openai,anthropic,amazon,frontend:** Enforce per-model declarations in core [v17 backport] (#284) ([6c6ff0e](https://github.com/umbraco/Umbraco.AI/commit/6c6ff0effa14b232b0126e7957c1a08d9ab2f49e)), closes [#284](https://github.com/umbraco/Umbraco.AI/issues/284)

### fix

* **deps:** Raise the minimum Umbraco CMS version to 17.5.0.
* **core,frontend,openai,anthropic,amazon:** Give temperature a real unset state (#274) ([38de95d](https://github.com/umbraco/Umbraco.AI/commit/38de95db9f4c29f702f122faa868e24c5cafa93b)), closes [#274](https://github.com/umbraco/Umbraco.AI/issues/274) [#256](https://github.com/umbraco/Umbraco.AI/issues/256) [#269](https://github.com/umbraco/Umbraco.AI/issues/269) [#266](https://github.com/umbraco/Umbraco.AI/issues/266)

## [17.0.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@17.0.1...Umbraco.AI.Amazon@17.0.2) (2026-07-28)

### fix

* **amazon:** Strip Bedrock version suffixes that carry no minor version ([4d07203](https://github.com/umbraco/Umbraco.AI/commit/4d07203b43dbd583997c6f8aa50e73fce0f25d52))
* **anthropic,openai,amazon:** Drop sampling parameters on models that reject them (#267) ([6066b04](https://github.com/umbraco/Umbraco.AI/commit/6066b040ba1d6beb491c5daf04466dc4dcfa5c00)), closes [#267](https://github.com/umbraco/Umbraco.AI/issues/267)

## [17.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@17.0.0...Umbraco.AI.Amazon@17.0.1) (2026-07-27)

### Internal

* Bump to align with Umbraco.AI 17.2.0.

## [17.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.1.8...Umbraco.AI.Amazon@17.0.0) (2026-06-22)

### Internal

* Bump major version to align with Umbraco CMS v17.

## [1.1.8](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.1.7...Umbraco.AI.Amazon@1.1.8) (2026-06-04)

### Internal

* Bump to align with Umbraco.AI 1.14.0.

## [1.1.7](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.1.6...Umbraco.AI.Amazon@1.1.7) (2026-06-01)

### fix

* **amazon:** Discover all embedding models and support Cohere request format ([369ae78](https://github.com/umbraco/Umbraco.AI/commit/369ae7844dd95314144fdab8a9be4b6c6e710c7c))

## [1.1.6](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.1.4...Umbraco.AI.Amazon@1.1.6) (2026-05-20)

### Internal

* Bump to align with Umbraco.AI 1.12.0.

## [1.1.4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.1.2...Umbraco.AI.Amazon@1.1.4) (2026-05-14)

### Internal

* Bump to align with Umbraco.AI 1.11.0.

## [1.1.3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.1.2...Umbraco.AI.Amazon@1.1.3) (2026-03-16)

## [1.1.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.1.1...Umbraco.AI.Amazon@1.1.2) (2026-03-04)

## [1.1.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.1.0...Umbraco.AI.Amazon@1.1.1) (2026-02-17)

### build

* **openai,anthropic,google,microsoft-foundry,amazon:** Add version updates to umbraco-package.json ([46038a4](https://github.com/umbraco/Umbraco.AI/commit/46038a48f0e36c21f2fa50407466f96caec08f41))

### fix

* **amazon:** Added missing translations lang file ([854f96f](https://github.com/umbraco/Umbraco.AI/commit/854f96f9904975dbdde96b8cfb9f88d2b57c0f55))

## [1.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.0.0...Umbraco.AI.Amazon@1.1.0) (2026-02-10)

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Amazon@1.0.0...Umbraco.AI.Amazon@1.0.1) (2026-02-04)

### chore

* **ci:** Add umbraco-marketplace tags to provider packages ([ad8021d](https://github.com/umbraco/Umbraco.AI/commit/ad8021d0e2cd66d25e71d8fef9515f32f85fcf6c))

## [1.0.0] - 2026-02-03

Initial release.

[1.0.0]: https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Amazon@1.0.0
