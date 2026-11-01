# Task Logs

One markdown file per completed unit of task. This is how work is handed off between AI agent sessions without re-explaining the project — a new agent reads the checklist for *state* and these logs for *detail*.

## File naming

```
YYYY-MM-DD-<TASK-ID>-<short-kebab-slug>.md
```

Example: `2026-11-02-P1-BE-3-mess-crud-endpoints.md`

## Template

```markdown
# <TASK-ID>: <Task title>

- **Date**: YYYY-MM-DD
- **Agent**: <agent/session identifier>
- **Phase / track**: e.g. Phase 1 / Backend

## What was built
<2–5 sentences. What exists now that didn't before.>

## Files touched
- <path> — <what changed>

## Key decisions & why
- <decision> — <reason; anything a future agent would otherwise re-litigate>

## How to verify / test
<commands to run, endpoints to hit, screens to check>

## Follow-ups / known gaps
- <anything left for later tasks, with task ID if known>
```

## Rules

- Written **after** the task's checklist entry is ticked; the log is the detail, the checklist is the state.
- Keep each log self-contained — a future agent should not need the originating conversation.
- Never edit or delete existing logs; supersede with a new log if decisions change.
