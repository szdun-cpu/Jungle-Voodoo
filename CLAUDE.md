# CLAUDE.md

This file provides guidance for AI assistants (Claude and others) working in this repository.

## Repository Status

This repository (`szdun-cpu/Jungle-Voodoo`) is currently empty and awaiting initial project setup. This CLAUDE.md will be updated as the project evolves.

## Development Branch

Active development should target the branch specified at the start of each session. Check your session context for the designated branch before making any commits or pushes. **Never push directly to `main` or `master` without explicit instruction.**

## Git Conventions

- Use clear, descriptive commit messages in the imperative mood (e.g., "Add user authentication", not "Added" or "Adding")
- Commit logically related changes together; avoid mixing unrelated changes in one commit
- Always use `git push -u origin <branch-name>` when pushing a new branch
- Do not amend published commits; create a new commit instead

## Working in an Empty Repository

Since no source code exists yet:

1. **Do not invent project structure** — wait for the user to specify the language, framework, and architecture
2. **Ask before scaffolding** — if asked to scaffold a project, confirm the stack (language, framework, test runner, linter) before generating files
3. **Keep the first commit minimal** — prefer a focused initial commit (e.g., just package files + this CLAUDE.md) over large scaffolded dumps

## General AI Assistant Guidelines

### Do
- Read files before editing them
- Prefer editing existing files over creating new ones
- Make only the changes requested; do not refactor surrounding code
- Confirm before taking destructive or irreversible actions (force-push, file deletion, dropping data)
- Validate only at system boundaries (user input, external APIs); trust internal framework guarantees

### Do Not
- Add features, error handling, or abstractions beyond what is explicitly requested
- Add comments or docstrings to code you did not change
- Use `--no-verify` to skip hooks or bypass safety checks
- Push to any branch other than the one designated for the current session

## Updating This File

When the project gains source code, update this file to include:

- **Project overview** — what the project does and its primary language/framework
- **Directory structure** — annotated map of key directories
- **Setup instructions** — how to install dependencies and run the project locally
- **Development workflow** — branch strategy, PR process, code review expectations
- **Testing** — how to run tests, required coverage thresholds, testing conventions
- **Linting & formatting** — tools used, how to run them, CI enforcement
- **Environment variables** — required variables and how to configure them (reference `.env.example`)
- **Key conventions** — naming patterns, architectural decisions, patterns to follow or avoid
- **CI/CD** — pipeline overview and what must pass before merging
