# ADR 0008: NuGet publishing workflow

## Status
Accepted

## Context

The project needs a repeatable and secure process for publishing NuGet packages that supports both preview and stable channels, validates package quality, and keeps package metadata consistent.

## Decision

- Publish only the public package projects in `01 - Packages` (`Hosting`, `Contracts`, `Web.Components`, `Web`).
- Use shared packaging metadata and packaged assets through `Directory.Build.props`.
- Use MIT licensing for package metadata and repository licensing.
- Add `NuGet Validate` workflow for restore/build/test/pack and package requirement checks.
- Add `NuGet Publish` workflow for preview/stable channels with SemVer enforcement and secrets-based publishing.
- Use protected GitHub environments for stable release approval (`nuget-stable`).

## Consequences

- Package metadata is centrally managed and consistent.
- Preview and stable releases are separated and explicit.
- Publication is auditable and does not require local secrets.
- Future package feeds can be added by extending workflow publish targets.
