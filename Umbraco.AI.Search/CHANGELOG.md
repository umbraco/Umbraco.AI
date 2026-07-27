# Changelog - Umbraco.AI.Search

All notable changes to Umbraco.AI.Search will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@17.0.0...Umbraco.AI.Search@17.0.1) (2026-07-27)

### Internal

* Bump to align with Umbraco.AI 17.2.0.

### fix

* **search, deps:** Target released Umbraco.Cms.Search 17.x ([3a74419](https://github.com/umbraco/Umbraco.AI/commit/3a7441913c9991cd0e218a00fdccd26e9cbdd481)), closes [#237](https://github.com/umbraco/Umbraco.AI/issues/237)

## [17.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@1.0.0...Umbraco.AI.Search@17.0.0) (2026-06-22)

### feat

* **search:** Add usage telemetry provider for CMS telemetry report ([a6fcf20](https://github.com/umbraco/Umbraco.AI/commit/a6fcf20b68f02755f5b683e6e775e9fe36c2a0a3))

## [1.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@1.0.0-beta10...Umbraco.AI.Search@1.0.0) (2026-06-10)

First stable release of Umbraco.AI.Search, built against the stable Umbraco.Cms.Search.Core 1.0.0.

### feat

* **core,agent,prompt,search:** Use dedicated connection for custom AI connection strings ([ca01d98](https://github.com/umbraco/Umbraco.AI/commit/ca01d98c4c76b471c736a47659cfcab99a3733bd)), closes [umbraco/Umbraco-CMS#22133](https://github.com/umbraco/Umbraco-CMS/issues/22133)

## [1.0.0-beta10](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@1.0.0-beta9...Umbraco.AI.Search@1.0.0-beta10) (2026-06-04)

### Internal

* Bump to align with Umbraco.AI 1.14.0.

## [1.0.0-beta9](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@1.0.0-beta8...Umbraco.AI.Search@1.0.0-beta9) (2026-06-01)

### Internal

* Bump to align with Umbraco.AI 1.13.0.

## [1.0.0-beta8](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@1.0.0-beta6...Umbraco.AI.Search@1.0.0-beta8) (2026-05-20)

### Internal

* Bump to align with Umbraco.AI 1.12.0.

## [1.0.0-beta6](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@1.0.0-beta4...Umbraco.AI.Search@1.0.0-beta6) (2026-05-14)

### Internal

* Bump to align with Umbraco.AI 1.11.0.

## [1.0.0-beta4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Search@1.0.0-beta3...Umbraco.AI.Search@1.0.0-beta4) (2026-05-06)

### fix

* **search:** Update Umbraco.Cms.Search to 1.0.0-beta.5 ([f18ca81](https://github.com/umbraco/Umbraco.AI/commit/f18ca81eeb2ca4156ed5a0df0c1af921d1c389ff)), closes [#117](https://github.com/umbraco/Umbraco.AI/issues/117)

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
