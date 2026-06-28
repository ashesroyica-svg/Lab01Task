<p align="center">
  <img src="images/Logo_ICA.jpg" alt="ICA Logo" width="160" />
</p>

<h1 align="center">ICA TaskFlow</h1>
<p align="center">Employee Task & Project Management Portal</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet" />
  <img src="https://img.shields.io/badge/Architecture-Clean%20Architecture-blue" />
  <img src="https://img.shields.io/badge/Database-SQL%20Server-CC2927?logo=microsoftsqlserver" />
  <img src="https://img.shields.io/badge/Auth-JWT%20Bearer-orange" />
  <img src="https://img.shields.io/badge/Tests-35%20Passed-brightgreen" />
</p>

---

## Overview

ICA TaskFlow is an internal web application that lets ICA employees create, organise, and track their daily tasks and projects. It is built on **ASP.NET Core 8** using **Clean Architecture** — a Razor Pages front-end communicates with a dedicated Web API back-end backed by **Microsoft SQL Server**.

---

## Features

- **Authentication** — Register, login, and logout with JWT Bearer tokens
- **Projects** — Create, edit, filter, and soft-delete projects with status and priority tracking
- **Tasks** — Full CRUD for tasks linked to projects; quick one-click status cycling
- **Dashboard** — KPI cards (Total Projects, Pending Tasks, High Priority, Completed) plus a bar chart (tasks per project) and pie chart (task status breakdown)
- **Dark / Light theme** — Toggle persisted in `localStorage`
- **Responsive UI** — Bootstrap 5 sidebar layout that collapses on mobile

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | ASP.NET Core 8 Razor Pages, Bootstrap 5.3, Chart.js 4, Vanilla JS |
| Backend | ASP.NET Core 8 Web API |
| ORM | Entity Framework Core 8 (Code-First) |
| Database | Microsoft SQL Server |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Password Hashing | BCrypt.Net-Next (work factor 12) |
| API Docs | Swashbuckle / Swagger UI |
| Testing | xUnit, Moq |
| Secrets | DotNetEnv (`.env` file → environment variables) |

---

## Architecture

The solution follows **Clean Architecture** with a strict inward dependency rule:

```
Presentation  →  Infrastructure  →  Application  →  Domain
```

```
SRC/
├── Core/
│   ├── Application/
│   │   ├── DTOs/                  ← Request / response shapes
│   │   ├── RepositoryInterfaces/  ← Contracts for data access
│   │   ├── ServiceInterfaces/     ← Contracts for business logic
│   │   ├── Services/              ← Business logic implementations
│   │   └── WrapperClass/          ← ApiResponse<T>
│   └── Domain/
│       └── Entities/              ← TBL_User, TBL_Project, TBL_Task
├── Infrastructure/
│   └── Persistence/
│       ├── Context/               ← AppDbContext (EF Core)
│       ├── Helpers/               ← PasswordHasher (BCrypt)
│       └── Repositories/         ← EF Core repository implementations
└── Presentation/
    ├── Todo.API/                  ← ASP.NET Core Web API (port 7001)
    └── Todo.UI/                   ← ASP.NET Core Razor Pages (port 7000)

Tests/
└── Application.Tests/             ← 35 xUnit tests (service layer)
```

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Microsoft SQL Server (local or remote)
- Git

---

## Getting Started

### 1. Clone the repository

```bash
git clone <repo-url>
cd Lab01
```

### 2. Configure secrets

Copy the environment template and fill in your values:

```bash
cp SRC/Presentation/Todo.API/.env.example SRC/Presentation/Todo.API/.env
```

Edit `.env`:

```env
ConnectionStrings__DefaultConnection=Server=YOUR_SERVER;Database=ICATodoApp;User Id=YOUR_DB_USER;Password=YOUR_DB_PASSWORD;TrustServerCertificate=True;
JwtSettings__SecretKey=REPLACE_WITH_MIN_32_CHAR_RANDOM_SECRET!!
JwtSettings__Issuer=ICA_TodoApp
JwtSettings__Audience=ICA_Employees
JwtSettings__ExpiryInMinutes=60
```

> **Note:** `.env` is gitignored and must never be committed. Use a dedicated SQL account with least-privilege permissions — do not use `sa`.

### 3. Restore packages

```bash
dotnet restore
```

### 4. Apply database migrations

```bash
dotnet ef database update \
  --project SRC/Infrastructure/Persistence \
  --startup-project SRC/Presentation/Todo.API
```

### 5. Run the application

Open two terminals:

```bash
# Terminal 1 — API
dotnet run --project SRC/Presentation/Todo.API

# Terminal 2 — UI
dotnet run --project SRC/Presentation/Todo.UI
```

| App | Default URL |
|---|---|
| Web UI | https://localhost:7000 |
| REST API | https://localhost:7001 |
| Swagger UI | https://localhost:7001/swagger |

---

## API Endpoints

All endpoints (except auth) require `Authorization: Bearer <token>`.

### Auth — `/api/auth`

| Method | Route | Description | Auth |
|---|---|---|---|
| POST | `/register` | Create a new account | No |
| POST | `/login` | Authenticate and receive JWT | No |

### Projects — `/api/project`

| Method | Route | Description |
|---|---|---|
| GET | `/` | List all projects (filter by status, priority, search) |
| GET | `/{id}` | Get project by ID |
| POST | `/` | Create a project |
| PUT | `/{id}` | Update a project |
| DELETE | `/{id}` | Soft-delete a project |

### Tasks — `/api/todo`

| Method | Route | Description |
|---|---|---|
| GET | `/` | List all tasks (filter by projectId, status, priority, search) |
| GET | `/{id}` | Get task by ID |
| POST | `/` | Create a task |
| PUT | `/{id}` | Update a task |
| PATCH | `/{id}/status` | Update task status only |
| DELETE | `/{id}` | Soft-delete a task |

### Dashboard — `/api/dashboard`

| Method | Route | Description |
|---|---|---|
| GET | `/stats` | KPI counts + chart data for the authenticated user |

### API Response Shape

All responses use a consistent wrapper:

```json
{
  "success": true,
  "message": "Operation description",
  "data": { }
}
```

---

## Running Tests

```bash
dotnet test Tests/Application.Tests/Application.Tests.csproj
```

```
Total tests: 35   Passed: 35   Failed: 0
```

The test suite covers the full service layer — `AuthService`, `ProjectService`, `TodoService`, and `DashboardService` — using mocked repositories.

---

## Database Schema

Every table follows this base convention:

| Column | Type | Notes |
|---|---|---|
| `Id` | `int` | Primary key, identity |
| `CreatedDate` | `datetime` | UTC, set on insert |
| `UpdatedDate` | `datetime?` | UTC, set on update |
| `IsDeleted` | `bit` | Soft-delete flag (default `false`) |

EF Core global query filters ensure deleted records are never returned in any query.

---

## Project Pages

| Page | Route | Auth Required |
|---|---|---|
| Register | `/Account/Register` | No |
| Login | `/Account/Login` | No |
| Dashboard | `/Dashboard` | Yes |
| Projects | `/Projects` | Yes |
| Tasks | `/Todos` | Yes |

---

## Security Notes

- Passwords are hashed with BCrypt (work factor 12) — never stored in plain text
- JWT tokens are validated on every protected request (issuer, audience, lifetime, signature)
- All records are soft-deleted; no data is permanently removed
- Users can only access their own projects and tasks
- Secrets are loaded from `.env` at startup and must not be committed to source control

---

## License

Internal use only — ICA Institute © 2026
