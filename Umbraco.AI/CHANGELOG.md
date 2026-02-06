# Changelog - Umbraco.AI

All notable changes to Umbraco.AI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1](https://github.com/umbraco/Umbraco.AI/compare/Umbraco.AI@1.0.0...Umbraco.AI@1.0.1) (2026-02-04)

### ⚠ BREAKING CHANGE

* **core:** Removes the DetailLevel setting from audit logs and changes
default persistence behavior.

- Remove AIAuditLogDetailLevel enum (was never checked in code)
- Remove DetailLevel from AIAuditLog model and AIAuditLogEntity
- Remove DetailLevel from API response models
- Change PersistPrompts default from false to true
- Change PersistResponses default from false to true
- Keep PersistFailureDetails default as true
- Create database migrations to drop DetailLevel column (SQL Server & SQLite)
- Update docs to reference PersistPrompts/PersistResponses

The three boolean flags provide clearer, more flexible control than abstract
detail levels. Users can still disable any persistence option in
appsettings.json if needed for privacy.

### feat

* add missing properties to uaiProfile ([701c738](https://github.com/umbraco/Umbraco.AI/commit/701c7383cc26e1fdc7c2d81c7bb17d0797435b54))

### fix

* **core:** Fixed core package being too liberal with the umbraco-marketplace tag ([5c660a0](https://github.com/umbraco/Umbraco.AI/commit/5c660a07e6352df18265eac43f52bf47ea660cb9))

### refactor

* **core:** Remove unused DetailLevel setting and set PersistX defaults to true ([d9c8a75](https://github.com/umbraco/Umbraco.AI/commit/d9c8a757756a52d4e07a330ef143af239999e74a))

## [1.0.0] - 2026-02-03

Initial release.

[1.0.0]: https://github.com/umbraco/Umbraco.AI/releases/tag/Umbraco.AI@1.0.0
