# Changelog - Umbraco.AI.Moonshot

All notable changes to Umbraco.AI.Moonshot will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.0.3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Moonshot@17.0.2...Umbraco.AI.Moonshot@17.0.3) (2026-11-11)

### fix

* Stop OpenAI SDK providers retrying an out-of-credit reply 3 times ([499943a](https://github.com/umbraco/Umbraco.AI/commit/499943a6a886b068c2b5bf48eefa80a13b8d09b8)), closes [#709](https://github.com/umbraco/Umbraco.AI/issues/709)

## [17.0.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Moonshot@17.0.1...Umbraco.AI.Moonshot@17.0.2) (2026-11-03)

### Internal

* Bump to align with Umbraco.AI 17.7.0.

## [17.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Moonshot@17.0.0...Umbraco.AI.Moonshot@17.0.1) (2026-10-19)

### Internal

* Bump to align with Umbraco.AI 17.6.0.

## [17.0.0](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Moonshot@17.0.0) (2026-09-14)

### feat

* **moonshot:** Add Moonshot AI provider ([f922cf9](https://github.com/umbraco/Umbraco.AI/commit/f922cf9b97beaff9575095889783a2d31d838123)), closes [umbraco/Umbraco.AI#357](https://github.com/umbraco/Umbraco.AI/issues/357)

### fix

* **moonshot:** Strip temperature from Kimi requests, only 1.0 is accepted ([bbd8e34](https://github.com/umbraco/Umbraco.AI/commit/bbd8e3497425bb59c0e5aaadd4675d00964a15f0))