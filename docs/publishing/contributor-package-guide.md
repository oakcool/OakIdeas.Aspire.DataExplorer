# Contributor Package Guide

## Package metadata requirements

Public package projects must define:

- Package Id
- Title
- Description
- Authors
- Company
- Repository URL and type
- Package tags
- Project URL
- Package icon
- Copyright
- Release notes
- License information

## README requirements

NuGet package README content is sourced from `docs/nuget/package-readme.md` and packed as `README.md`.

## Licensing requirements

This repository uses the MIT License (`LICENSE`).

Rationale:

- Widely recognized permissive open-source license
- Enables broad use and modification
- Preserves attribution through the required notice

## Release notes expectations

Release notes are tracked through GitHub releases and linked through package metadata.

## Package publishing workflow

Use GitHub Actions workflows:

- `NuGet Validate` for build/test/pack and package requirement checks
- `NuGet Publish` for preview and stable publishing

Stable releases must use an approved environment configuration (`nuget-stable`) before publishing.
