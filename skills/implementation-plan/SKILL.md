---
name: implementation-plan
description: Produce a structured implementation plan (no code) from an existing specification or deep analysis. Use after a spec is written to outline steps, file touch points, sequencing, and test strategy without writing code.
---

# Implementation Plan

## Overview

Transform an existing specification into a clear, ordered implementation plan with concrete steps, affected areas, and test execution sequence. Do not write code.

## Workflow

1) Confirm inputs
- Ensure a specification already exists in the current context.
- If missing, ask for the spec or a link to it.

2) Identify workstreams
- Break the spec into discrete steps: core logic, validation, UI, persistence, integration, tests, tooling.
- Map each step to files/areas referenced in the spec.

3) Order the steps
- Prefer foundational utilities first, then integrations, then UI, then tests.
- Highlight prerequisites and dependencies between steps.

4) Define test strategy
- List the test layers to update (unit/integration/e2e).
- Specify the target projects and key scenarios.
- Include build/test command order when relevant.

5) Call out risks and checkpoints
- Note migration or compatibility concerns.
- Identify risky changes that warrant extra verification.

## Output Format

- Title
- Assumptions
- Step-by-step plan (numbered)
- Files/Areas to touch (bulleted)
- Test plan (bulleted)
- Risks/Checkpoints

## Constraints

- Do not generate code.
- Ask questions only if the specification is missing or ambiguous.
- Keep plan concise but actionable.
