# AGENTS.md — High and Low (project context)

This file is auto-read by coding agents on session start. Read the context files
**in order** before implementing anything or making an architectural decision:

1. `context/project-overview.md` — product definition, goals, features, scope
2. `context/architecture.md` — system structure, boundaries, storage model, invariants
3. `context/ui-context.md` — theme, color tokens, typography, component conventions
4. `context/code-standards.md` — implementation rules and conventions
5. `context/ai-workflow-rules.md` — development workflow, scoping rules, delivery approach
6. `context/progress-tracker.md` — current phase, completed work, open questions, next steps

## Rules

- **Update `context/progress-tracker.md` after every meaningful implementation change.**
- If implementation changes the architecture, scope, or standards documented in the
  context files, update the relevant file before continuing.
- If a requirement is ambiguous or missing, resolve it in the relevant context file
  (or add it as an open question in `progress-tracker.md`) before implementing.
- Do not invent product behavior that is not defined in the context files.
- PR descriptions and commit messages are written in neutral third person
  (what was fixed and why), never first person, never "the assistant did X".
- Commits and PRs must not contain any agent/AI references.
