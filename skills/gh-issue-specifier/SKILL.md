---
name: gh-issue-specifier
description: Analyze a GitHub issue in depth and produce a detailed specification based on codebase analysis. Use when given a GitHub issue URL or number and asked to deeply analyze requirements, scan the repository, and write a structured spec (no code changes).
---

# Gh Issue Specifier

## Overview

Build a deep, code-informed specification from a GitHub issue using gh CLI and targeted repository analysis. Ask clarifying questions when needed, then deliver a structured, implementation-ready spec.

## Workflow

1) Parse the issue target
- Accept a full GitHub issue URL or an issue number.
- Resolve owner/repo from the URL when provided.
- If only a number is provided, ask which repo to use.

2) Fetch issue details via gh
- Always use `gh issue view <number> --repo <owner>/<repo>`.
- Capture title, body, labels, and comments.

3) Scan the codebase deeply
- Use `rg` for fast discovery (types, references, UI strings, tests, configs).
- Open and read relevant files with `Get-Content`.
- Trace the end-to-end flow: entry points -> services -> domain -> storage -> UI.
- Identify existing validations, error handling, localization, and logging patterns.

4) Identify gaps and impacts
- What is missing relative to the issue requirements?
- Which modules and layers must change?
- What new tests are required and where?
- Identify risk areas and backward compatibility issues.

5) Ask clarifying questions when needed
- Ask only the minimum set of questions to disambiguate scope, UX, or behavior.
- If no questions are needed, proceed directly to the spec.

6) Produce the specification
- Provide a clear, structured spec with concrete behaviors and impacted areas.
- Do not propose code or patches.

## Required Spec Format

Include these sections in order:

- Summary
- Scope (In/Out)
- Functional Requirements
- Non-Functional Requirements
- UX / Messaging / Localization
- Logging / Telemetry
- Edge Cases / Error Handling
- Affected Components (files/areas)
- Test Plan (unit/integration/e2e)
- Migration / Compatibility (if applicable)
- Open Questions
- Risks

## Operational Notes

- Always use `gh issue view` (no browser).
- Always use `rg` for discovery before opening files.
- Prefer short file excerpts and paraphrase; avoid pasting large blocks.
- Keep the spec actionable and grounded in the current codebase.

## Example Trigger Phrases

- "Analyze issue 260 and write a detailed spec"
- "Deeply analyze this GitHub issue and the codebase"
- "Create a specification from https://github.com/<org>/<repo>/issues/<id>"
