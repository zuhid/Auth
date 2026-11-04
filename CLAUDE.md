# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

- `Auth/` — ASP.NET Core (.NET 10) identity API, assembly/namespace `Zuhid.Auth`. Runs on `http://localhost:5101`.
- `Auth.Tests/` — xUnit integration tests (`Zuhid.Auth.Tests`).
- `Web.Vue/` — Vue 3 SPA frontend on `http://localhost:5201`. Its conventions live in the `vue-dev` and `vue-test` agent instructions (`.claude/agents/`).
- `Auth.slnx` — the solution file, which contains only the two .NET projects.

## Subagents

Project subagents live in `.claude/agents/`. Delegate work to the one that owns the area:

- `api-dev` — implements and changes the API in `Auth/`.
- `api-test` — writes and runs the xUnit integration tests in `Auth.Tests/`; reports bugs instead of changing `Auth/`.
- `vue-dev` — implements and changes the Vue SPA in `Web.Vue/src`.
- `vue-test` — writes and runs the Playwright e2e tests in `Web.Vue/tests`; reports bugs instead of changing `Web.Vue/src`.

When a developer agent changes an API contract (routes, DTOs, status codes) or UI selectors, follow up with the matching test agent, and with `vue-dev` for API contract changes.

## Sibling repo dependency

`Auth.csproj` references `../../Base/Base/Base.csproj` in **Debug**, and the `Zuhid.Base` NuGet package in **Release**. So a Debug build needs the `Base` repo checked out next to this one (`../Base`). Much of the plumbing lives there rather than here: `AddServices` (JWT bearer auth, controllers, CORS, Swagger), `BuildServices`, `AddPostgres`, `AddScopedByConvention`, `ApiClient`, `SettingBase`, `ToSnakeCase`, `LoadCsvData`, and the MVC `ActionFilter`/`ExceptionFilter`. When behavior isn't explained by this repo, look in `../Base/Base/`.

## Commands

```sh
dotnet tool restore                      # dotnet-ef, reportgenerator (pinned in dotnet-tools.json)
dotnet build Auth.slnx
dotnet run --project Auth                # http://localhost:5101, Swagger at /swagger
dotnet test Auth.Tests
dotnet test Auth.Tests --filter "FullyQualifiedName~AccountTest.Login_With_Two_Factor_Email"   # single test
```

`scripts.sh` collects helper functions that you enable by uncommenting the calls at the bottom:
- `docker_install_postgres`: PostGIS container on 5432 plus a `dbuser` superuser.
- `generate_rsa_keys Auth`: writes `Auth/Keys/private.key` and `public.key`. `Keys/` is gitignored and the app fails at startup without it.
- `update_database Auth Auth`: **deletes `Auth/Migrations`**, re-adds a single `Initial` migration, writes `Migrations/Script.sql`, then drops and recreates the DB. The project keeps one squashed migration instead of a migration history.
- `run_test Auth.Tests`: tests with coverlet coverage plus an HTML report in `Auth.Tests/TestResults/CoverageReport`.

## Configuration

- In the connection strings in `appsettings.Development.json`, the `[postgres_credential]` placeholder is replaced by the `postgres_credential` config value (see `AppSetting.ReplaceCredential`). It comes from user secrets (`UserSecretsId` is set in `Auth.csproj`). Example value: `User Id=dbuser;Password=...`.
- `AppUrl` points at the Vue app.
- Outbound email and SMS go through a separate Notification service at `Notification:BaseUrl` (`http://localhost:5102`), via `NotificationApiClient`. This API never sends mail itself.

## Architecture

- **Feature folders.** `Account/` covers self-service work: register, email/phone verification, login, 2FA with Authenticator/Email/Phone providers, and JWT issuance in `JwtService` (RSA-signed with the keys in `Keys/`). `User/` is admin-only CRUD and stats (`[Authorize(Roles = "Admin")]`). `Entities/` holds the ASP.NET Identity types keyed by `Guid`. `Notification/` holds the client for the notification service and its message DTOs.
- **DI by naming convention.** `Program.cs` registers every concrete class whose name ends in `Mapper`, `Repository`, `Validator` or `ApiClient` as scoped. A new class with one of those suffixes is injectable without any registration. `JwtService` is the one class registered explicitly.
- **Error flow.** Controllers usually return `Task` and push errors into `ModelState` (repositories return `List<KeyValuePair<code, description>>?`). Base's `ActionFilter` runs *after* the action and turns any invalid ModelState into a 400, so a "void" action can still fail. Unhandled exceptions become a generic 500 through `ExceptionFilter`.
- **EF conventions.** `AuthContext` puts every table in the `auth` schema and calls `ToSnakeCase()`, so tables, columns, keys and indexes are snake_case (for example `auth.users`). `AddPostgres` sets `QueryTrackingBehavior.NoTracking` globally, so you must attach or update entities explicitly. Identity's `UserManager` does this for you.
- **Seed data.** The CSVs in `Dataload/Entities/<Entity>.csv` are loaded through `HasData` **only at design time** (`EF.IsDesignTime`), which bakes them into the migration. Editing a CSV has no effect until you regenerate the migration. Seeded users include `admin@example.com` (Admin role).
- `User.Theme` is persisted per user and returned with the login token. The frontend's fixed `light | dark` theme ids depend on it.

## Tests

`Auth.Tests/Integration` uses `WebApplicationFactory<Program>` against the real pipeline and the real database, with no mocks of the HTTP layer. Before running them, start Postgres (with the `auth` DB migrated), the Notification service on 5102, and the mail catcher on 8025 (MailHog/Mailpit-compatible API). Tests read verification and 2FA codes out of the mail catcher, and SMS messages arrive in the `phone@test.com` inbox. Coverage excludes `AuthContext`, `Migrations` and `Program` (`coverlet.runsettings`).
