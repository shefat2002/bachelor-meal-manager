# P0-DX-2: Hostel → Mess wording normalization

- **Date**: 2026-11-02
- **Agent**: ZCode (GLM-5.3)
- **Phase / track**: Phase 0 / Docs

## What was built

All legacy Hostel/HostelId/Hostels terminology normalized to Mess/MessId/Messes/MembersMesses in the two docs the task named, plus the now-stale "older docs say HostelId" pointers in root `AGENTS.md` updated to past tense.

## Files touched

- `docs/BACKEND_PROJECT_OVERVIEW.md` — 10 spots: overview prose ("single hostel mess…hostels" → "single mess (shared kitchen)"), `HostelId` → `MessId` (×2), Hostels Table → Messes Table, MembersHostels → MembersMesses, "per hostel"/"specific hostels"/"from hostels"/"hostel details"/"Members or Hostels" → mess wording
- `docs/SKILL_BUILDER.md` (backend section) — 2 spots: "grains for members and hostels" → messes; "scope data queries by `HostelId`" → `MessId`
- Root `AGENTS.md` — repo-map note "(uses older Hostel wording — see §4)" removed; §4 canonical decision now reads "normalized 2026-11-02 (P0-DX-2)"

## Key decisions & why

- **Intentional mentions left alone**: definitional prose ("shared kitchens in hostels/bachelor apartments" in PROJECT_PHASES, root AGENTS §1), the checklist's own task text/history rows, the DoD rule, and the `bmm-efcore-multitenancy` guard sentence — these *describe* the legacy wording rather than use it.

## How to verify / test

`grep -rn -i hostel docs/ *.md` → only the intentional mentions above remain (logs excluded by protocol).

## Follow-ups / known gaps

- None — the DoD check "no HostelId terminology introduced" now guards against regressions.
