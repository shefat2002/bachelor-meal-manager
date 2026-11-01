# Frontend Architecture Document: Universal Meal Manager (Web)

## 1. Core Technology Stack
* **Framework**: Next.js (App Router)
* **Language**: TypeScript (Strict Mode)
* **Styling**: Tailwind CSS + Shadcn UI (Radix Primitives)
* **State Management**:
  * **Server State & Caching**: TanStack Query (React Query)
  * **Client State**: Zustand (for UI states, modal workflows, temporary local toggles)
* **Form Handling & Validation**: React Hook Form + Zod (aligned with backend FluentValidation rules)
* **HTTP Client**: Axios with centralized request/response interceptors for JWT injection and refresh logic
* **Data Visualization**: Recharts (for monthly expenditure, meal rate trends, balance distribution)

---

## 2. Directory & Modular Architecture
A domain-driven feature structure inside the `src/` directory ensures code maintainability and strict boundary isolation:


src/
├── app/                          # Next.js App Router (Routes & Layouts)
│   ├── (auth)/                   # Authentication route group (Login, Verify)
│   │   ├── login/
│   │   └── layout.tsx
│   ├── (dashboard)/              # Authenticated application workspace
│   │   ├── layout.tsx            # Injects Tenant/Mess selector & navigation
│   │   ├── [messId]/             # Tenant-scoped route boundary
│   │   │   ├── meals/            # Daily meal logging & entry
│   │   │   ├── expenses/         # Bazar & utility management
│   │   │   ├── members/          # Member directory, roles & invites
│   │   │   ├── reports/          # Financial summaries, meal rates
│   │   │   └── settings/         # UtilityType configs, mess rules
│   │   └── page.tsx              # Tenant redirect/selector dashboard
│   ├── layout.tsx                # Global providers (QueryClient, AuthProvider)
│   └── page.tsx                  # Public landing / redirection
│
├── components/                   # Shared UI primitives
│   ├── ui/                       # Base components (Button, Dialog, Dropdown, Table)
│   ├── feedback/                 # Toasts, skeletons, error boundaries
│   └── navigation/               # Navbar, Sidebar, TenantSwitcher
│
├── features/                     # Domain modules (Feature-driven)
│   ├── meals/
│   │   ├── components/           # MealGrid, MealCounterInput, MealHistoryTable
│   │   ├── hooks/                # useDailyMeals, useUpdateMeals
│   │   └── services/             # mealApi.ts
│   │   └── types/                # meal.types.ts
│   ├── expenses/
│   │   ├── components/           # BazarExpenseForm, UtilityPaymentList
│   │   ├── hooks/                # useExpenses, useCreateBazarEntry
│   │   └── services/             # expenseApi.ts
│   ├── members/
│   │   ├── components/           # MemberTable, InviteModal, RoleBadge
│   │   ├── hooks/                # useMembers, useInviteLink
│   │   └── services/             # memberApi.ts
│   └── summary/
│       ├── components/           # FinancialSummaryCard, BalanceChart
│       ├── hooks/                # useMessSummary
│       └── services/             # summaryApi.ts
│
├── lib/                          # Core configurations
│   ├── api-client.ts             # Axios instance & interceptors
│   ├── auth.ts                   # Token storage, parsing & session checks
│   └── query-client.ts           # React Query default configuration
│
└── types/                        # Global shared models & API response wrappers
├── api.ts                    # PaginatedResult, ApiErrorResponse
└── tenant.ts                 # MessContext, Role definitions

---

## 3. Multi-Tenancy & Routing Strategy
The application operates around multi-tenant scoping where state and API calls depend on the active `MessId`.

* **Route Structure**: `/dashboard/[messId]/*` ensures bookmarkable, shareable views directly tied to a specific mess.
* **Tenant Context**: A global `MessContext` (or custom hook `useCurrentMess()`) extracts `messId` from the route params and automatically attaches the `X-Mess-Id: <id>` header to outgoing HTTP requests via Axios interceptors.
* **Mess Switching**: Switching messes via the UI updates the route, invalidating and refetching relevant TanStack Query caches instantly.

---

## 4. Authentication & Authorization (RBAC) Architecture

### 4.1 Authentication Flow
* Authentication is handled via Phone Number / Password (or OTP) with JWT tokens (Access + Refresh tokens).
* Tokens are stored in secure HTTP-only cookies where applicable, or memory-backed access tokens paired with secure cookie refresh flows to prevent XSS attacks.

### 4.2 Role-Based Access Control (RBAC)
Role resolution is evaluated on two tiers:
1. **System Level**: Super Admin.
2. **Tenant Level (Mess-scoped)**: Meal Manager, Member.

* **Role Guard Component**: `<Authorize roles={['SuperAdmin', 'MealManager']}>` conditionally renders UI elements like expense entry forms, utility configuration panels, and role-management options.
* **Route Protection**: Next.js Middleware checks route access based on session tokens, redirecting unauthorized users before the page renders.

---

## 5. State Management & Data Fetching Patterns

### 5.1 Server State (TanStack Query)
* **Normalized Keys**: Query keys follow strict hierarchy:
  * `['messes', messId, 'meals', { month, year }]`
  * `['messes', messId, 'expenses', 'bazar', { page, pageSize }]`
  * `['messes', messId, 'summary']`
* **Optimistic Updates**: Immediate UI feedback on meal status toggles (e.g., ticking breakfast/lunch/dinner on/off) with rollback on server failure.
* **Auto Invalidation**: Submitting a bazar expense or updating meal participation automatically marks the `summary` query key stale to refresh meal rates and balances in the background.

### 5.2 Form Management & Validations
* Forms use **React Hook Form** with **Zod** schemas mirrored directly against backend FluentValidation rules.
* Dynamic field arrays handle multi-member daily meal submissions and batch expense items smoothly.

---

## 6. Real-Time & Event Handling (Orleans & RabbitMQ alignment)
* Background generation tasks (such as end-of-month financial settlement statements and PDF exports processed via RabbitMQ) trigger notifications.
* WebSockets / SSE (or lightweight polling via React Query) alert the manager when async generation tasks complete, enabling automatic download.

---

## 7. Responsive Design & Mobile Adaptability
* Built with a mobile-first philosophy using Tailwind CSS to accommodate users accessing the web dashboard from smartphones.
* Touch-friendly controls for meal count sheets (stepper buttons, swipe gestures, collapsible expense lists).
* Designed to ease the eventual transition to Native/Flutter/MAUI by matching API contracts and data models 1:1.
