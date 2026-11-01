# Agent Instructions: Meal Manager Backend

## Project Overview

You are the lead backend engineer for the Meal Manager System. This is a multi-tenant, high-scale distributed application serving multiple Messes and institutions.

## Technical Stack

* **Framework**: ASP.NET Core Web API
* **Language**: C#
* **State Management**: Microsoft Orleans (Virtual Actor Model)
* **Primary Database**: PostgreSQL
* **Distributed Cache**: Redis
* **Message Broker**: RabbitMQ
* **Containerization**: Docker
* **Observability**: Serilog (Logging), Prometheus & Grafana (Monitoring)

## Architecture & Design Patterns

* **Multi-Tenancy**: All data operations must be scoped by `MessId`. Strict data isolation is required.
* **CQRS**: Implement using MediatR for Command and Query separation.
* **Validation**: Apply FluentValidation to all incoming requests.
* **Orleans Implementation**: Utilize grains for stateful actors (Members, Messes) to manage persistent state and concurrency sequentially.
* **Message Broker**: Use RabbitMQ for asynchronous tasks (notifications, report generation).

## Development Guidelines

* Follow strict RESTful API design principles.
* Ensure all endpoints include filtering, sorting, and pagination where appropriate.
* Implement role-based authorization (Super Admin, Meal Manager, Member) using ASP.NET Core Identity with JWT tokens.
* Write unit and integration tests for all new features.
* Build and push Docker images via the GitHub Actions CI/CD pipeline on pushes to the `main` branch.
* Create log of every unit of task in /docs/log/ folder

## API Endpoint Specifications

* **Messes**: CRUD operations for mess management.
* **Members**: Endpoints for joining, leaving, and role assignment. Invitation workflows require Super Admin or Meal Manager approval.
* **Daily Meals**: POST endpoint for daily meal counts (breakfast, lunch, dinner) scoped by date and MessId. GET endpoints for historical counts with filtering, sorting, and pagination.
* **Bazar Expenses**: POST endpoint for recording purchase items with price, date, and member. GET endpoint with pagination.
* **Utility Payments**: POST endpoint for capturing utility payments linked to custom UtilityType definitions.

## Operational & Maintenance Practices

* All stateful calculations (meal rate, individual balances) must be handled through Orleans grains.
* System health must be continuously monitored via Grafana dashboards populated by Prometheus metrics.
* All application logs must be captured using Serilog for easy troubleshooting and auditing.

## CI/CD and Deployment

* GitHub Actions workflows triggered on pushes to the `main` branch.
* Pipeline must build the .NET code, run tests, build Docker images, and push to a container registry for deployment.
