---
name: bmm-cqrs-mediatr
description: CQRS API implementation for Bachelor Meal Manager using MediatR — thin controllers dispatching Commands/Queries, FluentValidation pipeline behavior running before handler execution, RESTful endpoint design with filtering/sorting/pagination, and the shared response envelope. Use when adding or changing any API endpoint, command, query, validator, or controller in the backend.
---

# CQRS & Endpoint Logic (MediatR + FluentValidation)

Specs: `backend/AGENTS.md` (Architecture, API specs), `docs/BACKEND_PROJECT_PLAN.md` §6, root `AGENTS.md` §4.

## Structure per feature (e.g. `Features/Meals/`)

```
Features/Meals/
├── MealsController.cs        # thin: route → MediatR.Send → result
├── Commands/
│   ├── SubmitDailyMealsCommand.cs      + Handler + Validator
├── Queries/
│   ├── GetDailyMealsQuery.cs           + Handler (+ Validator if params need it)
└── MealsContracts.cs         # DTOs / response models
```

Rules:
- **Controllers contain no logic** — bind, `Send`, map result. Anything smarter belongs in a handler.
- Commands mutate (POST/PUT/DELETE), queries read (GET). Never a handler that does both.
- Handlers receive already-validated, tenant-scoped requests. **`MessId` resolution happens before the handler** (from `X-Mess-Id` header/route + membership check middleware or a MediatR pipeline behavior) — handlers must not trust client-supplied tenant ids.

## Validation pipeline

FluentValidation runs as a MediatR pipeline behavior **before** handler execution; invalid requests short-circuit to `400` with field-level errors:

```csharp
public class SubmitDailyMealsValidator : AbstractValidator<SubmitDailyMealsCommand>
{
    public SubmitDailyMealsValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Entries).NotEmpty().WithMessage("At least one meal entry required");
        RuleForEach(x => x.Entries).ChildRules(e =>
        {
            e.RuleFor(x => x.Breakfast).InclusiveBetween(0, 10);
            e.RuleFor(x => x.Lunch).InclusiveBetween(0, 10);
            e.RuleFor(x => x.Dinner).InclusiveBetween(0, 10);
        });
    }
}
```

Frontend Zod schemas mirror these rules 1:1 (skill `bmm-forms-validation`) — when you change a validator, note it in the log so the frontend skill/schema gets updated too.

## Endpoint conventions

- RESTful routes per plan §6: `POST/GET /api/meals`, `POST/GET /api/expenses/bazar`, `POST/GET /api/expenses/utility`, `GET /api/summary/messes/{messId}`, messes/members CRUD.
- Every list GET supports **filtering, sorting, pagination**: `?page=&pageSize=&sortBy=&sortDir=&from=&to=` returning `PaginatedResult<T> { items, page, pageSize, totalCount }` (envelope defined in `docs/API_CONVENTIONS.md`, Phase 0).
- Authorization via role policies (`SuperAdmin`, `MealManager`, `Member` — mess-scoped checks combine role + membership).
- On write paths that affect money (meals/expenses/deposits): handler persists via EF Core (`bmm-efcore-multitenancy`), then updates the grain (`bmm-orleans-grains`) / publishes events (`bmm-rabbitmq-workers`), then invalidates Redis summary cache.

## Testing

Unit-test each handler (validator short-circuit + happy path + authz failure) and add integration tests per endpoint (routing, persistence, tenant isolation).
