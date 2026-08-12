# Changelog - Umbraco.AI.Agent.Copilot

All notable changes to Umbraco.AI.Agent.Copilot will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [18.1.0-rc.4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@18.0.4...Umbraco.AI.Agent.Copilot@18.1.0-rc.4) (2026-08-12)

### feat

* **agent,copilot:** Lock destructive backend tools out of the contextual copilot surface ([7ffa73d](https://github.com/umbraco/Umbraco.AI/commit/7ffa73dd92bf2b25e08b70aaf8502520304b61d7))
* **copilot,agent-ui:** Greet with the item name in the empty chat via an empty-state slot ([30b48b6](https://github.com/umbraco/Umbraco.AI/commit/30b48b65fc60aed2ccb97dac9454f524d7b5f576))
* **copilot,agent-ui:** Refocus the contextual copilot with context framing and per-node history ([36669e8](https://github.com/umbraco/Umbraco.AI/commit/36669e85cd67b6f7c03db0ac58a4d3d19b19a117))
* **copilot:** Add contextual floating chat button in supported workspaces ([4d605f1](https://github.com/umbraco/Umbraco.AI/commit/4d605f1b9ba6d7c3574db30e64f9478f9eac1ce9))
* **copilot:** Make the contextual trigger keyboard and screen-reader accessible ([cc03796](https://github.com/umbraco/Umbraco.AI/commit/cc03796ba77f069fa7ccc4fc7f4a63c976240b72))
* **copilot:** Remember the selected agent across reloads ([b865cfc](https://github.com/umbraco/Umbraco.AI/commit/b865cfcd4d31ca4bcfa8f687632ae76767d93cb2))
* **copilot:** Use the AI sparkles glyph on the contextual copilot button ([dfd8ec1](https://github.com/umbraco/Umbraco.AI/commit/dfd8ec14a316cd2cf00c5447e025c14f900f5d91))

### fix

* **core,copilot:** Persist contextual copilot history by keying on the reactive entity unique ([530c015](https://github.com/umbraco/Umbraco.AI/commit/530c0154f30db645577df004a0d1e820d893ba94))

### refactor

* **agent-ui,copilot:** Rename the empty-state slot to empty-state-message ([7b7445a](https://github.com/umbraco/Umbraco.AI/commit/7b7445a7596686f1908273cbe4b7c1261f23117c))
* **copilot:** Centralise FAB ownership and derive support from adapters ([ff4e7eb](https://github.com/umbraco/Umbraco.AI/commit/ff4e7eb7f745be166c3dfeeaad019a92a5bce232))
* **copilot:** Extract the hide-debounce and ref-count the section observable ([523eb29](https://github.com/umbraco/Umbraco.AI/commit/523eb2908ced5f95845428c65f6326bab28040f7))
* **copilot:** Remove retired header-app section stack and unify trigger visibility ([758cde4](https://github.com/umbraco/Umbraco.AI/commit/758cde4d435f94496594886083574008607537c4))

## [18.1.0-rc.3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@18.0.4...Umbraco.AI.Agent.Copilot@18.1.0-rc.3) (2026-08-11)

### feat

* **agent,copilot:** Lock destructive backend tools out of the contextual copilot surface ([7ffa73d](https://github.com/umbraco/Umbraco.AI/commit/7ffa73dd92bf2b25e08b70aaf8502520304b61d7))
* **copilot,agent-ui:** Greet with the item name in the empty chat via an empty-state slot ([30b48b6](https://github.com/umbraco/Umbraco.AI/commit/30b48b65fc60aed2ccb97dac9454f524d7b5f576))
* **copilot,agent-ui:** Refocus the contextual copilot with context framing and per-node history ([36669e8](https://github.com/umbraco/Umbraco.AI/commit/36669e85cd67b6f7c03db0ac58a4d3d19b19a117))
* **copilot:** Add contextual floating chat button in supported workspaces ([4d605f1](https://github.com/umbraco/Umbraco.AI/commit/4d605f1b9ba6d7c3574db30e64f9478f9eac1ce9))
* **copilot:** Make the contextual trigger keyboard and screen-reader accessible ([cc03796](https://github.com/umbraco/Umbraco.AI/commit/cc03796ba77f069fa7ccc4fc7f4a63c976240b72))
* **copilot:** Use the AI sparkles glyph on the contextual copilot button ([dfd8ec1](https://github.com/umbraco/Umbraco.AI/commit/dfd8ec14a316cd2cf00c5447e025c14f900f5d91))

### fix

* **core,copilot:** Persist contextual copilot history by keying on the reactive entity unique ([530c015](https://github.com/umbraco/Umbraco.AI/commit/530c0154f30db645577df004a0d1e820d893ba94))

### refactor

* **agent-ui,copilot:** Rename the empty-state slot to empty-state-message ([7b7445a](https://github.com/umbraco/Umbraco.AI/commit/7b7445a7596686f1908273cbe4b7c1261f23117c))
* **copilot:** Centralise FAB ownership and derive support from adapters ([ff4e7eb](https://github.com/umbraco/Umbraco.AI/commit/ff4e7eb7f745be166c3dfeeaad019a92a5bce232))
* **copilot:** Extract the hide-debounce and ref-count the section observable ([523eb29](https://github.com/umbraco/Umbraco.AI/commit/523eb2908ced5f95845428c65f6326bab28040f7))
* **copilot:** Remove retired header-app section stack and unify trigger visibility ([758cde4](https://github.com/umbraco/Umbraco.AI/commit/758cde4d435f94496594886083574008607537c4))

## [18.0.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@18.0.1...Umbraco.AI.Agent.Copilot@18.0.2) (2026-07-27)

### fix

* **copilot:** stage invariant/shared properties with null culture (PropertyTypeCultureVarianceMismatch) (#247) ([eac1402](https://github.com/umbraco/Umbraco.AI/commit/eac1402733976135b6d4bc51cccabff605a58185)), closes [#247](https://github.com/umbraco/Umbraco.AI/issues/247)

## [18.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@18.0.0...Umbraco.AI.Agent.Copilot@18.0.1) (2026-07-06)

### fix

* **api,deps:** Regenerate OpenAPI clients and reconcile hey-api version ([1bd31c9](https://github.com/umbraco/Umbraco.AI/commit/1bd31c96f3799c5b844d1f662b58018e9f2bd0e1)), closes [#216](https://github.com/umbraco/Umbraco.AI/issues/216) [#217](https://github.com/umbraco/Umbraco.AI/issues/217)

## [18.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@17.0.0...Umbraco.AI.Agent.Copilot@18.0.0) (2026-06-25)

### Internal

* Bump major version to align with Umbraco CMS v18.

## [17.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@1.0.1...Umbraco.AI.Agent.Copilot@17.0.0) (2026-06-22)

### Internal

* Bump major version to align with Umbraco CMS v17.

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@1.0.0...Umbraco.AI.Agent.Copilot@1.0.1) (2026-06-04)

### Internal

* Bump to align with Umbraco.AI 1.14.0.

## [1.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@1.0.0-alpha7...Umbraco.AI.Agent.Copilot@1.0.0) (2026-05-14)

### feat

* **copilot:** Add property value operation tools ([310194b](https://github.com/umbraco/Umbraco.AI/commit/310194be876ca9658638cd7814a22ed07db75ac7))
* **copilot:** Add save and save_and_publish frontend tools ([2226c70](https://github.com/umbraco/Umbraco.AI/commit/2226c70a95d14a1b1940d0d12b28848026fac99f))
* **copilot:** Provide entity adapter context to copilot tools ([ece257f](https://github.com/umbraco/Umbraco.AI/commit/ece257f1ca2e2bcf9f5ec6f18327f4bd3700940e))
* **core,copilot,deps:** Add schema-aware property tooling for agents ([418847a](https://github.com/umbraco/Umbraco.AI/commit/418847a118dbd15a290906f4cc6dd95d7a95a61a))

### fix

* **copilot:** Hide Auto agent option when only one agent is available ([e4bad2a](https://github.com/umbraco/Umbraco.AI/commit/e4bad2a574c13594aa06dd985d1203f7567274be))
* **copilot:** Spell out replace semantics + read-merge requirement on set_value ([26afa7c](https://github.com/umbraco/Umbraco.AI/commit/26afa7c2d8511938cf3ca38f6b4d2e8e52e251f2))
* **copilot:** Tighten set_value description so Sonnet 4.6 invokes instead of narrating ([10eca8c](https://github.com/umbraco/Umbraco.AI/commit/10eca8c1235e0382b95fcebe6f752d81a7fae8a4))

### Notes

First stable 1.0.0 release.

## [1.0.0-alpha7](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@1.0.0-alpha6...Umbraco.AI.Agent.Copilot@1.0.0-alpha7) (2026-04-30)

### fix

* **copilot:** Bump Umbraco CMS peer dependency minimum to 17.3.0 ([342d3e1](https://github.com/umbraco/Umbraco.AI/commit/342d3e1ff10bf9e22be17b5c441a030e0327d4ab))
* **ui,copilot:** Add missing localization keys for semantic search and set value tools ([b7a527d](https://github.com/umbraco/Umbraco.AI/commit/b7a527defa243059802b4b6e309081a6e29b1a28))

## [1.0.0-alpha6](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@1.0.0-alpha5...Umbraco.AI.Agent.Copilot@1.0.0-alpha6) (2026-04-08)

### fix

* **copilot:** Keep sidebar content visible during close transition ([01c60c8](https://github.com/umbraco/Umbraco.AI/commit/01c60c8a6c1eb130cbebfd97a2511109e0480b42))
* **speechtotext,copilot:** Stop recording when copilot drawer closes ([194f01d](https://github.com/umbraco/Umbraco.AI/commit/194f01d4d493b07463f9a7becc880273b05be9d4))

## [1.0.0-alpha5](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@1.0.0-alpha4...Umbraco.AI.Agent.Copilot@1.0.0-alpha5) (2026-03-16)

### fix

* **core,prompt,agent,copilot:** Add white-space nowrap to all uui-tag elements ([e4e9c04](https://github.com/umbraco/Umbraco.AI/commit/e4e9c04e3cbe7eb1c8782c20915032081d421f55))

## [1.0.0-alpha4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@1.0.0-alpha3...Umbraco.AI.Agent.Copilot@1.0.0-alpha4) (2026-03-04)

## [1.0.0-alpha3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.Agent.Copilot@1.0.0-alpha2...Umbraco.AI.Agent.Copilot@1.0.0-alpha3) (2026-02-17)

### feat

* **agent,copilot:** Add surface context contributor and frontend integration ([017aca1](https://github.com/umbraco/Umbraco.AI/commit/017aca1cb889a068035a771cd765360e03ed33e3))
* **copilot:** Add auto mode with synthetic Auto agent ([3a681df](https://github.com/umbraco/Umbraco.AI/commit/3a681df936544367ae79ab589eb9c2287f100d23))
* **copilot:** Add dynamic section compatibility system ([58b8af9](https://github.com/umbraco/Umbraco.AI/commit/58b8af9897f1ce5e35d3968bb74ec773de4a2387))
* **copilot:** Always show Auto option in agent dropdown ([32c3a9a](https://github.com/umbraco/Umbraco.AI/commit/32c3a9a2be23b456dadfdd8228d2505ce2b1667d))
* **copilot:** Improve context-aware filtering with reactive observables ([a6305fe](https://github.com/umbraco/Umbraco.AI/commit/a6305fe18c0139b2912f5f33fa1ec2babe1645be))
* **copilot:** Send section context to backend for tool filtering ([1a90ac0](https://github.com/umbraco/Umbraco.AI/commit/1a90ac08312590a26143c2a9b332b72764cbbf03))
* **core,copilot:** Add manifest-driven request context contributor system ([ec3f985](https://github.com/umbraco/Umbraco.AI/commit/ec3f98503ed98b96e86f3a6c78f9320922eccb3b))

### fix

* **copilot:** Add missing section-compatibility.ts file ([d96d84c](https://github.com/umbraco/Umbraco.AI/commit/d96d84c38af4d5a3ea8f79802692166aca9d666a))
* **copilot:** Ensure agent list renders immediately on page load ([185b9cd](https://github.com/umbraco/Umbraco.AI/commit/185b9cd5a2e33eb936f109f26e1199906d9046e1))
* **copilot:** Fix section-detector import paths ([c39633c](https://github.com/umbraco/Umbraco.AI/commit/c39633cea8709ec14764c7423f7fd89ac91fc57b))
* **copilot:** Prevent history API corruption with multiple section observers ([5450850](https://github.com/umbraco/Umbraco.AI/commit/545085058ad474dbf0b6712f3bc2121ff6221c15))
* **core,agent,prompt,copilot:** Add client ready promises to prevent race conditions ([8b961db](https://github.com/umbraco/Umbraco.AI/commit/8b961dbf5c0c8e74198772c6ff44e570238e7c1d))
* **tools:** Filter non-numeric issue references from changelogs ([5e70577](https://github.com/umbraco/Umbraco.AI/commit/5e70577235083b4b488c0512ba1736aaf83f7775)), closes [#36](https://github.com/umbraco/Umbraco.AI/issues/36) [#33](https://github.com/umbraco/Umbraco.AI/issues/33)

### refactor

* **agent,copilot:** Rename context dimensions to scope dimensions ([a888be0](https://github.com/umbraco/Umbraco.AI/commit/a888be09565460ad1b41f3ace9fce3f696e9c948))
* **copilot:** Consolidate section compatibility types into base types.ts ([b9d9067](https://github.com/umbraco/Umbraco.AI/commit/b9d906753b26bccd6859b251d97f8aa74ea66521))
* **copilot:** Rename section-detector to context-observer ([edd7eec](https://github.com/umbraco/Umbraco.AI/commit/edd7eecfc2a310ed3ddd50f1a56f7b0051ba30ad))
* **core,agent-ui,copilot:** Extract surface contributor into reusable kind ([7e2100c](https://github.com/umbraco/Umbraco.AI/commit/7e2100c5c3a69f81b71988378fc6997f4beaa5c5))
* **core,agent-ui,copilot:** Rename surface kind to agentSurface ([c578ef3](https://github.com/umbraco/Umbraco.AI/commit/c578ef3aa9a88f925241b2ea811dc83563d19a5c))
* **core,agent,copilot:** Rename SectionAlias to Section ([353e167](https://github.com/umbraco/Umbraco.AI/commit/353e1670fdcdc09f88c9055d81d7b688b693bccc))

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
