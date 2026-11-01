---
name: bmm-efcore-multitenancy
description: Data access and multi-tenant isolation for Bachelor Meal Manager — Entity Framework Core with Npgsql/PostgreSQL, the canonical entity model (Messes, Members, MembersMesses, DailyMeals, BazarExpenses, UtilityTypes, UtilityPayments, Deposits), global query filters scoped by MessId, migrations, and tenant-isolation testing. Use when adding entities, writing DbContext or repository code, migrations, or any database query.
---

# Data Access & Multi-Tenant Scoping (EF Core + PostgreSQL)

Specs: `docs/BACKEND_PROJECT_PLAN.md` §3, `backend/AGENTS.md`. **Tenant key is `MessId`** (root `AGENTS.md` §4 — older docs saying `HostelId` are legacy wording; never introduce it).

## Canonical entities

| Entity | Key points |
|---|---|
| `Mess` | Tenant root. Id, name, settings. Created by Super Admin (creator becomes Meal Manager) |
| `Member` | User identity: name, phone number (ASP.NET Identity user) |
| `MemberMess` | M2M members↔messes: role in mess, join date, active/left status |
| `DailyMeal` | MemberId, MessId, Date, Breakfast, Lunch, Dinner (decimal — 0.5 steps + guest meals) |
| `BazarExpense` | MessId, ItemName, Price, Date, BuyerMemberId |
| `UtilityType` | Per-mess custom types (Electricity, Wi-Fi, Cook Salary) — manager-defined |
| `UtilityPayment` | MessId, UtilityTypeId, Amount, PaymentDate |
| `Deposit` | MessId, MemberId, Amount, Date, ApprovedBy (manager approval flow) |

## Global query filters — the isolation backbone

Every tenant-owned entity has a `MessId` FK, and `ApplicationContext` applies a **global query filter** so no query can ever cross tenants:

```csharp
public class ApplicationContext : DbContext
{
    private readonly ITenantContext _tenant; // resolved from X-Mess-Id + membership check

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<DailyMeal>().HasQueryFilter(e => e.MessId == _tenant.MessId);
        b.Entity<BazarExpense>().HasQueryFilter(e => e.MessId == _tenant.MessId);
        // ... every tenant-owned entity
    }
}
```

Rules:
- Tenant context comes from the validated `X-Mess-Id` header/route + membership check in the request pipeline (see skill `bmm-cqrs-mediatr`) — **never** from a client-supplied DTO field.
- `IgnoreQueryFilters()` is forbidden outside explicitly-audited admin (Super Admin) paths.
- Indexes: composite `(MessId, Date)` on DailyMeal/BazarExpense, `(MessId, Month)` on summary snapshots — all hot queries are tenant+date scoped.
- Money columns: `decimal(18,2)`; meal counts `decimal(4,1)` (half meals); dates as `DateOnly`.

## Migrations & repositories

- Npgsql provider; migrations in the Persistence project; `dotnet ef migrations add <Feature_Name>` — one migration per PR-sized change, reviewed like code.
- Repos wrap `DbContext` per aggregate; no direct `DbContext` in handlers beyond queries via dedicated read models when projection is needed (CQRS read side may use raw SQL/Dapper for report queries).

## Tenant-isolation testing (Definition of Done)

An integration test per entity: seed mess A + mess B with data, query as a member of A, assert zero B rows ever return — including list endpoints, filters, and pagination. This suite is the project's most important regression test.
