# Arcane Security Review (July 2026)

> **Source:** Based on the Arcane Security Review performed on
> **2026-07-14**. This document is an original Markdown adaptation
> intended for repository documentation and issue tracking.

## Overview

The review evaluated the development-time SQL database explorer with
emphasis on the SQL execution surface, development-only protections,
authentication boundaries, configuration, dependencies, secrets
handling, injection risks, and general security posture.

### Review Summary

  Item                Value
  ------------------- ----------------------------------------
  Target              OakIdeas.Aspire.DataExplorer
  Review Date         2026-07-14
  Method              Multi-agent security review
  Scope               \~21k lines of C#, 57 Razor components
  Findings Reviewed   33
  Refuted             26
  Confirmed           5
  Critical            0
  High                0

## Executive Summary

The review concluded that the project has a strong overall security
posture. The primary architectural boundary---restricting the explorer
to development environments---is enforced in multiple independent
locations and fails closed by default.

The remaining findings are defense-in-depth improvements rather than
architectural flaws. They should be treated as hardening opportunities.

------------------------------------------------------------------------

# Confirmed Findings

## F-01 -- URL Driven Query Auto Execution

**Severity:** Medium

### Summary

The query page allows SQL and an auto-execution flag to be supplied
through URL parameters, allowing SQL execution after navigation without
explicit user interaction.

### Potential Impact

-   Drive-by execution
-   Destructive SQL could execute if additional protections are bypassed
-   Limited by development-only protections

### Recommended Direction

-   Remove automatic execution from URL parameters, or
-   Require explicit user interaction before execution
-   If retained, require same-site validation and additional
    protections.

------------------------------------------------------------------------

## F-02 -- RequireLocalConnections Is Not Enforced

**Severity:** Low

### Summary

A configuration option exists suggesting only local database connections
are allowed, but the review found no runtime enforcement.

### Potential Impact

Could create a false sense of safety by allowing remote database
connections.

### Recommended Direction

Either:

-   Enforce the option during connection validation, or
-   Remove the configuration option.

------------------------------------------------------------------------

## F-03 -- Write Detection Relies on First SQL Token

**Severity:** Low

### Summary

Write detection relies on inspecting only the first SQL token.

### Potential Impact

Alternative SQL constructs may bypass this heuristic.

### Recommended Direction

Prefer server-side read-only enforcement, least-privilege accounts, or
rollback-only transactions rather than token inspection.

------------------------------------------------------------------------

## F-04 -- Identifier Escaping

**Severity:** Low

### Summary

Generated query templates should properly escape database identifiers
and quoted strings.

### Potential Impact

A specially crafted database object name could produce unsafe generated
scripts.

### Recommended Direction

Escape identifiers correctly or use SQL Server's `QUOTENAME`.

------------------------------------------------------------------------

## F-05 -- Mermaid Loaded From CDN

**Severity:** Low

### Summary

Mermaid is loaded from a CDN without integrity validation.

### Potential Impact

Introduces an unnecessary supply-chain dependency.

### Recommended Direction

-   Bundle Mermaid locally, or
-   Pin an exact version
-   Use Subresource Integrity (SRI)
-   Consider an appropriate Content Security Policy.

------------------------------------------------------------------------

# Security Strengths

The review highlighted several strengths:

-   Multiple independent development-only enforcement points
-   Fail-closed startup behavior
-   Architecture documented through ADRs
-   Server-side feature gates
-   Parameterized metadata queries
-   Secrets remain server-side
-   Proper output encoding
-   Strong dependency management and package hygiene

------------------------------------------------------------------------

# Recommended Follow-up

1.  Revalidate every finding against the current codebase.
2.  Determine whether each finding still exists.
3.  Create remediation plans only for confirmed findings.
4.  Implement fixes individually.
5.  Build and test after every change.
6.  Add regression tests where necessary.
7.  Update documentation.
8.  Do not close the work until every confirmed finding has been
    resolved or documented as no longer applicable.
