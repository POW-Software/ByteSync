## Project Overview

**ByteSync** is an open-source file synchronization software with end-to-end encryption and smart delta transfers, supporting up to 5 remote locations per session.

This is a C#/.NET 8 solution with a client-server architecture:

### Main Projects
- **`ByteSync.Client/`** - Cross-platform desktop app (Avalonia UI, ReactiveUI/MVVM)
- **`ByteSync.Functions/`** - Azure Functions serverless backend (HTTP APIs, SignalR hub)
- **`ByteSync.Common/`** - Shared business logic and domain models
- **`ByteSync.ServerCommon/`** - Server-side services and data repositories

### Key Technologies
- **Frontend**: Avalonia UI, ReactiveUI
- **Backend**: Azure Functions, SignalR, MediatR (CQRS)
- **Storage**: Azure Cosmos DB, Redis
- **Sync**: FastRsync for binary diffs

## Branch & PR Guidelines

### Overview
This repository uses a branch-based development workflow. Please follow the conventions below when working in this project, especially when creating branches, writing PR titles, and generating commits or documentation.
Agents must not create branches until explicitly instructed to do so by a human.

### Branch Naming
Use the following prefixes:
- `feature/<name>` for new features
- `fix/<name>` for bug fixes
- `refactor/<name>` for refactorings
- `docs/<name>` for refactorings
- `test/<name>` for tests

Avoid slashes other than the prefix.

### PR Titles
Format:
`[<type>] <Short summary>`

Allowed types: `feature`, `fix`, `refactor`, `docs`, `test`

Examples:
- `[feature] Add user registration`
- `[bugfix] Fix memory leak in sync module`

### Notes for Agents
- Never propose changes directly on `master`.
- Always create a branch using the proper prefix.
- PR titles must follow the documented format.

## Commenting Guidelines
- All comments must be written in English.
- Do not add comments or Javadoc/XML-style documentation in C# code.
- Comments are only allowed when strictly necessary to explain complex or non-obvious logic.
- Prefer clear and self-explanatory code over adding comments.

## Build and Test Guidelines
- Always run build and test as two separate commands to avoid blocking issues.
- Use `dotnet build --verbosity quiet /property:WarningLevel=0` to build the solution.
- When running tests, do not use the `--verbosity` modifier.
- If you need to clean the solution, use `dotnet clean --verbosity quiet` before building.