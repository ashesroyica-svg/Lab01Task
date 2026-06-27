# claude.md — AI Assistant Guidance for ICA Todo Application

## Project Identity

**Project Name:** ICA Employee Task Tracker  
**Type:** Full Stack Web Application  
**Architecture:** Clean Architecture (ASP.NET Core 8 + Razor Pages)  
**Purpose:** Internal tool for ICA Employees to record and track daily tasks and projects

---

## Tech Stack Summary

| Layer | Technology |
|---|---|
| Frontend | ASP.NET Core 8 Razor Pages, Bootstrap 5.3, Vanilla JS |
| Backend | ASP.NET Core 8 Web API, Clean Architecture |
| ORM | Entity Framework Core 8 (Code-First) |
| Database | Microsoft SQL Server |
| Auth | JWT Bearer Tokens (stored in localStorage) |
| Password Hashing | BCrypt.Net-Next |
| DI Container | Built-in ASP.NET Core DI |

---

## Clean Architecture Folder Structure

```
SRC/
├── Core/
│   ├── Application/
│   │   ├── DTOs/
│   │   ├── RepositoryInterfaces/
│   │   ├── Services/
│   │   ├── ServiceInterfaces/
│   │   └── WrapperClass/          ← ApiResponse<T> lives here
│   └── Domain/
│       └── Entities/
├── Infrastructure/
│   ├── Infrastructure/
│   │   └── Helpers/               ← Third-party integrations if needed
│   └── Persistence/
│       └── Repositories/          ← All EF Core DB code
└── Presentation/
    ├── Todo.API/                  ← ASP.NET Core Web API
    └── Todo.UI/                   ← ASP.NET Core Razor Pages
```

**Dependency Rule:** Dependencies flow inward only.  
`Presentation → Infrastructure → Application → Domain`  
Domain has zero dependencies on outer layers.

---

## Coding Conventions

### General
- Use `async/await` throughout — no `.Result` or `.Wait()` calls
- All service methods return `ApiResponse<T>` wrapper
- Never expose domain entities directly via API — always use DTOs
- Soft deletes only — never hard delete records
- All timestamps in UTC (`DateTime.UtcNow`)

### Naming
- Entities: `TBL_` prefix in database (e.g., `TBL_User`, `TBL_Project`, `TBL_Task`)
- Interfaces: `I` prefix (e.g., `IUserService`, `ITaskRepository`)
- DTOs: suffix with `Dto` (e.g., `RegisterDto`, `TaskCreateDto`)
- Repositories: suffix with `Repository` (e.g., `UserRepository`)
- Services: suffix with `Service` (e.g., `AuthService`, `TaskService`)

### API Response Wrapper
All API responses MUST use this structure:
```json
{
  "success": true,
  "message": "Operation description",
  "data": null
}
```
Wrapper class: `ApiResponse<T>` in `Application/WrapperClass/`

---

## Authentication Rules

- JWT tokens generated with `Microsoft.AspNetCore.Authentication.JwtBearer`
- Secrets stored in `appsettings.json` under `JwtSettings`
- Token stored in browser `localStorage` on login
- Token deleted from `localStorage` on logout
- All Todo/Project API endpoints require `[Authorize]` attribute
- Token validation middleware applied globally in API

---

## Frontend Behavior Rules

### Pages & Navigation
| Page | Route | Auth Required |
|---|---|---|
| Register | `/Account/Register` | No |
| Login | `/Account/Login` | No |
| Dashboard | `/Dashboard` | Yes |
| Projects | `/Projects` | Yes |
| Tasks/Todos | `/Todos` | Yes |

### UI Patterns
- Show loading spinner on every API call
- Validate all inputs client-side before API call
- On JWT expiry → redirect to Login and clear localStorage
- All modals use Bootstrap 5 modal component
- Dark/Light theme toggle persisted in `localStorage` as `theme`

### Debouncing
Search/filter inputs must use 300ms debounce before triggering API calls.

---

## Database Rules

- Use EF Core Code-First migrations
- All tables have: `Id` (PK, identity), `CreatedDate` (UTC), `UpdatedDate` (UTC nullable), `IsDeleted` (bool, default false)
- Soft delete filter applied globally via EF Core query filter: `.HasQueryFilter(e => !e.IsDeleted)`
- Passwords are NEVER stored in plain text — always BCrypt hashed
- Add indexes on: `Email` (unique), `UserId` (FK), `IsDeleted`, `Status`

---

## Key Business Rules

1. A **Project** has: Name, Description, Status (Active/OnHold/Completed), Priority (Low/Medium/High), DueDate, OwnerId
2. A **Task** belongs to a Project; has: Title, Description, Priority (Low/Medium/High), Status (Pending/InProgress/Completed), DueDate
3. Dashboard shows 4 KPI cards: Total Projects, Pending Tasks, High Priority Tasks, Completed Tasks
4. Dashboard shows Bar chart (tasks per project) and Pie chart (task status distribution)
5. Users only see their own projects and tasks (filter by authenticated UserId)
6. Logout clears JWT from localStorage and redirects to Login

---

## NuGet Packages (Backend)

```
Microsoft.AspNetCore.Authentication.JwtBearer
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
BCrypt.Net-Next
AutoMapper (optional, for DTO mapping)
Swashbuckle.AspNetCore (Swagger)
```

---

## appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=ICA_TodoDB;..."
  },
  "JwtSettings": {
    "SecretKey": "YOUR_SECRET_KEY_MIN_32_CHARS",
    "Issuer": "ICA_TodoApp",
    "Audience": "ICA_Employees",
    "ExpiryInMinutes": 60
  },
  "Logging": { ... },
  "AllowedHosts": "*"
}
```

---

## What Claude Should NOT Do

- Do NOT store passwords in plain text
- Do NOT expose `IsDeleted` records in any list API response
- Do NOT hard-delete any records
- Do NOT return domain `Entities` directly from controllers — use DTOs
- Do NOT skip JWT validation on protected endpoints
- Do NOT put business logic in controllers — use Services
- Do NOT put DB queries in Services — use Repositories
- Do NOT use synchronous DB calls

---

## File Generation Priority Order

When generating code files, follow this order:
1. Domain Entities
2. Application DTOs + Interfaces + Wrapper
3. Persistence (DbContext + Repositories)
4. Application Services
5. API Controllers
6. Razor Pages (UI)
7. Program.cs registrations (both API and UI)
8. appsettings.json
9. EF Core Migrations

---

## Running the Application

```bash
# Restore packages
dotnet restore

# Apply migrations
dotnet ef database update --project Infrastructure/Persistence --startup-project Presentation/Todo.API

# Run API
cd Presentation/Todo.API
dotnet run

# Run UI (separate terminal)
cd Presentation/Todo.UI
dotnet run
```

Default ports: API → `https://localhost:7001` | UI → `https://localhost:7000`
