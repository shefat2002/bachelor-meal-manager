# Comprehensive Project Plan: Universal Meal Manager System

## 1. Overview
The Universal Meal Manager System is a highly scalable, multi-tenant application designed to manage daily meal participation, expenses, and financial contributions across multiple messes. The system automates financial calculations, ensures data isolation between tenants, and provides role-based access control.

## 2. System Architecture
The application utilizes a distributed, service-oriented architecture designed for high availability and horizontal scaling.

### 2.1 Technology Stack
- **Backend Framework**: ASP.NET Core Web API (.NET 10, C# 14)
- **State Management & Concurrency**: Microsoft Orleans (Virtual Actor Model)
- **Primary Database**: PostgreSQL
- **Distributed Cache & State Store**: Redis
- **Message Broker**: RabbitMQ
- **Containerization**: Docker
- **CI/CD**: GitHub Actions
- **Observability**: Serilog (Logging), Prometheus & Grafana (Monitoring)

### 2.2 Design Patterns
- **CQRS (Command Query Responsibility Segregation)**: Implemented using MediatR to separate read and write operations.
- **Validation**: FluentValidation applied to all incoming API requests.

## 3. Database Schema & Multi-Tenancy
The system utilizes a multi-tenant architecture where all data operations are scoped by `MessId`.

- **messes**: Stores details of individual messes.
- **UtilityTypes**: Allows Super Admins and Meal Managers to define custom utility types (e.g., Electricity, Wi-Fi, Cook Salary) per Mess.
- **Members**: Stores basic user information (Name, Phone Number).
- **MembersMesses**: Manages the many-to-many relationship of users belonging to multiple messes, including joining/leaving logic and role assignments.
- **DailyMeals**: Tracks meal participation (Breakfast, Lunch, Dinner) linked to a Member, Date, and MessId.
- **BazarExpenses**: Records daily meal expenses (Item Name, Price, Date, Buyer Member).
- **UtilityPayments**: Captures periodic utility payments associated with a specific utility type, amount, and payment date.

## 4. Authentication & Authorization
Built using **ASP.NET Core Identity** with JWT tokens.

### 4.1 Roles
- **Super Admin**: Full global privileges and system management, including Mess creation and deletion.
- **Meal Manager**: Scoped to specific messes, managing daily meals, utility custom types, and expenses.
- **Member**: General user, can generate invitation links requiring Admin/Manager approval to join.

### 4.2 User Flow & Access Control
- Users register/log in via phone number.
- Users can join multiple messes via an invitation link or Mess ID. 
- Data isolation ensures members can only view data from messes they are authorized to access.

## 5. Business Logic & Equations
- **Meal Rate Calculation**: (Total Meal Costs for the Month) / (Total Meal Units Consumed).
- **Individual Meal Cost**: (Member's Total Meal Units) x (Meal Rate).
- **Total Individual Cost**: (Individual Meal Cost) + (Share of Utility Costs).
- **Surplus / Arrear**: (Total Money Given by Member) - (Total Individual Cost).
- **On Hand Balance**: (Total Money Collected) - (Total Expenses).

### 5.1 Orleans Implementation
- **Grains**: Virtual actors representing individual Members or messes.
- **Concurrency**: Orleans processes requests per grain sequentially, eliminating the need for manual locking mechanisms.
- **State Management**: Grains manage their persistent state automatically, loading from Redis/PostgreSQL on activation.

## 6. API Endpoint Specifications

### 6.1 messes
- `POST /api/messes`: Create a new Mess (Super Admin).
- `GET /api/messes`: List authorized messes.
- `PUT /api/messes/{id}`: Update Mess details.
- `DELETE /api/messes/{id}`: Delete a Mess (Super Admin/Creator).

### 6.2 Members
- `POST /api/members/join`: Join a Mess via invitation link.
- `POST /api/members/invite`: Generate an invitation link.
- `PUT /api/members/{id}/role`: Assign roles within a Mess.

### 6.3 Daily Meals
- `POST /api/meals`: Submit daily meal counts for members (Breakfast, Lunch, Dinner).
- `GET /api/meals`: Retrieve historical meal counts with filtering, sorting, and pagination.

### 6.4 Bazar Expenses
- `POST /api/expenses/bazar`: Record purchase items with price, date, and member.
- `GET /api/expenses/bazar`: Fetch paginated bazar expenses for a given Mess.

### 6.5 Utility Payments
- `POST /api/expenses/utility`: Record utility payments linked to custom UtilityType definitions.
- `GET /api/expenses/utility`: Retrieve utility payments for a specific Mess.

### 6.6 Financial Summary
- `GET /api/summary/messes/{messId}`: Returns meal rate, total costs, and individual balances for all members.

## 7. Message Broker (RabbitMQ) Roles
- Handles asynchronous decoupling of services.
- Examples include background processing for push notifications, email alerts for invitations, and automated monthly report generation.

## 8. Frontend Overview (Future Scope)
- **Web Application**: Dashboard for Admins, Managers, and Members to input data and view financial summaries.
- **Mobile Application**: Simplified interface for members to check their daily meal status and individual balances.

## 9. Observability and Deployment

### 9.1 Observability
- **Logging**: Application logs captured via Serilog.
- **Monitoring**: Prometheus scrapes metrics for system health.
- **Visualization**: Grafana dashboards display system performance and resource usage.

### 9.2 CI/CD and Deployment
- Automate pipelines via **GitHub Actions**.
- Pipelines build the .NET code, execute unit and integration tests, construct Docker images, and push them to a container registry for deployment.