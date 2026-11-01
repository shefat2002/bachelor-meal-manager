## Overview

This document outlines the high-level backend architecture for the Meal Manager Application, designed to scale from a single hostel mess to a universal platform serving multiple institutions, hostels, and users.

## Technology Stack

* **Framework**: ASP.NET Core Web API
* **Language**: C#
* **State Management & Concurrency**: Microsoft Orleans (Virtual Actor Model)
* **Primary Database**: PostgreSQL
* **Distributed Cache & State Store**: Redis
* **Message Broker**: RabbitMQ
* **Containerization**: Docker
* **CI/CD**: GitHub Actions
* **Observability**: Serilog (Logging), Prometheus & Grafana (Monitoring)

## Database Schema & Multi-Tenancy

The system utilizes a multi-tenant architecture scoped by `HostelId`.

* **Hostels Table**: Stores details of individual messes or hostels.
* **UtilityTypes Table**: Allows Super Admins and Meal Managers to define custom utility types (e.g., Electricity, Wi-Fi, Cook Salary) per hostel.
* **Members Table**: Stores basic user information (Name, Phone Number).
* **MembersHostels Table**: Manages the many-to-many relationship of users belonging to multiple hostels, including joining/leaving logic and role assignments.
* **DailyMeals Table**: Tracks meal participation (Breakfast, Lunch, Dinner) linked to a Member, Date, and HostelId.
* **BazarExpenses Table**: Records daily meal expenses (Item Name, Price, Date, Buyer Member).
* **UtilityPayments Table**: Captures periodic utility payments associated with a specific utility type, amount, and payment date.

## Authentication & Authorization

Built using **ASP.NET Core Identity** with JWT tokens.

* **Roles**:
* **Super Admin**: Full global privileges and system management.
* **Meal Manager**: Scoped to specific hostels, managing daily meals and expenses.
* **Member**: General user, can generate invitation links requiring Admin/Manager approval to join.


* **Access Control**: Strict data isolation ensuring members can only view data from hostels they are authorized to access.

## Design Patterns & Logic

* **CQRS (Command Query Responsibility Segregation)**: Implemented using MediatR to separate read and write operations.
* **Validation**: FluentValidation applied to incoming requests.
* **Caching**: Redis is utilized to cache frequently accessed data such as calculated meal rates and hostel details to reduce database queries.

## Microsoft Orleans Implementation

Orleans handles high-scale stateful actor management.

* **Grains**: Virtual actors representing individual Members or Hostels.
* **State Management**: Automatically manages persistent state and concurrency, allowing the system to process concurrent requests sequentially per grain without locking mechanisms.
* **Scaling**: Distributed across a cluster of silos capable of handling millions of concurrent users.

## Message Broker (RabbitMQ)

Handles asynchronous communication and decouples services.

* Examples include processing background services for push notifications, email alerts for invitations, and automated report generation.

## CI/CD & Deployment

* Automated pipelines built using **GitHub Actions**.
* Triggers build on pushes to the `main` branch.
* Runs unit and integration tests, builds Docker images, and pushes them to a container registry for deployment to cloud providers or on-premises servers.

## Observability Stack

* **Logging**: Serilog captures application logs.
* **Monitoring**: Prometheus scrapes metrics for system health, which are visualized via Grafana dashboards.