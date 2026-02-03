---
name: perform-implement-plan
description: Execute an implementation plan end-to-end in the ByteSync repo. Use when the user asks to implement a previously established plan step by step, requires creating a branch before coding, expects regular commits during progress, and wants build/test validation.
---

# Plan Implementation

## Overview

Implement a previously agreed plan in ByteSync, one step at a time, while enforcing branch creation and frequent commits.

## Workflow

### 1) Confirm plan scope

Restate the implementation plan in brief. If the plan is not present in the conversation, ask for it before starting.

### 2) Create a branch before implementation

Create a new branch before making any code changes. The branch name must follow ByteSync guidelines:

- Use one prefix only: `feature/`, `fix/`, `refactor/`, `docs/`, or `test/`
- Avoid extra slashes beyond the prefix
- Include the issue number if an issue exists (e.g., `feature/1234-add-sync-filter`)
- Add a short English description

### 3) Implement step by step

Implement the plan in small, ordered steps. After each step:

- Verify the change fits the plan
- Commit the work with a concise English message
- Keep commits focused and incremental

### 4) Respect repo conventions and validation

Follow the repo guidance (AGENTS.md), including:

- Do not add unnecessary comments
- Use FluentAssertions for tests
- Build and test as separate commands
- During intermediate commits, run only tests related to the changes when reasonable
- At the end, run the full solution tests

### 5) Keep the user informed

Provide short progress updates after each step and call out any blockers or missing details before proceeding.
