# Agent Skill Definitions: Bachelor Meal Manager Frontend (Web)

## 1. Next.js App Router & Multi-Tenant Mess Scoping
Configure Next.js App Router route groups (`(auth)` and `(dashboard)/[messId]`) with strict TypeScript typing. Implement `MessContext` and centralized Axios request/response interceptors that automatically extract `messId` from route parameters, attach `X-Mess-Id` and JWT Bearer headers, and handle token refresh flows seamlessly.

## 2. Design System & Responsive App Shell (Tailwind CSS + Shadcn UI)
Build accessible UI primitives using Shadcn UI (Radix Primitives) and Tailwind CSS. Enforce `tabular-nums` typography on all financial and meal count displays, semantic color tokens for Surplus (Emerald) and Arrear (Rose) balances, and a responsive layout that transitions from a collapsible desktop sidebar to a fixed mobile bottom navigation bar.

## 3. Server State & Optimistic Mutations (TanStack Query)
Implement normalized, tenant-scoped React Query keys (`['messes', messId, ...]`) and custom data-fetching hooks. Configure optimistic UI updates with automatic error rollback for high-frequency interactions (such as toggling or incrementing daily Breakfast, Lunch, and Dinner counts) and trigger automatic invalidation of the `summary` query cache whenever meals, bazar expenses, or utility payments change.

## 4. Dynamic Forms & Schema Validation (React Hook Form + Zod)
Construct complex, multi-row data entry forms using React Hook Form and `useFieldArray` for batch Bazar item logging and daily meal sheets. Define strict Zod validation schemas that mirror backend FluentValidation rules 1:1, providing real-time client-side error feedback and auto-calculated subtotals before submission.

## 5. Role-Based Access Control (RBAC) & Route Guards
Implement Next.js Edge Middleware for protected route verification and a reusable `<Authorize roles={[...]}>` client wrapper component. Enforce tenant-level permission boundaries so Super Admins and Meal Managers can access expense drawers, utility configurations, member approvals, and month-locking controls, while regular Members receive read-only views of mess data.

## 6. Financial Visualization & Async Report Handling
Build interactive financial analytics using Recharts (daily bazar spend vs. meal volume trends) and high-density master settlement tables. Integrate real-time notification listeners (SSE/WebSockets or smart query polling) to track asynchronous RabbitMQ background jobs, alerting managers when monthly PDF financial statements are ready for download.


# Agent Skill Definitions: Bachelor Meal Manager Backend

## 1. Orleans Actor Management
Implement and configure Microsoft Orleans virtual actors (grains) for members and messes. Manage grain lifecycles, state persistence, and sequential execution of concurrent stateful requests (e.g., meal rate and balance accumulation) without manual locking.

## 2. CQRS and Endpoint Logic (MediatR)
Design API endpoints following the Command Query Responsibility Segregation pattern using MediatR. Separate read (queries) and write (commands) operations, and integrate FluentValidation for request payload validation before handler execution.

## 3. Data Access and Multi-Tenant Scoping (PostgreSQL)
Implement Entity Framework Core repositories for PostgreSQL. Ensure all database operations implement global query filters or scope data queries by `MessId` to enforce strict multi-tenant data isolation.

## 4. Asynchronous Worker Setup (RabbitMQ)
Configure RabbitMQ message brokers and background worker services. Enable decoupled, asynchronous processing for non-blocking tasks such as invitation notifications, email alerts, and automated monthly reporting.

## 5. Logging and Monitoring Configuration
Integrate Serilog for structured application logging across all services. Set up Prometheus metric endpoints for real-time system health tracking and configure Grafana dashboards for performance visualization.

## 6. CI/CD Pipeline Automation (GitHub Actions)
Create continuous integration and continuous deployment workflows. Automate building the .NET code, executing unit and integration tests, containerizing the application with Docker, and deploying images to the container registry.