# Changelog - Umbraco.AI.Agent.Copilot

All notable changes to Umbraco.AI.Agent.Copilot will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [1.0.0-alpha2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@1.0.0-alpha1...Umbraco.AI.Agent.Copilot@1.0.0-alpha2) (2026-02-10)

### feat

* **agent-ui,copilot:** Extract shared chat UI into @umbraco-ai/agent-ui package ([71ecf0c](https://github.com/umbraco/Umbraco.AI/commit/71ecf0cf411c2698534332b728bd8be87029fb42))
* **agent-ui,copilot:** Implement entity context contract and move frontend tool repository ([27a29ba](https://github.com/umbraco/Umbraco.AI/commit/27a29ba089bb79bfa12b6102bcae297c624d439f))
* **copilot:** Add localization keys for frontend tools ([5d2beca](https://github.com/umbraco/Umbraco.AI/commit/5d2becaf7ffe9a097dc03fe3a9819601175f5973))
* **copilot:** Add scope property to tool manifests and update repository ([7994a52](https://github.com/umbraco/Umbraco.AI/commit/7994a52b75e5e19587f9df286575fa333feaa01d))
* **copilot:** Disable chat input when no agents available ([267df0b](https://github.com/umbraco/Umbraco.AI/commit/267df0b6187e8d11635af6b0cf171cdb5fef496b))
* **copilot:** Filter agent repository observable using RxJS operators ([093fc52](https://github.com/umbraco/Umbraco.AI/commit/093fc529dd073b662d78df0c8a58e70ac4ab0b90))
* **copilot:** Subscribe to copilot agent observable in context ([5843cf7](https://github.com/umbraco/Umbraco.AI/commit/5843cf7cc4528c106735d0106cef0aef3398880a))
* **core,agent,copilot:** Add tool metadata flow for scope and destructive permissions ([6e9efbc](https://github.com/umbraco/Umbraco.AI/commit/6e9efbc5cd01b4fb79ec3731a47e4ca64b05efb7))
* **tools:** Add tool permission override components ([c355ded](https://github.com/umbraco/Umbraco.AI/commit/c355dedaa9adcf107c219d0ea25ff0f47898397e))
* **tools:** Add tool scope infrastructure for permission management ([766a28e](https://github.com/umbraco/Umbraco.AI/commit/766a28e4529ba0b7f4c49bc2ce9f32e02600b84b))
* **tools:** Add tool scope picker component ([4ce9270](https://github.com/umbraco/Umbraco.AI/commit/4ce9270f181d83c83b94004d69dbb64fcf109802))

### fix

* **copilot:** Copilot should have dependency on Umbraco.AI.Agent ([a0f1724](https://github.com/umbraco/Umbraco.AI/commit/a0f1724f704dab4d37acb11431f74e840babe27e)), closes [#36](https://github.com/umbraco/Umbraco.AI/issues/36)
* **copilot:** Fix merge conflict resolution issues ([fc76ff9](https://github.com/umbraco/Umbraco.AI/commit/fc76ff94c8660a5d6b6b69b07a478b42e672ee0d))
* **copilot:** Fixed copilot package being too liberal with the umbraco-marketplace tag ([ff7bbf6](https://github.com/umbraco/Umbraco.AI/commit/ff7bbf6bfd46b2d02d50f32cee2c6ae89caa821c))
* **copilot:** Hide agent dropdown when no agents available ([56e927e](https://github.com/umbraco/Umbraco.AI/commit/56e927e9f541ccaabfaa9428cffcf9050f27a1cd))
* **copilot:** Remove duplicate import map entries ([3d53c5d](https://github.com/umbraco/Umbraco.AI/commit/3d53c5da6a5c97d299de374cca097fc1f59aa012))
* **copilot:** Update version to 1.0.0-alpha2 following semver pre-release conventions ([4229407](https://github.com/umbraco/Umbraco.AI/commit/42294075c238c273ff94837482171c12c153f4b5))
* **core,tools:** Fix TypeScript compilation errors ([feae61a](https://github.com/umbraco/Umbraco.AI/commit/feae61a279c8181e189c4206a29375309078cb7c))
* **tools:** Update tool scope override to use item picker modal ([5ca2eaa](https://github.com/umbraco/Umbraco.AI/commit/5ca2eaad8c92bbcd4809b2c5b911e98a6973715e))
## [1.0.0-alpha1] - 2026-02-03

Initial release.

[1.0.0-alpha1]: https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.Agent.Copilot@1.0.0-alpha1
