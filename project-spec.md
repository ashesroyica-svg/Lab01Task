# project-spec.md — Project Delivery Specification
## ICA Employee Task Tracker

**Project Code:** ICA-TASK-001  
**Type:** Internal Web Application  
**Target Environment:** Windows Server / IIS or Azure App Service  
**Runtime:** .NET 8 SDK  
**IDE Support:** Visual Studio 2022, VS Code, JetBrains Rider

---

## 1. Project Goals

| Goal | Description |
|---|---|
| Primary | Enable ICA employees to create, assign, and track tasks grouped by project |
| Secondary | Provide management visibility via dashboard KPIs and charts |
| Technical | Demonstrate Clean Architecture with ASP.NET Core 8 as a reusable internal template |

---

## 2. Scope

### In Scope
- User registration and login with JWT authentication
- Project CRUD (Create, Read, Update, Soft Delete)
- Task/Todo CRUD per project (Create, Read, Update, Status Update, Soft Delete)
- Dashboard with KPI cards and charts
- Dark/Light theme toggle
- Responsive design (mobile + desktop)
- Search with debounce on Task and Project list pages
- Status and priority filtering on list pages

### Out of Scope (v1.0)
- Email notifications
- Role-based access control (Admin vs Employee)
- File attachments on tasks
- Task comments/activity log
- Real-time updates (SignalR)
- Multi-tenant support
- Mobile native app

---

## 3. User Stories

### Authentication
| ID | Story | Priority |
|---|---|---|
| US-001 | As an employee, I want to register with my name, email and password so I can access the system | Must Have |
| US-002 | As an employee, I want to log in with email and password so I can access my tasks | Must Have |
| US-003 | As an employee, I want to log out so my session is cleared on shared devices | Must Have |

### Dashboard
| ID | Story | Priority |
|---|---|---|
| US-004 | As an employee, I want to see total projects, pending tasks, high-priority tasks, and completed tasks at a glance | Must Have |
| US-005 | As an employee, I want to see a bar chart of tasks per project to understand workload distribution | Should Have |
| US-006 | As an employee, I want to see a pie chart of task status distribution | Should Have |

### Projects
| ID | Story | Priority |
|---|---|---|
| US-007 | As an employee, I want to create a project with a name, description, status, priority, and due date | Must Have |
| US-008 | As an employee, I want to view all my projects in a card layout | Must Have |
| US-009 | As an employee, I want to filter projects by status and priority | Should Have |
| US-010 | As an employee, I want to search projects by name | Should Have |
| US-011 | As an employee, I want to edit a project's details | Must Have |
| US-012 | As an employee, I want to delete a project (soft delete) | Must Have |

### Tasks/Todos
| ID | Story | Priority |
|---|---|---|
| US-013 | As an employee, I want to create a task under a project with title, description, priority, status, and due date | Must Have |
| US-014 | As an employee, I want to view all my tasks in a card layout grouped or filtered by project | Must Have |
| US-015 | As an employee, I want to filter tasks by project, status, and priority | Must Have |
| US-016 | As an employee, I want to search tasks by title using a debounced search box | Should Have |
| US-017 | As an employee, I want to edit a task's details | Must Have |
| US-018 | As an employee, I want to quickly update a task's status from the card | Must Have |
| US-019 | As an employee, I want to delete a task (soft delete) | Must Have |

### UI/UX
| ID | Story | Priority |
|---|---|---|
| US-020 | As an employee, I want to toggle between dark and light themes | Should Have |
| US-021 | As an employee, I want to see a loading spinner when data is being fetched | Must Have |
| US-022 | As an employee, I want the app to work well on my phone | Should Have |

---

## 4. Functional Requirements

### FR-001: Registration
- System validates: username (required, 2–100 chars), email (required, valid format, unique), password (min 6 chars), confirmPassword (must match)
- Password hashed using BCrypt (work factor 12) before storage
- Returns `ApiResponse<null>` with success/failure message
- Redirects to Login page on success

### FR-002: Login
- System validates credentials against DB
- BCrypt password verification
- Generates JWT token (60 min expiry) on success
- Returns token, username, email in response
- Frontend stores token in `localStorage['ica_token']`
- Redirects to Dashboard on success

### FR-003: JWT Authorization
- All Project, Todo, and Dashboard API endpoints require valid JWT
- Invalid/expired token → 401 Unauthorized → frontend redirects to Login
- Token carries UserId claim for data isolation

### FR-004: Project Management
- All operations scoped to authenticated user (UserId from JWT)
- Soft delete sets `IsDeleted = true` and `UpdatedDate = UtcNow`
- List API excludes soft-deleted records via global EF query filter
- Status values: `Active`, `OnHold`, `Completed`
- Priority values: `Low`, `Medium`, `High`

### FR-005: Task Management
- Tasks must belong to a project owned by the authenticated user
- Status values: `Pending`, `InProgress`, `Completed`
- `IsCompleted` flag set to `true` when status changes to `Completed`
- Search uses LIKE query on Title field (debounced 300ms on frontend)
- Status-only PATCH endpoint for quick card toggle

### FR-006: Dashboard
- Counts derived from DB queries filtered by UserId
- High Priority = tasks where Priority = 'High' AND IsDeleted = false
- Charts data returned as arrays in single `/api/dashboard/stats` call

### FR-007: Theme
- Default theme: light
- Toggle saved to `localStorage['ica_theme']`
- Applied on page load before content renders to prevent flash
- Uses Bootstrap 5.3 `data-bs-theme` attribute on `<html>` tag

---

## 5. Non-Functional Requirements

| Category | Requirement |
|---|---|
| Performance | API responses < 500ms for list endpoints with up to 1000 records |
| Security | Passwords never logged or returned in API responses |
| Security | JWT secrets not committed to source control (use env vars in production) |
| Scalability | Repository pattern enables future switch to Dapper or other ORM |
| Maintainability | Each layer only knows about the layer directly inward |
| Auditability | All records track CreatedDate and UpdatedDate (UTC) |
| Availability | Target 99.5% uptime on internal network |
| Browser Support | Chrome 90+, Edge 90+, Firefox 88+, Safari 14+ |
| Responsive | Functional on screens 375px wide and above |

---

## 6. Data Flow Diagrams

### 6.1 Login Flow
```
User fills Login Form
        ↓
Client-side validation (email format, password required)
        ↓
POST /api/auth/login { email, password }
        ↓
AuthController → AuthService → UserRepository
        ↓
BCrypt.Verify(inputPassword, storedHash)
        ↓ (success)
JwtHelper.GenerateToken(userId, email, username)
        ↓
ApiResponse { success:true, data: { token, username, email } }
        ↓
Frontend: localStorage.setItem('ica_token', token)
        ↓
Redirect → /Dashboard
```

### 6.2 Task Creation Flow
```
User fills New Task Modal
        ↓
Click "Save Task"
        ↓
Client-side validation
        ↓
POST /api/todos { projectId, title, description, priority, status, dueDate }
  Headers: Authorization: Bearer <token>
        ↓
TodoController → [Authorize] → JWT Middleware validates token
        ↓
Extract UserId from JWT claims
        ↓
TodoService.CreateAsync(dto, userId)
        ↓
Verify projectId belongs to userId (security check)
        ↓
TodoRepository.AddAsync(taskEntity)
        ↓
EF Core → INSERT INTO TBL_Task
        ↓
ApiResponse { success:true, message: "Task created", data: taskResponseDto }
        ↓
Frontend: close modal, reload task list
```

### 6.3 Search with Debounce Flow
```
User types in search box
        ↓
'input' event fires
        ↓
debounce(300ms) — previous timer cleared
        ↓ (after 300ms of no input)
GET /api/todos?search=keyword&projectId=1&status=Pending
        ↓
TodoRepository.GetAllAsync(filters)
  → WHERE Title LIKE '%keyword%'
  AND UserId = {userId}
  AND IsDeleted = 0
        ↓
Paginated ApiResponse
        ↓
Frontend: re-render task cards
```

---

## 7. Database Entity Relationship

```
TBL_User (1) ────────────── (N) TBL_Project
     │                              │
     │                              │
     └───── (N) TBL_Task (N) ───────┘
                (has UserId FK AND ProjectId FK)
```

- One User → Many Projects
- One Project → Many Tasks
- One User → Many Tasks (direct FK for quick user-scoped queries)

---

## 8. Development Phases

### Phase 1 — Foundation (Week 1)
- [ ] Solution setup with all projects and references
- [ ] Domain entities defined
- [ ] AppDbContext with fluent configuration
- [ ] EF Core migrations and DB creation
- [ ] Repository implementations (CRUD)
- [ ] ApiResponse wrapper class

### Phase 2 — Authentication (Week 1–2)
- [ ] RegisterDto, LoginDto, LoginResponseDto
- [ ] IAuthService + AuthService
- [ ] IUserRepository + UserRepository
- [ ] AuthController (Register + Login endpoints)
- [ ] JWT configuration in Program.cs
- [ ] Register Razor Page (UI)
- [ ] Login Razor Page (UI)

### Phase 3 — Core Features (Week 2–3)
- [ ] ProjectService + ProjectRepository
- [ ] TodoService + TodoRepository
- [ ] DashboardService
- [ ] ProjectController
- [ ] TodoController
- [ ] DashboardController

### Phase 4 — Frontend (Week 3–4)
- [ ] Shared Layout + Navbar partial
- [ ] Dashboard Page with charts
- [ ] Projects Page (CRUD, filter, search)
- [ ] Todos Page (CRUD, filter, search, debounce)
- [ ] Theme toggle
- [ ] Loading spinner
- [ ] Logout functionality

### Phase 5 — Polish & Testing (Week 4)
- [ ] Input validation (both client and server)
- [ ] Error handling and user-friendly messages
- [ ] Responsive breakpoint testing
- [ ] Dark theme QA
- [ ] Swagger API documentation
- [ ] Final review against all user stories

---

## 9. API Error Handling

| HTTP Status | When | Response |
|---|---|---|
| 200 | Success | `{ success: true, message: "...", data: ... }` |
| 400 | Validation failure or business rule violation | `{ success: false, message: "...", data: null }` |
| 401 | Missing or invalid JWT token | `{ success: false, message: "Unauthorized", data: null }` |
| 404 | Resource not found | `{ success: false, message: "Not found", data: null }` |
| 500 | Unhandled server error | `{ success: false, message: "Internal server error", data: null }` |

---

## 10. Environment Setup Guide

### Prerequisites
```
✅ .NET 8 SDK (https://dotnet.microsoft.com/download)
✅ SQL Server 2019+ or LocalDB (included with Visual Studio)
✅ Visual Studio 2022 (Community or above) OR VS Code + C# extension
✅ Node.js (optional, only if adding a build step)
```

### Step-by-Step Setup
```bash
# 1. Clone or extract the project
cd ICA_TodoApp

# 2. Restore all NuGet packages
dotnet restore

# 3. Update connection string in Todo.API/appsettings.json
#    (change Server= to your SQL Server instance)

# 4. Create the database
dotnet ef database update \
  --project SRC/Infrastructure/Persistence \
  --startup-project SRC/Presentation/Todo.API

# 5. Run the API (Terminal 1)
cd SRC/Presentation/Todo.API
dotnet run
# API available at https://localhost:7001
# Swagger UI at https://localhost:7001/swagger

# 6. Run the UI (Terminal 2)
cd SRC/Presentation/Todo.UI
dotnet run
# UI available at https://localhost:7000
```

---

## 11. Acceptance Criteria

### AC-001: Register
- [ ] Form shows validation errors without calling API
- [ ] Spinner visible during API call
- [ ] Duplicate email shows friendly error
- [ ] Successful registration redirects to Login

### AC-002: Login
- [ ] Wrong credentials show error message
- [ ] Successful login stores token in localStorage
- [ ] Redirects to Dashboard after login
- [ ] Back button after logout does not show dashboard (redirect back to login)

### AC-003: Projects
- [ ] New project appears in list immediately after creation
- [ ] Edit modal pre-fills existing values
- [ ] Deleted project disappears from list
- [ ] Status/Priority filter narrows results correctly
- [ ] Search filters by project name

### AC-004: Tasks
- [ ] New task appears in list immediately
- [ ] Task filtered by correct project
- [ ] Search debounces (no API call on every keystroke)
- [ ] Status toggle updates card badge without page reload
- [ ] Deleted task disappears from list

### AC-005: Dashboard
- [ ] 4 KPI cards show accurate counts
- [ ] Bar chart renders with correct project names
- [ ] Pie chart shows task status breakdown
- [ ] Charts update on page reload after task changes

### AC-006: Theme
- [ ] Toggle switches between light and dark instantly
- [ ] Theme persists on page refresh
- [ ] All pages respect saved theme

### AC-007: Security
- [ ] Accessing /Dashboard without login redirects to Login
- [ ] API returns 401 for requests without valid JWT
- [ ] Logout clears token and redirects to Login

---

## 12. File Naming Conventions

| Type | Convention | Example |
|---|---|---|
| Entities | PascalCase | `User.cs`, `Project.cs`, `TodoTask.cs` |
| DTOs | `{Name}Dto.cs` | `RegisterDto.cs`, `ProjectCreateDto.cs` |
| Interfaces | `I{Name}.cs` | `IUserRepository.cs`, `IAuthService.cs` |
| Services | `{Name}Service.cs` | `AuthService.cs`, `TodoService.cs` |
| Repositories | `{Name}Repository.cs` | `UserRepository.cs` |
| Controllers | `{Name}Controller.cs` | `AuthController.cs` |
| Razor Pages | `{Name}.cshtml` + `{Name}.cshtml.cs` | `Login.cshtml` |
| JS files | `camelCase.js` | `auth.js`, `todos.js` |
| CSS | `kebab-case.css` | `site.css`, `theme.css` |

---

## 13. Glossary

| Term | Definition |
|---|---|
| Clean Architecture | Software design pattern with concentric layers; dependencies point inward toward domain |
| JWT | JSON Web Token — stateless auth token carrying user claims |
| Soft Delete | Marking a record as deleted (`IsDeleted=true`) without removing it from the database |
| BCrypt | Password hashing algorithm with configurable work factor for brute-force resistance |
| DTO | Data Transfer Object — class used to move data between layers without exposing entities |
| Repository Pattern | Abstraction layer between business logic and data access code |
| Debouncing | Delaying a function call until a pause in input events to reduce API request frequency |
| EF Core | Entity Framework Core — Microsoft's ORM for .NET |
| Code-First | EF approach where C# classes define the DB schema, migrations generate SQL |
| KPI Card | Dashboard widget showing a single key performance metric |
