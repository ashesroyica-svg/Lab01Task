# spec.md — Technical Specification
## ICA Employee Task Tracker — ASP.NET Core 8 / Clean Architecture

**Version:** 1.0.0  
**Date:** 2025  
**Author:** Solution Architect  
**Status:** Approved for Development

---

## 1. System Overview

The ICA Employee Task Tracker is an internal web application that enables ICA employees to create, manage, and track daily tasks organized by project. It features JWT-based authentication, a responsive Bootstrap 5.3 UI with dark/light theme support, and a RESTful API backend following Clean Architecture principles.

---

## 2. Architecture

### 2.1 Architecture Pattern
**Clean Architecture** — separates concerns into concentric layers:

```
[Domain] ← [Application] ← [Infrastructure] ← [Presentation]
```

- **Domain:** Pure business entities. Zero dependencies.
- **Application:** Business logic, service interfaces, repository interfaces, DTOs.
- **Infrastructure:** EF Core implementation, database context, repository implementations.
- **Presentation:** API controllers, Razor Pages UI.

### 2.2 Solution Structure

```
ICA_TodoApp.sln
└── SRC/
    ├── Core/
    │   ├── Application/           (Class Library)
    │   │   ├── DTOs/
    │   │   │   ├── Auth/
    │   │   │   │   ├── RegisterDto.cs
    │   │   │   │   ├── LoginDto.cs
    │   │   │   │   └── LoginResponseDto.cs
    │   │   │   ├── Project/
    │   │   │   │   ├── ProjectCreateDto.cs
    │   │   │   │   ├── ProjectUpdateDto.cs
    │   │   │   │   └── ProjectResponseDto.cs
    │   │   │   └── Todo/
    │   │   │       ├── TodoCreateDto.cs
    │   │   │       ├── TodoUpdateDto.cs
    │   │   │       └── TodoResponseDto.cs
    │   │   ├── RepositoryInterfaces/
    │   │   │   ├── IUserRepository.cs
    │   │   │   ├── IProjectRepository.cs
    │   │   │   └── ITodoRepository.cs
    │   │   ├── ServiceInterfaces/
    │   │   │   ├── IAuthService.cs
    │   │   │   ├── IProjectService.cs
    │   │   │   ├── ITodoService.cs
    │   │   │   └── IDashboardService.cs
    │   │   ├── Services/
    │   │   │   ├── AuthService.cs
    │   │   │   ├── ProjectService.cs
    │   │   │   ├── TodoService.cs
    │   │   │   └── DashboardService.cs
    │   │   └── WrapperClass/
    │   │       └── ApiResponse.cs
    │   └── Domain/               (Class Library)
    │       └── Entities/
    │           ├── User.cs
    │           ├── Project.cs
    │           └── TodoTask.cs
    ├── Infrastructure/
    │   ├── Infrastructure/        (Class Library — optional helpers)
    │   │   └── Helpers/
    │   └── Persistence/           (Class Library)
    │       ├── Context/
    │       │   └── AppDbContext.cs
    │       ├── Repositories/
    │       │   ├── UserRepository.cs
    │       │   ├── ProjectRepository.cs
    │       │   └── TodoRepository.cs
    │       └── Migrations/
    └── Presentation/
        ├── Todo.API/              (ASP.NET Core Web API)
        │   ├── Controllers/
        │   │   ├── AuthController.cs
        │   │   ├── ProjectController.cs
        │   │   ├── TodoController.cs
        │   │   └── DashboardController.cs
        │   ├── Program.cs
        │   └── appsettings.json
        └── Todo.UI/               (ASP.NET Core Razor Pages)
            ├── Pages/
            │   ├── Account/
            │   │   ├── Register.cshtml / .cs
            │   │   └── Login.cshtml / .cs
            │   ├── Dashboard/
            │   │   └── Index.cshtml / .cs
            │   ├── Projects/
            │   │   └── Index.cshtml / .cs
            │   ├── Todos/
            │   │   └── Index.cshtml / .cs
            │   └── Shared/
            │       ├── _Layout.cshtml
            │       └── _Navbar.cshtml
            ├── wwwroot/
            │   ├── css/
            │   │   └── site.css
            │   └── js/
            │       ├── auth.js
            │       ├── dashboard.js
            │       ├── projects.js
            │       └── todos.js
            ├── Program.cs
            └── appsettings.json
```

---

## 3. Database Specification

### 3.1 Approach
EF Core 8 Code-First with SQL Server. Migrations tracked in `Persistence/Migrations/`.

### 3.2 Table Definitions

#### TBL_User
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, Identity, NOT NULL |
| Username | nvarchar(100) | NOT NULL |
| Email | nvarchar(255) | UNIQUE, NOT NULL |
| PasswordHash | nvarchar(500) | NOT NULL |
| IsDeleted | bit | DEFAULT 0, NOT NULL |
| CreatedDate | datetime2 | NOT NULL, DEFAULT GETUTCDATE() |
| UpdatedDate | datetime2 | NULL |

#### TBL_Project
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, Identity, NOT NULL |
| UserId | int | FK → TBL_User.Id, NOT NULL |
| Name | nvarchar(200) | NOT NULL |
| Description | nvarchar(1000) | NULL |
| Status | nvarchar(50) | NOT NULL (Active/OnHold/Completed) |
| Priority | nvarchar(50) | NOT NULL (Low/Medium/High) |
| DueDate | datetime2 | NULL |
| IsDeleted | bit | DEFAULT 0, NOT NULL |
| CreatedDate | datetime2 | NOT NULL |
| UpdatedDate | datetime2 | NULL |

**Index:** `IX_TBL_Project_UserId_IsDeleted` on (UserId, IsDeleted)

#### TBL_Task
| Column | Type | Constraints |
|---|---|---|
| Id | int | PK, Identity, NOT NULL |
| ProjectId | int | FK → TBL_Project.Id, NOT NULL |
| UserId | int | FK → TBL_User.Id, NOT NULL |
| Title | nvarchar(300) | NOT NULL |
| Description | nvarchar(2000) | NULL |
| Priority | nvarchar(50) | NOT NULL (Low/Medium/High) |
| Status | nvarchar(50) | NOT NULL (Pending/InProgress/Completed) |
| DueDate | datetime2 | NULL |
| IsCompleted | bit | DEFAULT 0, NOT NULL |
| IsDeleted | bit | DEFAULT 0, NOT NULL |
| CreatedDate | datetime2 | NOT NULL |
| UpdatedDate | datetime2 | NULL |

**Index:** `IX_TBL_Task_ProjectId_UserId_IsDeleted` on (ProjectId, UserId, IsDeleted)  
**Index:** `IX_TBL_Task_Status_Priority` on (Status, Priority)

### 3.3 Global Query Filter
```csharp
modelBuilder.Entity<Project>().HasQueryFilter(p => !p.IsDeleted);
modelBuilder.Entity<TodoTask>().HasQueryFilter(t => !t.IsDeleted);
```

---

## 4. API Specification

### 4.1 Base URL
```
API:  https://localhost:7001/api
UI:   https://localhost:7000
```

### 4.2 Standard Response Envelope
```json
{
  "success": true | false,
  "message": "Human-readable message",
  "data": <T> | null
}
```

### 4.3 Authentication Endpoints

#### POST /api/auth/register
**Request Body:**
```json
{
  "username": "string (required, min:2, max:100)",
  "email": "string (required, valid email)",
  "password": "string (required, min:6)",
  "confirmPassword": "string (must match password)"
}
```
**Success Response (200):**
```json
{ "success": true, "message": "Registration successful", "data": null }
```
**Failure Response (400):**
```json
{ "success": false, "message": "Email already registered", "data": null }
```

#### POST /api/auth/login
**Request Body:**
```json
{
  "email": "string (required)",
  "password": "string (required)"
}
```
**Success Response (200):**
```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "token": "eyJhbGciOi...",
    "username": "John Doe",
    "email": "john@ica.com",
    "expiresAt": "2025-01-01T12:00:00Z"
  }
}
```

### 4.4 Dashboard Endpoints

#### GET /api/dashboard/stats  `[Authorize]`
**Success Response (200):**
```json
{
  "success": true,
  "message": "Stats fetched",
  "data": {
    "totalProjects": 5,
    "pendingTasks": 12,
    "highPriorityTasks": 3,
    "completedTasks": 8,
    "projectTaskChart": [
      { "projectName": "Project A", "taskCount": 5 }
    ],
    "taskStatusChart": [
      { "status": "Pending", "count": 12 },
      { "status": "InProgress", "count": 4 },
      { "status": "Completed", "count": 8 }
    ]
  }
}
```

### 4.5 Project Endpoints  `[Authorize]`

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/projects | Get all projects (with filter: status, priority, search) |
| GET | /api/projects/{id} | Get project by ID |
| POST | /api/projects | Create new project |
| PUT | /api/projects/{id} | Update project |
| DELETE | /api/projects/{id} | Soft delete project |

**GET /api/projects — Query Params:**
```
?status=Active&priority=High&search=keyword&page=1&pageSize=10
```

**POST /api/projects — Request Body:**
```json
{
  "name": "string (required)",
  "description": "string (optional)",
  "status": "Active | OnHold | Completed",
  "priority": "Low | Medium | High",
  "dueDate": "2025-12-31T00:00:00Z"
}
```

### 4.6 Todo Task Endpoints  `[Authorize]`

| Method | Endpoint | Description |
|---|---|---|
| GET | /api/todos | Get all tasks (filter: projectId, status, priority, search) |
| GET | /api/todos/{id} | Get task by ID |
| POST | /api/todos | Create new task |
| PUT | /api/todos/{id} | Update task |
| PATCH | /api/todos/{id}/status | Update task status only |
| DELETE | /api/todos/{id} | Soft delete task |

**GET /api/todos — Query Params:**
```
?projectId=1&status=Pending&priority=High&search=keyword&page=1&pageSize=10
```

---

## 5. Frontend Specification

### 5.1 Pages

#### Register Page (`/Account/Register`)
- Full viewport centered card layout
- Fields: Username, Email, Password, Confirm Password
- Client-side validation before API call
- Show loading spinner during API call
- On success: redirect to `/Account/Login`
- On failure: show error message inside card

#### Login Page (`/Account/Login`)
- Full viewport centered card layout
- Fields: Email, Password
- Show loading spinner during API call
- On success: store JWT in `localStorage['ica_token']`, redirect to `/Dashboard`
- On failure: show inline error alert

#### Dashboard Page (`/Dashboard`)
- Navbar: ICA Logo + App Title (left), Logout button (right)
- 4 KPI Cards row: Total Projects | Pending Tasks | High Priority | Completed
- Bar Chart: Tasks per project (Chart.js)
- Pie Chart: Task status distribution (Chart.js)
- Data fetched on page load from `/api/dashboard/stats`

#### Projects Page (`/Projects`)
- Navbar (same pattern)
- "New Project" button → opens Bootstrap modal
- Filter bar: Status dropdown + Priority dropdown + Search input (debounced 300ms)
- Card grid of projects (each card shows: Name, Status badge, Priority badge, Due Date, Edit/Delete actions)
- Edit → opens pre-filled modal
- Delete → confirmation prompt → soft delete

#### Todos Page (`/Todos`)
- Navbar (same pattern)
- "New Task" button → opens Bootstrap modal (includes Project selector)
- Filter bar: Project dropdown + Status dropdown + Priority dropdown + Search (debounced 300ms)
- Card grid of tasks (each card: Title, Project, Status badge, Priority badge, Due Date, Edit/Delete)
- Status toggle button on each card
- Edit → opens pre-filled modal

### 5.2 Shared UI Components

#### Navbar (_Navbar.cshtml partial)
```html
[ICA Logo] ICA Task Tracker          [🌙 Theme] [Logout]
```

#### Loading Spinner
```html
<div id="loadingSpinner" class="d-none position-fixed top-50 start-50 translate-middle">
  <div class="spinner-border text-primary" role="status"></div>
</div>
```

#### Theme Toggle
- Toggle button in navbar
- Applies `data-bs-theme="dark"` to `<html>` element
- Persists preference in `localStorage['ica_theme']`
- Default: `light`

### 5.3 JavaScript Patterns

#### API Call Helper (auth.js)
```javascript
async function apiCall(url, method = 'GET', body = null) {
  const token = localStorage.getItem('ica_token');
  showSpinner();
  try {
    const res = await fetch(url, {
      method,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': token ? `Bearer ${token}` : ''
      },
      body: body ? JSON.stringify(body) : null
    });
    const data = await res.json();
    if (res.status === 401) { logout(); return null; }
    return data;
  } finally {
    hideSpinner();
  }
}
```

#### Debounce (todos.js / projects.js)
```javascript
function debounce(fn, delay = 300) {
  let timer;
  return (...args) => {
    clearTimeout(timer);
    timer = setTimeout(() => fn(...args), delay);
  };
}
const debouncedSearch = debounce(loadTasks);
document.getElementById('searchInput').addEventListener('input', debouncedSearch);
```

#### Logout
```javascript
function logout() {
  localStorage.removeItem('ica_token');
  localStorage.removeItem('ica_theme');
  window.location.href = '/Account/Login';
}
```

---

## 6. Security Specification

| Concern | Implementation |
|---|---|
| Password Storage | BCrypt.Net-Next with work factor 12 |
| JWT Secret | Stored in `appsettings.json` (move to env vars in production) |
| Token Lifetime | 60 minutes (configurable) |
| HTTPS | Enforced via `app.UseHttpsRedirection()` |
| CORS | Configured to allow only UI origin |
| SQL Injection | Prevented via EF Core parameterized queries |
| XSS | Razor auto-encoding + CSP headers |
| Authorization | All protected endpoints use `[Authorize]` attribute |
| Data Isolation | All queries filtered by authenticated `UserId` |

---

## 7. Configuration

### appsettings.json (Todo.API)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ICA_TodoDB;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "SecretKey": "ICA_TodoApp_SuperSecretKey_2025_MinLength32!",
    "Issuer": "ICA_TodoApp",
    "Audience": "ICA_Employees",
    "ExpiryInMinutes": 60
  },
  "AllowedHosts": "*"
}
```

### appsettings.json (Todo.UI)
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7001/api"
  },
  "AllowedHosts": "*"
}
```

---

## 8. NuGet Dependencies

### Application (Class Library)
_(No external dependencies — pure C#)_

### Persistence (Class Library)
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.*" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.*" />
<PackageReference Include="BCrypt.Net-Next" Version="4.*" />
```

### Todo.API
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="6.*" />
```

### Todo.UI
_(No additional NuGet — uses HttpClient for API calls via JS fetch)_

---

## 9. EF Core Setup Commands

```bash
# From solution root
dotnet ef migrations add InitialCreate \
  --project SRC/Infrastructure/Persistence \
  --startup-project SRC/Presentation/Todo.API \
  --output-dir Migrations

dotnet ef database update \
  --project SRC/Infrastructure/Persistence \
  --startup-project SRC/Presentation/Todo.API
```

---

## 10. Status & Priority Enums

```csharp
// Project Status
public enum ProjectStatus { Active, OnHold, Completed }

// Task Status
public enum TaskStatus { Pending, InProgress, Completed }

// Priority (shared)
public enum Priority { Low, Medium, High }
```

Stored as `nvarchar` in DB (not int) for readability and maintainability.
