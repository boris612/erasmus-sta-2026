# Topic 1

`Topic1` is an ASP.NET Core MVC sample application for managing sports events data. It uses PostgreSQL in normal application runs, HTMX for partial page updates, and a mix of unit, integration, and UI tests to cover the main workflows.

## Solution Overview

The solution file is [Topic1.sln](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Topic1.sln).

Projects:

- [Events.MVC](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC): the main ASP.NET Core MVC application
- [Events.EFModel](https://github.com/boris612/erasmus-sta-2026/tree/main/Events.EFModel): shared EF Core data model used by the MVC app and tests
- [Events.Tests.UnitTests](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests/Events.Tests.UnitTests): unit tests for controllers, paging helpers, mapping, and provider-specific examples
- [Events.Tests.IntegrationTests](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests/Events.Tests.IntegrationTests): HTTP-level integration tests using `WebApplicationFactory`
- [Events.Tests.UITests](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests/Events.Tests.UITests): Playwright UI tests

## Folder Guide

Inside [Events.MVC](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC):

- [Controllers](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC/Controllers): MVC controllers for screens and CRUD actions
- [Models](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC/Models): view models, paging settings, and UI-facing models
- [Views](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC/Views): Razor views and HTMX partials
- [Util](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC/Util): middleware, helpers, and test seeding support
- [wwwroot](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC/wwwroot): static files
- [Program.cs](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC/Program.cs): application startup and service registration

Inside [Tests](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests):

- [Events.Tests.UnitTests](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests/Events.Tests.UnitTests): fast isolated tests, mostly using EF Core `InMemory`
- [Events.Tests.IntegrationTests](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests/Events.Tests.IntegrationTests): end-to-end MVC request tests with in-memory hosting
- [Events.Tests.UITests](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests/Events.Tests.UITests): browser tests that start the MVC app and exercise the UI
- [README.md](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests/README.md): additional notes about test scope

## Prerequisites

- .NET SDK 8.0
- PostgreSQL
- Docker Desktop, if you want to run the provided PostgreSQL containers
- Node.js is not required

Optional for UI tests:

- Playwright CLI
- Chromium installed through Playwright

## Configuration

The MVC app uses the `EventDB` connection string from [appsettings.json](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC/appsettings.json) and user secrets.

The project already uses this user secrets id:

```text
Erasmus-STA-2026
```

Set the main application connection string with:

```powershell
dotnet user-secrets set "ConnectionStrings:EventDB" "Host=localhost;Port=5432;Database=events;Username=sport;Password=your-password;Persist Security Info=True;" --project Topic1\Events.MVC\Events.MVC.csproj
```

The unit test project also reads configuration from its own [appsettings.json](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests/Events.Tests.UnitTests/appsettings.json) and shares the same user secrets id. The PostgreSQL-backed provider test expects:

```text
ConnectionStrings:EventDB-Test
```

Example:

```powershell
dotnet user-secrets set "ConnectionStrings:EventDB-Test" "Host=localhost;Port=5433;Database=events;Username=sport;Password=your-password;Persist Security Info=True;" --project Topic1\Tests\Events.Tests.UnitTests\Events.Tests.UnitTests.csproj
```

## Running PostgreSQL

Two Docker setups are provided:

- [docker-definitions/postgres-eventsdb](docker-definitions/postgres-eventsdb): the main local database
- [docker-definitions/postgres-eventsdb-test](docker-definitions/postgres-eventsdb-test): the additional test database copy

From the repository root, start the main database:

```powershell
docker compose -f docker-definitions\postgres-eventsdb\docker-compose.yml up -d
```

Start the test database:

```powershell
docker compose -f docker-definitions\postgres-eventsdb-test\docker-compose.yml up -d
```

The application database typically runs on port `5432`, while the extra test database copy is intended to run on port `5433`.

## Running the Application

From the repository root:

```powershell
dotnet restore Topic1\Topic1.sln
dotnet build Topic1\Topic1.sln
dotnet run --project Topic1\Events.MVC\Events.MVC.csproj
```

Then open the URL printed by ASP.NET Core, usually:

```text
https://localhost:xxxx
```

Main screens:

- Home
- Sports
- Countries
- People
- Events
- Registrations

## Test Strategy

The test suite intentionally uses different storage setups depending on the goal:

- Unit tests mostly use EF Core `InMemory` for fast isolated checks
- Integration tests host the MVC application with `InMemory` through [CustomWebApplicationFactory.cs](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Tests/Events.Tests.IntegrationTests/Infrastructure/CustomWebApplicationFactory.cs)
- UI tests run the MVC app against the PostgreSQL test database configured through `EventDB-Test`
- One provider-specific unit test uses PostgreSQL directly to demonstrate behavior that `InMemory` cannot reproduce, such as `EF.Functions.ILike(...)`

This is important because some behaviors cannot be realistically tested in plain unit tests. Examples:

- MVC data annotation validation does not automatically populate `ModelState` in plain controller unit tests
- PostgreSQL-specific queries such as `ILike` do not run on EF Core `InMemory`

## Running Tests

Run all Topic 1 tests:

```powershell
dotnet test Topic1\Topic1.sln
```

Run only unit tests:

```powershell
dotnet test Topic1\Tests\Events.Tests.UnitTests\Events.Tests.UnitTests.csproj
```

Run only integration tests:

```powershell
dotnet test Topic1\Tests\Events.Tests.IntegrationTests\Events.Tests.IntegrationTests.csproj
```

Run only UI tests:

```powershell
dotnet test Topic1\Tests\Events.Tests.UITests\Events.Tests.UITests.csproj
```

Run the PostgreSQL-backed provider test only:

```powershell
dotnet test Topic1\Tests\Events.Tests.UnitTests\Events.Tests.UnitTests.csproj --filter ProviderSpecificQueryShould.ExecuteILikeWhenUsingPostgreSqlProvider
```

## Playwright Setup

UI tests require Playwright browsers. Install them once:

```powershell
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

The UI harness starts the MVC app automatically with an in-memory database, so no PostgreSQL instance is required for UI test execution.

## HTMX and Validation Notes

The MVC application uses HTMX for partial updates. Validation is currently enforced on the server side. That means:

- invalid form submissions are handled by controller actions and returned partial views
- integration tests are the correct place to verify real MVC validation messages
- plain unit tests are useful for controller branching logic, but not for full MVC validation pipeline behavior

If client-side validation is added later, HTMX-updated forms will need unobtrusive validation to be reparsed after swaps.

## Troubleshooting

- If the MVC app cannot connect to PostgreSQL, verify the `EventDB` connection string and the running container port
- If `ExecuteILikeWhenUsingPostgreSqlProvider` fails, verify `ConnectionStrings:EventDB-Test` and confirm the test database is reachable on port `5433`
- If UI tests fail to start the app, verify that `ConnectionStrings:EventDB-Test` is configured and points to the PostgreSQL copy used for UI testing
- If UI tests fail at browser startup, run `playwright install`
- If tests fail because assemblies are locked, make sure no old `dotnet run` process is still holding files from [Events.MVC](https://github.com/boris612/erasmus-sta-2026/tree/main/Topic1/Events.MVC)
