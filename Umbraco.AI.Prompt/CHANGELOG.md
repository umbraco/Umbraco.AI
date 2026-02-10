# Changelog - Umbraco.AI.Prompt

All notable changes to Umbraco.AI.Prompt will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Prompt@1.0.0...Umbraco.AI.Prompt@1.1.0) (2026-02-10)

### feat

* **core,agent,prompt:** Add alias existence API endpoints ([f48d7e9](https://github.com/umbraco/Umbraco.AI/commit/f48d7e9eb8218fee27adbb8bec00ed74c0c897ff))
* **core,agent,prompt:** Add visual error indicators for duplicate alias validation ([cdc56f0](https://github.com/umbraco/Umbraco.AI/commit/cdc56f0ea9f394a6acbbe7e8f43cd12bfbee4a65))
* **prompt,agent:** Add required validation to instructions fields ([c4716d2](https://github.com/umbraco/Umbraco.AI/commit/c4716d234900fdca6088447e7b9b2bc5ee16c6c6))
* **prompt:** Add getPromptById method to data source ([e91f532](https://github.com/umbraco/Umbraco.AI/commit/e91f532bae70376d239d3c3aef869cdaeceb2b55))
* **prompt:** Add PromptManifestEntry interface ([9e91ed6](https://github.com/umbraco/Umbraco.AI/commit/9e91ed6f521037980b4248c8a253a28fe75dfad9))
* **prompt:** Add validation to Prompt workspace ([313d1bd](https://github.com/umbraco/Umbraco.AI/commit/313d1bde2f74e85d2e2302ca73027bd2f3948b3c))

### fix

* **agent, prompt, openai:** fix validation states for required backoffice fields (#33) ([4c31e25](https://github.com/umbraco/Umbraco.AI/commit/4c31e255a68aa281787feb760d172586599cff3c)), closes [#33](https://github.com/umbraco/Umbraco.AI/issues/33)
* **core,agent,prompt:** Add checkValidity() to trigger visual validation state ([b1dc012](https://github.com/umbraco/Umbraco.AI/commit/b1dc0129791129e28becc7b8ff9165d72e628000))
* **core,agent,prompt:** Escape hyphen in regex pattern ([49092af](https://github.com/umbraco/Umbraco.AI/commit/49092afef2ce95239bbc33cd748df9d5fdb37fbd))
* **core,agent,prompt:** Fix validation blocking by calling checkValidity() method ([1766901](https://github.com/umbraco/Umbraco.AI/commit/1766901e5e308235aed721bdbfc90d84e3c07fd5))
* **core,agent,prompt:** Fix validation issues from code review ([5984276](https://github.com/umbraco/Umbraco.AI/commit/5984276e463b7dccd625edf1181c4c950b6555a7))
* **core,agent,prompt:** Properly block save on duplicate alias via validation messages ([908d1c8](https://github.com/umbraco/Umbraco.AI/commit/908d1c8592755e08a88e92ef0e246b0783479da5))
* **core,agent,prompt:** Use setCustomValidity for visual error display ([346f0e8](https://github.com/umbraco/Umbraco.AI/commit/346f0e8ad04776c0b39c67d39695ce00962c7520)), closes [#checkAliasUniqueness](https://github.com/umbraco/Umbraco.AI/issues/checkAliasUniqueness)
* **prompt:** Fix TypeScript compilation errors ([f12955a](https://github.com/umbraco/Umbraco.AI/commit/f12955a06e998d3adb61fbcf449f1da668809728))
* **prompt:** Fixed prompt package being too liberal with the umbraco-marketplace tag ([93280bc](https://github.com/umbraco/Umbraco.AI/commit/93280bcb1f0fd6c0a413aaf1aca684dc56e6125f))
* **prompt:** Prevent registrar from being garbage collected ([84947cd](https://github.com/umbraco/Umbraco.AI/commit/84947cd9a7542dc1f491c63ea06dca39fe4d071d))
* **prompt:** Properly unregister deleted prompts from registry ([74894a0](https://github.com/umbraco/Umbraco.AI/commit/74894a01d7ff5dad6c9fd3f3e111bc28feb85069))

### refactor

* **agent,prompt:** Standardize alias pattern to lowercase only ([297513c](https://github.com/umbraco/Umbraco.AI/commit/297513c66d05cbf2516950086a281a8c7dac35cc))
* **prompt:** Implement observable state in repository ([136eebb](https://github.com/umbraco/Umbraco.AI/commit/136eebb8d666dbbfd88cbb669cfadd6421f170f4))
* **prompt:** Simplify controller to thin sync layer ([a5adbf6](https://github.com/umbraco/Umbraco.AI/commit/a5adbf6dbd49dce3d2fcc59248c9421600b115d1))
* **prompt:** Simplify registration model construction ([b262251](https://github.com/umbraco/Umbraco.AI/commit/b262251d83f73b63c9d94362d05c96c0418b11c5))

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Prompt@1.0.0...Umbraco.AI.Prompt@1.0.1) (2026-02-04)

### Bug Fixes

* **prompt:** Fixed prompt package being too liberal with the umbraco-marketplace tag ([93280bc](https://github.com/umbraco/Umbraco.AI/commit/93280bcb1f0fd6c0a413aaf1aca684dc56e6125f))

## [1.0.0] - 2026-02-03

Initial release.

[1.0.0]: https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Prompt@1.0.0
