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