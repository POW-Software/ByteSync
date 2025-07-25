## Project Overview

This is a C#/.NET solution using .NET 8.

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
- On Windows :
  - Use `& "C:\Program Files\PowerShell\7\pwsh.exe" -NoProfile -Command "dotnet build --verbosity minimal /property:WarningLevel=0 > build_output.txt 2>&1"` to build the solution and capture all output including errors (recommended to avoid blocking issues).
  - Alternative: Use `dotnet build --verbosity minimal /property:WarningLevel=0 > build_output.txt 2>&1` with PowerShell 5.
  - To view compilation errors: `Get-Content build_output.txt -Head 20`
  - To count compilation errors: `Get-Content build_output.txt | findstr "error CS" | Measure-Object -Line`
- When running tests, do not use the `--verbosity` modifier.
- If you need to clean the solution, use `dotnet clean --verbosity quiet` before building.