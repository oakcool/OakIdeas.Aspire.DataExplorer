# Changelog

All notable changes to this project will be documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) conventions.
This project uses [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `CODE_OF_CONDUCT.md` — community participation standards
- `SECURITY.md` — vulnerability reporting policy and development-only boundary guidance
- `.editorconfig` — consistent editor formatting rules for C#, Razor, JSON, TypeScript, CSS, and Markdown
- `ROADMAP.md` — current priorities, near-term, and medium-term goals
- `.github/PULL_REQUEST_TEMPLATE.md` — standard pull request template
- `.github/ISSUE_TEMPLATE/bug-report.yml` — structured bug report template
- `.github/ISSUE_TEMPLATE/feature-request.yml` — structured feature request template
- `ObjectDefinitionRequest` / `ObjectDefinitionResponse` contracts and `IObjectDefinitionProvider` interface for unified object definition retrieval
- `SqlServerDatabaseProvider` implementation of `IObjectDefinitionProvider` covering views, procedures, functions, triggers, and indexes
- `Index` (`7`) added to `DatabaseObjectType` enum

### Changed
- Consolidated public-facing documentation and removed temporary development artifacts ahead of public release.
