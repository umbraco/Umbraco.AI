# Changelog - Umbraco.AI.FireworksAI

All notable changes to Umbraco.AI.FireworksAI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-04-24

Initial release.

- Chat completions via Fireworks AI's OpenAI-compatible endpoint
- Text embeddings via Fireworks AI's OpenAI-compatible endpoint
- Dynamic model discovery: chat and embedding models are listed automatically from Fireworks' native models API, with classification driven by Fireworks' own metadata (no hardcoded model lists)
- Configurable account id (defaults to the public `fireworks` catalog; override to expose fine-tuned or private models)
- Configurable endpoint for proxies or custom deployments

[1.0.0]: https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI.FireworksAI@1.0.0
