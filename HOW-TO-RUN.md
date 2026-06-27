# ICA Task Tracker — Quick Start

## Prerequisites
- .NET 8 SDK
- SQL Server or LocalDB (included with Visual Studio)

## 1. Restore packages
```
dotnet restore ICA_TodoApp.sln
```

## 2. Update connection string
Edit `SRC/Presentation/Todo.API/appsettings.json` — change `Server=(localdb)\\mssqllocaldb` to your SQL Server instance if needed.

## 3. Create the database (run once)
```
dotnet ef migrations add InitialCreate --project SRC\Infrastructure\Persistence --startup-project SRC\Presentation\Todo.API --output-dir Migrations
dotnet ef database update --project SRC\Infrastructure\Persistence --startup-project SRC\Presentation\Todo.API
```

## 4. Run the API  (Terminal 1)
```
cd SRC\Presentation\Todo.API
dotnet run
```
API: https://localhost:7001  
Swagger: https://localhost:7001/swagger

## 5. Run the UI  (Terminal 2)
```
cd SRC\Presentation\Todo.UI
dotnet run
```
UI: https://localhost:7000

## Default routes
| Page      | URL                            |
|-----------|--------------------------------|
| Register  | /Account/Register              |
| Login     | /Account/Login                 |
| Dashboard | /Dashboard                     |
| Projects  | /Projects                      |
| Tasks     | /Todos                         |
