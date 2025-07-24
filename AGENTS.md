## Project Overview

This is a C#/.NET solution using .NET 8. Use `dotnet` commands for building and testing:
- `dotnet build` - Build the solution
- `dotnet test` - Run all tests
- `dotnet restore` - Restore dependencies

## Branch & PR Guidelines

### Overview
This repository uses a branch-based development workflow. Please follow the conventions below when working in this project, especially when creating branches, writing PR titles, and generating commits or documentation.

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