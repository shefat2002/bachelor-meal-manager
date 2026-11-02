# API Conventions

**Binding contract for every endpoint in this project** — backend implementations and frontend types (`types/api.ts`) mirror this document 1:1. It is authored in Phase 0 (P0-DX-1); changes require a Decisions Log entry.

Base URL: the ASP.NET Core API (Aspire AppHost fixes it at `http://localhost:5080` in dev; the frontend reads `NEXT_PUBLIC_API_BASE_URL`). All routes are prefixed `/api`. JSON only, `camelCase` everywhere (field names, query params, enums).

## 1. Authentication & tenancy headers

| Header | Required | Notes |
|---|---|---|
| `Authorization: Bearer <accessToken>` | All authenticated routes (Phase 1+) | Access tokens are short-lived; refresh via `POST /api/auth/refresh` |
| `X-Mess-Id: <guid>` | All tenant-scoped routes | The **only** tenant selector. Resolved server-side before any handler runs (middleware/pipeline: header + membership check). Client-supplied `messId` in a body/DTO is never trusted |

Missing/invalid token → `401`. Valid token but not a member of the `X-Mess-Id` mess (or missing header on a tenant-scoped route) → `403` / `400` respectively (see §4).

## 2. Success envelope

- **Single resource**: the resource DTO directly.

  ```json
  { "id": "…", "name": "Mess A", "createdAt": "2026-11-02T10:00:00Z" }
  ```

- **Create (`POST`)**: `201 Created` + `Location: /api/<resource>/{id}` header + the created DTO.
- **Update (`PUT`)**: `200 OK` + the updated DTO. **Delete (`DELETE`)**: `204 No Content`.
- **Lists**: always paginated — `PaginatedResult<T>`:

  ```json
  {
    "items": [ … ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 134
  }
  ```

## 3. List query parameters (every list `GET`)

| Param | Default | Rules |
|---|---|---|
| `page` | `1` | ≥ 1 |
| `pageSize` | `20` | 1–100 (larger → `400` VALIDATION_ERROR) |
| `sortBy` | resource default | Whitelist per endpoint (below); unknown value → `400` |
| `sortDir` | `desc` | `asc` \| `desc` |
| `from`, `to` | — | Inclusive date range, `YYYY-MM-DD` (ISO 8601); `from ≤ to` |

Sort whitelists: meals → `date`; bazar expenses → `date`, `price` (line total = quantity × unit price); utility payments → `date` (payment date); deposits → `date`, `amount`. Utility types list is name-ordered (no `sortBy`).

Additional per-endpoint filters (beyond `from`/`to`): meals → `memberId`; bazar → `buyerMemberId`; utility payments → `utilityTypeId`; deposits → `memberId`, `status` (`Pending` | `Approved` | `Rejected`).

## 4. Error envelope

Every non-2xx response is `ApiErrorResponse` — ProblemDetails-shaped, machine-checkable:

```json
{
  "type": "VALIDATION_ERROR",
  "title": "One or more validation failures occurred",
  "status": 400,
  "detail": "Optional human summary",
  "errors": {
    "pageSize": ["'pageSize' must be between 1 and 100."],
    "items[0].price": ["'price' must be greater than 0."]
  },
  "traceId": "00-abc…-01"
}
```

`type` is the stable error code (frontend switches on it, never on `title`); `errors` maps **field path → messages** (FluentValidation pipeline output, camelCased paths).

### Error codes → HTTP status

| `type` | HTTP | When |
|---|---|---|
| `VALIDATION_ERROR` | 400 | FluentValidation failure (field-level `errors` present) |
| `TENANT_REQUIRED` | 400 | Tenant-scoped route called without `X-Mess-Id` |
| `UNAUTHENTICATED` | 401 | Missing/expired/invalid token (client: try refresh, else redirect to login) |
| `FORBIDDEN` | 403 | Authenticated but role/membership check failed |
| `NOT_FOUND` | 404 | Resource doesn't exist **or** exists in another tenant (no leakage) |
| `CONFLICT` | 409 | Version/state conflicts (e.g. editing a locked month, duplicate join) |
| `RATE_LIMITED` | 429 | Rate limit exceeded (Phase 5) |
| `INTERNAL_ERROR` | 500 | Unhandled — logged with `traceId`; body carries no internals |

## 5. Data type rules

- **Dates**: `YYYY-MM-DD` (`DateOnly`). **Timestamps**: ISO 8601 UTC (`2026-11-02T10:00:00Z`).
- **Money**: JSON numbers backed by `decimal(18,2)` — never floats client-side (frontend renders with `.num` class / `Intl.NumberFormat`).
- **Meal counts**: numbers in `0.5` steps (`decimal(4,1)` backend), guests included.
- **IDs**: GUID strings.
- **Enums**: lowerCamelCase strings (`"mealManager"`, `"member"`).

## 6. Endpoint catalogue (matches `docs/BACKEND_PROJECT_PLAN.md` §6)

Auth (Phase 1): `POST /api/auth/login | refresh | logout`.
Messes: `POST/GET /api/messes`, `PUT/DELETE /api/messes/{id}`.
Members: `POST /api/members/join | invite`, `PUT /api/members/{id}/role | approve | reject`, `DELETE /api/members/{id}` (remove; self = leave).
Meals (Phase 2): `POST /api/meals` — **bulk upsert** of one date's entries (`{date, entries:[{memberId, breakfast, lunch, dinner}]}`) → `200 OK` + resulting rows (upsert, not a pure create; re-submitting a member/date overwrites). Members may only submit their own `memberId`; managers anyone. `GET /api/meals`.
Bazar (Phase 2): `POST /api/expenses/bazar` — one row per item sharing `{date, buyerMemberId?}`; buyer defaults to the caller; manager-only. `GET /api/expenses/bazar`.
Utility types (Phase 2): `POST/GET /api/utility-types`, `PUT/DELETE /api/utility-types/{id}` — manager-only; duplicate name per mess → `409`; delete of a type with payments → `409` (deactivate via `PUT isActive=false` instead).
Utility payments (Phase 2): `POST/GET /api/expenses/utility` — manager-only writes, payments must reference a type of the same mess.
Deposits (Phase 2): `POST /api/deposits` (member → own deposit, starts `Pending`; manager → any member, auto-`Approved`), `GET /api/deposits`, `PUT /api/deposits/{id}/approve | reject` — manager-only, only `Pending` deposits transition (else `409`). **Month lock**: while a mess-month is locked, every write dated in it (`POST /api/meals`, bazar/utility/deposit creates, deposit approval) → `409 CONFLICT`.
Summary (Phase 3): `GET /api/summary/messes/{messId}?year=&month=` — the §5 equations, grain-computed and cached; the path `messId` **must equal** the `X-Mess-Id` tenant (else `404`); any active member may read; `year`/`month` default to the current UTC month; locked months serve the snapshot captured at lock time (`locked: true`). Month lock: `PUT /api/summary/messes/{messId}/month-lock` body `{year, month, locked}` — manager-only, `204`.

## 7. Non-goals / later additions

- No API versioning yet (single `/api` prefix). Revisit before the first breaking change.
- Rate limiting (Phase 5), ETags/caching, and HATEOAS links are deliberately absent now.
- Filtering beyond `from`/`to` (e.g. `buyerId=`) is added per-endpoint with this doc updated in the same PR.
