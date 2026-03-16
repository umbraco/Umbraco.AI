# Changelog - Umbraco.AI.Deploy

All notable changes to Umbraco.AI.Deploy will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0-beta2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Deploy@1.0.0-beta1...Umbraco.AI.Deploy@1.0.0-beta2) (2026-03-16)

### feat

* **core,deploy:** Add guardrail Deploy support across all Deploy projects ([0527502](https://github.com/umbraco/Umbraco.AI/commit/052750206e2cecf613c686688c6d879edfd4fcf3))
* **core,deploy:** Add guardrail persistence, notifications, versioning, and tests ([c08f9ff](https://github.com/umbraco/Umbraco.AI/commit/c08f9ffd6eb6b5ec77e7e09d214d4cef40857326))

## [1.0.0-beta1](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Deploy@1.0.0-beta1) (2026-03-02)

### build

* **deploy:** Rename deploy add-on packages to follow ecosystem convention ([d8bcb98](https://github.com/umbraco/Umbraco.AI/commit/d8bcb98204f27e1da2340d397d0fbb3188098c2f))

### feat

* **core,deploy:** Add Deploy support for AISettings ([5b16415](https://github.com/umbraco/Umbraco.AI/commit/5b164152cee40ca7a2485a737417148f562c3d5a))
* **deploy,deploy-prompt,deploy-agent:** Add package metadata and documentation ([25fca21](https://github.com/umbraco/Umbraco.AI/commit/25fca21c2e075829d55d01dea4b41a909d9a193b))
* **deploy:** Add components for explicit RegisterDiskEntityType registration ([89880a4](https://github.com/umbraco/Umbraco.AI/commit/89880a4c275b024e10fbed89a14a5106a82b3f4d))

### fix

* **deploy:** Change default IgnoreSensitive to false ([c53ca96](https://github.com/umbraco/Umbraco.AI/commit/c53ca96ffae897728e0e5beae39b513f7377eb85))
* **deploy:** Fix settings accessor mockability and encrypted value filtering ([e93148f](https://github.com/umbraco/Umbraco.AI/commit/e93148f04bcbfc7fb24ff17bf98a827e6be2829e))
* **deploy:** Restore missing artifact models excluded by gitignore ([68b4ac8](https://github.com/umbraco/Umbraco.AI/commit/68b4ac8bf42d32820c551d7f8cb539d09f198a64))

### refactor

* **core,deploy:** Introduce IAIEntity interface to reduce boilerplate ([7b08de1](https://github.com/umbraco/Umbraco.AI/commit/7b08de15fb3faca38a2137902f6b2b2e263fc987))
* **core,deploy:** Remove deletion notifications for AISettings ([674c893](https://github.com/umbraco/Umbraco.AI/commit/674c89385b2079f9e564f7cc3f05bf579d5b2cdf))
* **deploy:** Improve code style and solution structure ([9dc55f3](https://github.com/umbraco/Umbraco.AI/commit/9dc55f3a05843f11f23b2f3402d8f7fe913705a2))
* **deploy:** Remove environment-specific metadata from artifacts ([82d309e](https://github.com/umbraco/Umbraco.AI/commit/82d309eb3eb63da82e7e4a885e038f4687e0bed8))
* **deploy:** Simplify Profile and Settings deployment dependency resolution ([17ec432](https://github.com/umbraco/Umbraco.AI/commit/17ec43267e235a3c67279228c2f883fb593e1c47))
* **deploy:** Simplify profile-dependent entity deployment to single pass ([59be793](https://github.com/umbraco/Umbraco.AI/commit/59be793c2496d38f71ab25cc7795d92f6f5c1613))
* **deploy:** Use user group aliases instead of GUIDs for agent permissions ([8804321](https://github.com/umbraco/Umbraco.AI/commit/88043215ce5142373561a49697a16db8cf32dccc))
