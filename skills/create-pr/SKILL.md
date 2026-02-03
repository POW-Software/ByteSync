---
name: create-pr
description: Push the current branch and create a Pull Request for ByteSync. Use when the user wants the agent to push a branch and open a PR, and requires the PR title and description in English.
---

# Request PR

## Overview

Provide a short workflow to push the current branch and open a Pull Request with an English title and description using the `gh` CLI.

## Workflow

### 1) Confirm branch state

Confirm the current branch name and whether there are uncommitted changes. If changes remain, ask whether to commit before pushing.

### 2) Push branch

Push the current branch to the remote. If the user did not specify a tool, default to using the `gh` CLI context and run a normal git push for the current branch.

Example:
```bash
git push -u origin HEAD
```

### 3) Create PR

Create a Pull Request using `gh pr create` with:

- English title
- English description summarizing changes and approach

### 4) Provide PR text template

Provide a minimal PR template to pass to `gh pr create`:

Title: `[type] Short summary`

Description:
- Summary:
- Key changes:
- Notes/risks:

Ensure the template is in English and matches the repo's PR format when applicable.

### 5) Keep it brief

Keep the response focused on the push + PR actions, avoiding extra steps unless the user asks.
