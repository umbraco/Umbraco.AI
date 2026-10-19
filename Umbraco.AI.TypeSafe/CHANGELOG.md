# Changelog - Umbraco.AI.TypeSafe

All notable changes to Umbraco.AI.TypeSafe will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [17.0.0](https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.TypeSafe@17.0.0) (2026-10-19)

### feat

* **typesafe:** Add the TypeSafe AI provider, connecting Jev for typed yes/no, pick-one and score answers through the experimental Decision capability (requires Umbraco.AI 17.6.0 and the `Umbraco:AI:Experimental:Decision` feature flag) ([6c338c2](https://github.com/umbraco/Umbraco.AI/commit/6c338c247273031ba7677db632729d2789fc0157)), closes [#419](https://github.com/umbraco/Umbraco.AI/issues/419)
* **typesafe:** Send Decision requests to Jev as one batched call ([25ec28d](https://github.com/umbraco/Umbraco.AI/commit/25ec28d27f1f544a3f2efd455a09e7bda1d1ce70))
