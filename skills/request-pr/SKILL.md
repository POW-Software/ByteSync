---
name: request-pr
description: Request pushing the current branch and creating a Pull Request for ByteSync. Use when the user wants to push a branch and open a PR, and requires the PR title and description in English.
---

# Request PR

## Overview

Provide a short workflow to ask for pushing the current branch and opening a Pull Request with an English title and description.

## Workflow

### 1) Confirm branch state

Confirm the current branch name and whether there are uncommitted changes. If changes remain, ask whether to commit before pushing.

### 2) Push branch

Request the user to push the current branch to the remote.

### 3) Create PR

Request creation of a Pull Request with:

- English title
- English description summarizing changes and approach

### 4) Provide PR text template

Provide a minimal PR template the user can paste:

Title: `[type] Short summary`

Description:
- Summary:
- Key changes:
- Notes/risks:

Ensure the template is in English and matches the repo's PR format when applicable.

### 5) Keep it brief

Keep the response focused on the push + PR request, avoiding extra steps unless the user asks.
