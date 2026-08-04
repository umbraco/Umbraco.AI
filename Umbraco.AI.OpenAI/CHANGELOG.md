# Changelog - Umbraco.AI.OpenAI

All notable changes to Umbraco.AI.OpenAI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.2.0-rc.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@17.1.2...Umbraco.AI.OpenAI@17.2.0-rc.1) (2026-08-04)

### ⚠ BREAKING CHANGE

* **core,frontend,openai:** `AIImageGenerationProfileSettings.Quality` and `.Style` are removed, along
with their
API model properties and editor fields. Quality and Style now live under the provider's own
settings
on the profile editor. Values stored under the old shape are ignored and need re-entering
there.
Image generation is experimental — gated by `UMBRACOAI_IMAGEGEN` and the
`Umbraco:AI:Experimental:ImageGeneration` flag — so this ships without an obsolete shim.

No data migration: the target keys belong to a provider's schema, and a core migration
writing
provider-specific keys would put vendor knowledge in core, which is the coupling this whole
design

### feat

* **core,frontend,openai:** Drive the image size field from the model's declared sizes (#280) ([371cb53](https://github.com/umbraco/Umbraco.AI/commit/371cb5335ba4b53127e54bca26e0b0de55e3588f)), closes [#280](https://github.com/umbraco/Umbraco.AI/issues/280)
* **core,frontend,openai:** Move image quality and style to capability settings (#282) ([b7f462a](https://github.com/umbraco/Umbraco.AI/commit/b7f462a3ce01190727dedf809738cafc0274127a)), closes [#282](https://github.com/umbraco/Umbraco.AI/issues/282) [#275](https://github.com/umbraco/Umbraco.AI/issues/275) [#277](https://github.com/umbraco/Umbraco.AI/issues/277)
* **core,openai,anthropic,amazon,frontend:** Enforce per-model declarations in core [v17 backport] (#284) ([6c6ff0e](https://github.com/umbraco/Umbraco.AI/commit/6c6ff0effa14b232b0126e7957c1a08d9ab2f49e)), closes [#284](https://github.com/umbraco/Umbraco.AI/issues/284)
* **core,openai,anthropic:** Add provider-declared, model-aware capability settings (#270) ([a86199d](https://github.com/umbraco/Umbraco.AI/commit/a86199da8612ca03939ec7297934177f201861ef)), closes [#270](https://github.com/umbraco/Umbraco.AI/issues/270) [#269](https://github.com/umbraco/Umbraco.AI/issues/269)

### fix

* **core,anthropic,openai:** Read the cached input tokens adapters already report (v17 backport) ([40e0f34](https://github.com/umbraco/Umbraco.AI/commit/40e0f3443114bda9cd1082468df554d8e686b65e)), closes [#291](https://github.com/umbraco/Umbraco.AI/issues/291)
* **core,frontend,openai,anthropic,amazon:** Give temperature a real unset state (#274) ([38de95d](https://github.com/umbraco/Umbraco.AI/commit/38de95db9f4c29f702f122faa868e24c5cafa93b)), closes [#274](https://github.com/umbraco/Umbraco.AI/issues/274) [#256](https://github.com/umbraco/Umbraco.AI/issues/256) [#269](https://github.com/umbraco/Umbraco.AI/issues/269) [#266](https://github.com/umbraco/Umbraco.AI/issues/266)
* **openai,imagegeneration:** Send the image quality and style hints (#278) ([978a0f3](https://github.com/umbraco/Umbraco.AI/commit/978a0f34949501eba35ae6d15ccffe4a7a37d3f7)), closes [#278](https://github.com/umbraco/Umbraco.AI/issues/278)

## [17.1.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@17.1.1...Umbraco.AI.OpenAI@17.1.2) (2026-07-28)

### fix

* **anthropic,openai,amazon:** Drop sampling parameters on models that reject them (#267) ([6066b04](https://github.com/umbraco/Umbraco.AI/commit/6066b040ba1d6beb491c5daf04466dc4dcfa5c00)), closes [#267](https://github.com/umbraco/Umbraco.AI/issues/267)

## [17.1.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@17.1.0...Umbraco.AI.OpenAI@17.1.1) (2026-07-27)

### Internal

* Bump to align with Umbraco.AI 17.2.0.

## [17.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@17.0.0...Umbraco.AI.OpenAI@17.1.0) (2026-07-06)

### feat

* **imagegeneration:** Add experimental image generation capability ([33f9cb1](https://github.com/umbraco/Umbraco.AI/commit/33f9cb1ecc4c46485266095ff9484d492523a5e7)), closes [#195](https://github.com/umbraco/Umbraco.AI/issues/195)

## [17.0.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.2.6...Umbraco.AI.OpenAI@17.0.0) (2026-06-22)

### Internal

* Bump major version to align with Umbraco CMS v17.

## [1.2.6](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.2.5...Umbraco.AI.OpenAI@1.2.6) (2026-06-04)

### Internal

* Bump to align with Umbraco.AI 1.14.0.

## [1.2.5](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.2.4...Umbraco.AI.OpenAI@1.2.5) (2026-06-01)

### Internal

* Bump to align with Umbraco.AI 1.13.0.

## [1.2.4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.2.2...Umbraco.AI.OpenAI@1.2.4) (2026-05-20)

### Internal

* Bump to align with Umbraco.AI 1.12.0.

## [1.2.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.2.0...Umbraco.AI.OpenAI@1.2.2) (2026-05-14)

### fix

* **openai,microsoft-foundry:** Adapt to OpenAI 2.10 GetResponsesClient API change ([0a50abc](https://github.com/umbraco/Umbraco.AI/commit/0a50abcd9a76f7c2e8c48b93638ec8a779a0017e))

### Internal

* Bump `Microsoft.Extensions.AI.OpenAI` minimum to 10.6.0.

## [1.2.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.1.3...Umbraco.AI.OpenAI@1.2.0) (2026-04-08)

### feat

* **openai:** Add OpenAI speech-to-text capability ([d0bccf5](https://github.com/umbraco/Umbraco.AI/commit/d0bccf59948e21255a22e7e06727b2024c1f965c))

### fix

* **core,openai,anthropic,google,microsoft-foundry:** Fix graders and update provider packages ([087a132](https://github.com/umbraco/Umbraco.AI/commit/087a1327d18412e64502c0ac780a8b4b2343cbec))

## [1.1.4](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.1.3...Umbraco.AI.OpenAI@1.1.4) (2026-03-16)

### fix

* **core,openai,anthropic,google,microsoft-foundry:** Fix graders and update provider packages ([087a132](https://github.com/umbraco/Umbraco.AI/commit/087a1327d18412e64502c0ac780a8b4b2343cbec))

## [1.1.3](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.1.2...Umbraco.AI.OpenAI@1.1.3) (2026-03-04)

## [1.1.2](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.1.1...Umbraco.AI.OpenAI@1.1.2) (2026-03-02)

## [1.1.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.1.0...Umbraco.AI.OpenAI@1.1.1) (2026-02-17)

### build

* **openai,anthropic,google,microsoft-foundry,amazon:** Add version updates to umbraco-package.json ([46038a4](https://github.com/umbraco/Umbraco.AI/commit/46038a48f0e36c21f2fa50407466f96caec08f41))
* **openai:** Regenerate package lock file ([51844a4](https://github.com/umbraco/Umbraco.AI/commit/51844a4ade566c6ae8a8eaeb1a3d77c92fa81a10))

### fix

* **openai:** Fixed incorrectly API usage for Responses API ([8482f91](https://github.com/umbraco/Umbraco.AI/commit/8482f9147e8070ec74eaae303764605097ab2f42))
* **openai:** Migrate to Responses API for GPT-4o compatibility ([cecad4b](https://github.com/umbraco/Umbraco.AI/commit/cecad4bb68f1f91f60a9114654971807c370eba4)), closes [#50](https://github.com/umbraco/Umbraco.AI/issues/50)

## [1.1.0](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.0.0...Umbraco.AI.OpenAI@1.1.0) (2026-02-10)

### fix

* **agent, prompt, openai:** fix validation states for required backoffice fields (#33) ([4c31e25](https://github.com/umbraco/Umbraco.AI/commit/4c31e255a68aa281787feb760d172586599cff3c)), closes [#33](https://github.com/umbraco/Umbraco.AI/issues/33)

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI.OpenAI@1.0.0...Umbraco.AI.OpenAI@1.0.1) (2026-02-04)

### chore

* **ci:** Add umbraco-marketplace tags to provider packages ([ad8021d](https://github.com/umbraco/Umbraco.AI/commit/ad8021d0e2cd66d25e71d8fef9515f32f85fcf6c))

## [1.0.0](https://github.com/umbraco/Umbraco.AI/compare/...Umbraco.AI.OpenAI@1.0.0) (2026-01-28)

* Disable NBGV build number updates ([0a91e46](https://github.com/umbraco/Umbraco.AI/commit/0a91e46ab8c51b2022244d8f0a5839890ab76a39))
* Align NBGV release refs to shared branches ([d85d468](https://github.com/umbraco/Umbraco.AI/commit/d85d468900fb2321102cc9589a7783efe0e2b0f6))
* Add encryption support for sensitive AIFields using Data Protection API ([2aef55f](https://github.com/umbraco/Umbraco.AI/commit/2aef55f047784a0f2bd74f6d04565742e1ab1bd7))
* Update all package versions to 1.0.0 ([a40144b](https://github.com/umbraco/Umbraco.AI/commit/a40144bee41aafa361a17bce6d7736e7ac4fddb3))
* Add Umbraco Marketplace metadata and readme files ([e1ec6e2](https://github.com/umbraco/Umbraco.AI/commit/e1ec6e2b982c15f5490b0e5fa79337d8b23c08b8))
* Enforce committing the wwwroot folder in providers ([1f8af6e](https://github.com/umbraco/Umbraco.AI/commit/1f8af6e797350463bb407f587d1d65f40ba386cd))
* We don't need to use ConfigureAwait in .NET Core ([e20ab65](https://github.com/umbraco/Umbraco.AI/commit/e20ab65cdcc478a6c6ea41f3e37b08da3609cc96))
* We don't need to use ConfigureAwait in .NET Core ([b228625](https://github.com/umbraco/Umbraco.AI/commit/b228625b3d6cb09f07968fa4a35603b32eeaf337))
* We don't need to use ConfigureAwait in .NET Core ([707c8a8](https://github.com/umbraco/Umbraco.AI/commit/707c8a8b2d0882ec594672c56c8bb19d53f1bccb))
* Add safe web fetch tool for URL content extraction ([677f4c4](https://github.com/umbraco/Umbraco.AI/commit/677f4c4c7dec1d2b36ad38c83a8e2cd364e2cab7))
* Refactor background jobs to use RecurringHostedServiceBase ([18381bf](https://github.com/umbraco/Umbraco.AI/commit/18381bfca1939fcc3fb2f8a5376e522eb11f6524))

### feat

* add conditional project/package references for monorepo ([a1151a3](https://github.com/umbraco/Umbraco.AI/commit/a1151a399c097e461d37e757826f2b2cc3753755))

### fix

* add missing packages to Directory.Packages.props ([aa8e7fa](https://github.com/umbraco/Umbraco.AI/commit/aa8e7fad2bfdf08d36a4bd8213af203452342992))
