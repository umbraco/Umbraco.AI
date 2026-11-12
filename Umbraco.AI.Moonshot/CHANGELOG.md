# Changelog - Umbraco.AI.Moonshot

All notable changes to Umbraco.AI.Moonshot will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [19.0.0-rc.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Moonshot@18.0.2...Umbraco.AI.Moonshot@19.0.0-rc.1) (2026-11-12)

### ⚠ BREAKING CHANGES

* Requires Umbraco CMS 19 and .NET 11.

### fix

* Stop OpenAI SDK providers retrying an out-of-credit reply 3 times ([aefca39](https://github.com/umbraco/Umbraco.AI/commit/aefca3953e5461473960b02a1a9a13c4a59fb05e)), closes [#709](https://github.com/umbraco/Umbraco.AI/issues/709)

## [18.0.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Moonshot@18.0.1...Umbraco.AI.Moonshot@18.0.2) (2026-11-03)

### Internal

* Bump to align with Umbraco.AI 18.7.0.

## [18.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Moonshot@18.0.0...Umbraco.AI.Moonshot@18.0.1) (2026-10-19)

### Internal

* Bump to align with Umbraco.AI 18.6.0.

## [18.0.0](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Moonshot@18.0.0) (2026-09-14)

### feat

* **moonshot:** Add Moonshot AI provider ([c398172](https://github.com/umbraco/Umbraco.AI/commit/c398172b99ca4d0dbd4b319a05d000fa01ec6d81))

### fix

* **moonshot:** Strip temperature from Kimi requests, only 1.0 is accepted ([134285f](https://github.com/umbraco/Umbraco.AI/commit/134285faf5f78bf07313c5edeaca12e307380bdb))