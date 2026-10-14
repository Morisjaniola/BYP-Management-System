# BYProgram — Barangay Youth Program Management System

ASP.NET Core MVC (.NET 9) application for managing barangay youth programs:
participants, programs, registrations, attendance (with photo proof),
certificates, reports, notifications, and role-based user management
(Admin / Staff / Youth).

## Technology

- ASP.NET Core MVC, .NET 9
- Entity Framework Core + Pomelo MySQL provider
- ASP.NET Identity (cookie authentication, role authorization)
- Razor Views, custom CSS/JS (Light & Dark themes)

## Local setup

1. Restore packages and build:

   ```
   dotnet restore
   dotnet build
   ```

2. Create your configuration file:

   ```
   copy appsettings.example.json appsettings.json
   ```

   Then edit `appsettings.json` (it is git-ignored) and replace every
   `<placeholder>` with your own local database credentials.

3. Set the development seed-account passwords in the local secret store
   (never in source code):

   ```
   dotnet user-secrets set "SeedAccounts:AdminPassword" <your-value>
   dotnet user-secrets set "SeedAccounts:StaffPassword" <your-value>
   ```

   If these are not set, the application still runs, but it skips creating
   the development admin/staff accounts on a fresh database.

4. Run:

   ```
   dotnet run --launch-profile http
   ```

   The application opens at `http://localhost:5009`.

## Configuration notes

- The database connection string lives in `appsettings.json` (git-ignored)
  or can be overridden with the environment variable
  `ConnectionStrings__DefaultConnection`.
- Uploaded attendance photos are stored in `App_Data/AttendancePhotos`
  (outside `wwwroot`), are never served as static files, and are only
  delivered through controller actions that verify identity and ownership.
- Profile images are stored in `wwwroot/uploads/profile-images` and are
  intentionally public (avatars).
- Development account seeding runs only when
  `ASPNETCORE_ENVIRONMENT=Development` and is idempotent.

## Repository hygiene

`.gitignore` excludes `bin/`, `obj/`, `.vs/`, `appsettings.*.json`
(except the example), `.env` files, logs, databases, SQL/ZIP backups,
and uploaded private photos. Do not commit real credentials or user data.
