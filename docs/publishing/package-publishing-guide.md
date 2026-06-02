# Package Publishing Guide

## Versioning strategy

This repository uses Semantic Versioning (SemVer):

- Stable: `x.y.z`
- Preview: `x.y.z-preview.n`

## Preview release process

Preview publishing is supported from:

- Preview tags (for example `v1.2.0-preview.1`)
- `release-candidate/*` branches (version derived as `<branchVersion>-preview.<run_number>`)
- Manual workflow execution (`NuGet Publish`) with `release_type=preview`

## Stable release process

Stable publishing is supported from:

- Release tags (for example `v1.2.0`)
- Manual workflow execution (`NuGet Publish`) with `release_type=stable`

The stable publish job targets the `nuget-stable` environment so maintainers can require explicit approval reviewers in repository settings.

## Approval requirements

- Configure required reviewers on `nuget-stable`.
- Keep write access limited to maintainers.
- Do not share or commit API keys.

## Rollback strategy

NuGet packages are immutable. Rollback is handled by:

1. Unlisting the problematic package version on NuGet.org.
2. Publishing a patched version with an incremented SemVer.
3. Updating release notes and issue tracking with the replacement version.

## Troubleshooting

- **Missing `NUGET_API_KEY` secret**: publish workflow fails fast before push.
- **Version format rejected**: ensure stable uses `x.y.z` and preview uses `x.y.z-preview.n`.
- **Package validation failed**: review workflow logs for missing metadata, README, icon, or license entries.
- **Symbol package missing**: confirm `.snupkg` artifacts were generated during pack.

## Security and secret handling

- `NUGET_API_KEY` is supplied via GitHub Actions secrets only.
- Publishing only happens in approved workflows.
- No credentials are stored in source control.
