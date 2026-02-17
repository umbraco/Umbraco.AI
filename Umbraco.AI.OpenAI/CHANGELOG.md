# Changelog - Umbraco.AI.OpenAI

All notable changes to Umbraco.AI.OpenAI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
