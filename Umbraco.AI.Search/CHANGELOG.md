# Changelog - Umbraco.AI.Search

All notable changes to Umbraco.AI.Search will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-beta3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@1.0.0-beta2...Umbraco.AI.Search@1.0.0-beta3) (2026-04-16)

### fix

* **core,prompt,agent,search:** Fix EF Core migrations failing on startup ([51069e9](https://github.com/umbraco/Umbraco.AI/commit/51069e955c96c4dba4b6cd43aa3634e7d5d5f930)), closes [#121](https://github.com/umbraco/Umbraco.AI/issues/121)

## [1.0.0-beta2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@1.0.0-beta1...Umbraco.AI.Search@1.0.0-beta2) (2026-04-08)

* Add custom connection string and per-product migrations history table (#117) ([237e545](https://github.com/umbraco/Umbraco.AI/commit/237e54568f6297a62d9649732735cfd248d25ca9)), closes [#117](https://github.com/umbraco/Umbraco.AI/issues/117) [umbraco/Umbraco-CMS#22133](https://github.com/umbraco/Umbraco-CMS/issues/22133)

### fix

* **search:** Remove CMS Search workarounds resolved in beta 3 ([385f5a3](https://github.com/umbraco/Umbraco.AI/commit/385f5a3a486faeb28a4e6e3977af5c50d8a934fa)), closes [umbraco/Umbraco.Cms.Search#108](https://github.com/umbraco/Umbraco.Cms.Search/issues/108)
* **search:** Update indexer and searcher to use builder-based embedding API ([e2c47a1](https://github.com/umbraco/Umbraco.AI/commit/e2c47a182d4516ded18ae81017a078ce8afc7183))

## [1.0.0-beta1](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Search@1.0.0-beta1) (2026-03-26)

### feat

* **search,core:** Add Umbraco.AI.Search semantic vector search package ([dfffb84](https://github.com/umbraco/Umbraco.AI/commit/dfffb848d41449639a965da8cfde833c8c426b50))
