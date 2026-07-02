# Security Policy

## Supported Versions

OakIdeas.Aspire.DataExplorer is a **development-time-only** Aspire add-on. It is not designed or supported for production deployment. Security fixes are maintained on the current release line only.

| Version | Supported |
|---------|-----------|
| Latest  | ✅        |
| Older   | ❌        |

## Development-Only Boundary

This library enforces development-only guardrails at runtime:

- The web component throws on startup outside the `Development` environment.
- The hosting extension throws on startup outside the `Development` environment.
- Connection strings and credentials are never exposed to client-side code.

Do not attempt to bypass these guards in non-development environments.

## Reporting a Vulnerability

If you discover a security vulnerability in OakIdeas.Aspire.DataExplorer, please **do not open a public GitHub issue**.

Instead, report it privately using [GitHub's private vulnerability reporting](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing/privately-reporting-a-security-vulnerability) on this repository.

Please include:

- A description of the vulnerability and its potential impact
- Steps to reproduce or a proof-of-concept
- The affected version(s)

You can expect an acknowledgement within a few business days. We will work with you to understand, validate, and address the issue before any public disclosure.

## Scope

Given the development-only nature of this tool, the following are generally **out of scope**:

- Vulnerabilities only exploitable with elevated local developer access
- Vulnerabilities only present in non-`Development` environments (since startup is blocked there)

Please use good judgment when determining whether a finding is in scope. When in doubt, report privately.
