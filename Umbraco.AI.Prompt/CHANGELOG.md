# Changelog - Umbraco.AI.Prompt

All notable changes to Umbraco.AI.Prompt will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Prompt@1.1.0...Umbraco.AI.Prompt@1.2.0) (2026-02-17)

### feat

* **core,prompt,agent:** Add persistent menu highlighting for entity containers ([dd9cf3a](https://github.com/umbraco/Umbraco.AI/commit/dd9cf3a6aac91278a8003befc5c7719e4c0e7fd9))
* **prompt:** Add AIPrompt lifecycle notifications and service integration ([89690d7](https://github.com/umbraco/Umbraco.AI/commit/89690d7030e5a91530c9b4d439d3e036094dfafe))
* **prompt:** Add OptionCount property to domain model and database ([920fc29](https://github.com/umbraco/Umbraco.AI/commit/920fc296c3868eb541b5ee6669993f391e152c2a))
* **prompt:** Add OptionCount to API models, mappings, and validation ([f7ad317](https://github.com/umbraco/Umbraco.AI/commit/f7ad317e01dac5322c0df1047932c37571d13968))
* **prompt:** Add optionCount to frontend detail model and mappers ([af952e5](https://github.com/umbraco/Umbraco.AI/commit/af952e556fdb78de57e37d6b993a94df419cc186))
* **prompt:** Add prompt execution notifications ([9a26c2f](https://github.com/umbraco/Umbraco.AI/commit/9a26c2f7a84293fb596719ea77e925ad8641ef6f))
* **prompt:** Add Result Type dropdown with conditional option count input ([6bf8215](https://github.com/umbraco/Umbraco.AI/commit/6bf821541ed5c71357cbb9b085c07a527c2562de))
* **prompt:** Add ResultOptions with unified structure for execution results ([0830c91](https://github.com/umbraco/Umbraco.AI/commit/0830c916f16846802ba9c946f5c4d1cc6232cfc8))
* **prompt:** Add sync icon to retry button in preview modal ([67c584c](https://github.com/umbraco/Umbraco.AI/commit/67c584cc5ba3d34e142f2e657bf2fd4fc876ca16))
* **prompt:** Add TypeScript types for ResultOptions ([131a05d](https://github.com/umbraco/Umbraco.AI/commit/131a05d91f35d16929a4201587bd30a68335e81b))
* **prompt:** Create dedicated Availability workspace view ([3017ca7](https://github.com/umbraco/Umbraco.AI/commit/3017ca77afd4a2b5835b33a7a0ef6c7287930073))
* **prompt:** Implement frontend for OptionCount and ResultOptions ([4d287b3](https://github.com/umbraco/Umbraco.AI/commit/4d287b37f1be574aa60a3dac0249f6ed134fe7e4))
* **prompt:** Implement OptionCount execution logic with retry ([1d24a8d](https://github.com/umbraco/Umbraco.AI/commit/1d24a8d54c11f45485657a2a0681bf9953b7c4a3))

### fix

* **core,agent,prompt,copilot:** Add client ready promises to prevent race conditions ([8b961db](https://github.com/umbraco/Umbraco.AI/commit/8b961dbf5c0c8e74198772c6ff44e570238e7c1d))
* **core,agent,prompt:** Migrate authorization to custom AI section ([c5b4503](https://github.com/umbraco/Umbraco.AI/commit/c5b4503031fa5612f636409cc876659e4632fe82))
* **core,prompt,agent:** Use IEventAggregator instead of INotificationPublisher for Umbraco v17 ([0385280](https://github.com/umbraco/Umbraco.AI/commit/03852809bb696a4eaeaf8150f3e30deb6f9377bb))
* **prompt:** Add OptionCount to entity factory mappings ([1fe6658](https://github.com/umbraco/Umbraco.AI/commit/1fe665887d0fa3e561c3efcc52698a7838b63ea1))
* **prompt:** Fix string interpolation syntax in format instructions ([5fb8e87](https://github.com/umbraco/Umbraco.AI/commit/5fb8e87a888a21c7f951459a8e846a54b39cd5ad))
* **prompt:** Include error details in prompt execution failures ([0c8ba29](https://github.com/umbraco/Umbraco.AI/commit/0c8ba293f7142fea447ce440978dce9e73bf0b6e))
* **prompt:** Standardize prompt scope dimension labels ([810125e](https://github.com/umbraco/Umbraco.AI/commit/810125e255217915c6ed340133c9db956777d6f8))
* **prompt:** Update frontend types to use resultOptions instead of valueChanges ([af661dc](https://github.com/umbraco/Umbraco.AI/commit/af661dce6559f7321f79c6860807c0c3fd265e19))
* **prompt:** Use correct uui-select pattern with .options property ([1ede13e](https://github.com/umbraco/Umbraco.AI/commit/1ede13edbb0b122b3712b1cfaee3b01dbaec9527))

### refactor

* **core,prompt:** Rename PropertyChange to ValueChange ([bcc58fd](https://github.com/umbraco/Umbraco.AI/commit/bcc58fdc44178839d78ec4daf7f1a52828827720))
* **prompt,agent:** Rename scope rule components for consistency ([9115f0e](https://github.com/umbraco/Umbraco.AI/commit/9115f0eebc35cf06611d3792bad921d67038d6fb))
* **prompt:** Enhance preview modal with improved copy UX and layout ([16e990e](https://github.com/umbraco/Umbraco.AI/commit/16e990e3d914b5e4a2838ec7529a618e1646d41f))
* **prompt:** Polish preview modal UI and layout ([4df8d77](https://github.com/umbraco/Umbraco.AI/commit/4df8d7775b2b5ac9f9dd6357d3e7ff1093beb780))
* **prompt:** Remove scope description from availability view ([2bd692f](https://github.com/umbraco/Umbraco.AI/commit/2bd692f806195810adb01bdc3368969c9e9c36fb))
* **prompt:** Remove unused copied state from preview modal ([ff82757](https://github.com/umbraco/Umbraco.AI/commit/ff82757ff50e6390b8715b9ed6db0d4568dd451e))

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
* **core,agent,prompt:** Use setCustomValidity for visual error display ([346f0e8](https://github.com/umbraco/Umbraco.AI/commit/346f0e8ad04776c0b39c67d39695ce00962c7520))
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
