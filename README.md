# Erasmus STA 2026 Teaching Materials

This repository contains teaching materials and runnable sample code prepared for the lectures:

- `T1: Unit Testing and Integration Testing in ASP.NET Core` (31.3.2026.)
- `T2: Web API Integration Testing in Complex Scenarios` (2.4.2026.)

These materials were created for teaching activities delivered during the Erasmus+ `KA131 STA` mobility at the Faculty of Computer Science and Engineering, Ss. Cyril and Methodius University of Skopje.

The README files in this repository were scaffolded and expanded with the help of Codex GPT-5.4.

## Repository Structure

- [Topic1](Topic1/README.md): ASP.NET Core MVC sample focused on unit, integration, and UI testing
- [Topic2](Topic2/README.md): ASP.NET Core Web API, client app, messaging, and benchmark examples
- [DataGenerator](DataGenerator/README.md): console tool that generates SQL seed data for people
- [Events.EFModel](Events.EFModel/README.md): shared EF Core database model
- [docker-definitions](docker-definitions/README.md): local PostgreSQL container definitions and initialization scripts
- [slides](slides/README.md): exported slide decks for the two lectures

## What The Examples Demonstrate

- `Topic1` shows how to combine plain unit tests, HTTP-level integration tests, and browser-based UI tests in an ASP.NET Core MVC application.
- `Topic2` shows API testing in more complex scenarios that include JWT authentication, authorization scopes, MediatR, MassTransit consumers, generated output files, and performance benchmarks.
- The support folders provide shared database schema, seed data, and helper tooling used by both topics.

## Prerequisites

- .NET SDK 8.0
- Docker Desktop, or another Docker-compatible environment
- PostgreSQL client tools are optional but helpful for inspecting the database
- Node.js 20+ for [Topic2/Events.ClientApp](Topic2/Events.ClientApp/README.md)
- Playwright CLI for Topic 1 UI tests
- A RabbitMQ instance if you want to run the full Topic 2 Web API with real message-bus integration
- An Auth0 tenant if you want to exercise the secured Topic 2 client and bearer-token scenarios outside the test suite

## Shared Configuration And Secrets

Several .NET projects in this repository use the same `UserSecretsId`:

```text
Erasmus-STA-2026
```

The following projects rely on that shared secret store:

- [Topic1/Events.MVC](Topic1/Events.MVC/Events.MVC.csproj)
- [Topic1/Tests/Events.Tests.UnitTests](Topic1/Tests/Events.Tests.UnitTests/Events.Tests.UnitTests.csproj)
- [Topic2/Events.WebAPI](Topic2/Events.WebAPI/Events.WebAPI.csproj)
- [DataGenerator](DataGenerator/DataGenerator.csproj)

Set the main PostgreSQL connection string once:

```powershell
dotnet user-secrets set "ConnectionStrings:EventDB" "Host=localhost;Port=5432;Database=events;Username=sport;Password=your-password;Persist Security Info=True;" --project Topic1\Events.MVC\Events.MVC.csproj
```

Set the additional test database connection string used by Topic 1 PostgreSQL-backed tests:

```powershell
dotnet user-secrets set "ConnectionStrings:EventDB-Test" "Host=localhost;Port=5433;Database=events;Username=sport;Password=your-password;Persist Security Info=True;" --project Topic1\Tests\Events.Tests.UnitTests\Events.Tests.UnitTests.csproj
```

Because the `UserSecretsId` is shared, those values are also available to the other .NET projects that use the same id.

Do not commit real secrets to the repository. Prefer:

- `.env.example` files for client-side configuration templates
- `.env` files only for local-only infrastructure setup
- `dotnet user-secrets` for local development credentials and connection strings

## Configuration Before Running

Before starting the examples, make sure the required configuration is in place.

### Topic 1

- set `ConnectionStrings:EventDB` for the MVC app
- if you want to run Topic 1 UI tests or PostgreSQL-specific provider tests, also set `ConnectionStrings:EventDB-Test`

### Topic 2

- set `ConnectionStrings:EventDB` for `Topic2/Events.WebAPI`
- the API already uses these Auth defaults in [appsettings.json](/Users/boris/gitrepos/erasmus-sta-2026/Topic2/Events.WebAPI/appsettings.json):
  `Auth:Authority=https://fer-web2.eu.auth0.com/`
  `Auth:Audience=https://erasmus-sta-2026/events-api`
- before the first SPA run, copy `Topic2/Events.ClientApp/.env.example` to `Topic2/Events.ClientApp/.env.local`
- the client example values use:
  `VITE_AUTH0_DOMAIN=fer-web2.eu.auth0.com`
  `VITE_AUTH0_CLIENT_ID=whed5Hdb8l1b1fGyyAz7Qrdsb2oKcSh3`
  `VITE_AUTH0_AUDIENCE=https://erasmus-sta-2026/events-api`
  `VITE_API_BASE_URL=https://localhost:7150`
- if you want the full Topic 2 app with real messaging, RabbitMQ should be available on `localhost`

## Local Database Setup

The repository includes two PostgreSQL Docker setups:

- [docker-definitions/postgres-eventsdb](docker-definitions/postgres-eventsdb): the main database on port `5432`
- [docker-definitions/postgres-eventsdb-test](docker-definitions/postgres-eventsdb-test): the additional test database on port `5433`

From the repository root you can manage them through the helper scripts in [commands](/Users/boris/gitrepos/erasmus-sta-2026/commands):

```sh
./commands/db.sh up -d
./commands/db-test.sh up -d
```

The init scripts inside each container definition create schema objects and seed example data used across the teaching materials.

## Quick Start

### Topic 1

```sh
./commands/db.sh up
./commands/topic1.sh
```

When you are done, you can stop the main database:

```sh
./commands/db.sh down
```

If you also want to remove the volume:

```sh
./commands/db.sh down -v
```

See [Topic1/README.md](Topic1/README.md) for full setup, test types, and troubleshooting.

### Topic 2

Start PostgreSQL first, then start RabbitMQ:

```sh
./commands/db.sh up
./commands/start-bus.sh
```

Before the first Topic 2 run, install the client dependencies once:

```sh
cd Topic2/Events.ClientApp
npm install
```

Then start the full Topic 2 application:

```sh
./commands/topic2.sh
```

When you are done, stop the main database:

```sh
./commands/db.sh down
```

Or remove it together with the volume:

```sh
./commands/db.sh down -v
```

If RabbitMQ is still running in another terminal, stop it with:

```sh
./commands/stop-bus.sh
```

See [Topic2/README.md](Topic2/README.md) for API configuration, Auth0 variables, test setup, and benchmark details.

### RabbitMQ Bus

If you only want to start the message bus used by Topic 2, run:

```sh
./commands/start-bus.sh
```

To stop it from another terminal, run:

```sh
./commands/stop-bus.sh
```

The start script runs `rabbitmq:4-management` in interactive mode with `--rm`, so the container is removed after shutdown.

### Topic 1 Tests

Start the test database first only for Topic 1 UI tests and PostgreSQL-specific tests:

```sh
./commands/db-test.sh up
```

Then run the desired test suite:

```sh
./commands/topic1-test-unit.sh
./commands/topic1-test-integration.sh
./commands/topic1-test-ui.sh
```

The extra test database is mainly needed for:

- `./commands/topic1-test-ui.sh`
- PostgreSQL-specific Topic 1 tests such as the `EF.Functions.ILike(...)` provider example

Most other Topic 1 unit and integration tests do not require `db-test.sh` because they use in-memory test infrastructure.

When you are done, stop the test database:

```sh
./commands/db-test.sh down
```

Or remove it together with the volume:

```sh
./commands/db-test.sh down -v
```

### Topic 2 Tests

Run the Topic 2 integration tests with:

```sh
./commands/topic2-test.sh
```

If you want the main PostgreSQL container running first, start it with:

```sh
./commands/db.sh up
```

When you are done, stop it with:

```sh
./commands/db.sh down
```

### Topic 2 Benchmarks

Run Topic 2 benchmarks with:

```sh
./commands/benchmark.sh 1
```

Before running benchmarks, make sure the required infrastructure is available:

- for database-backed benchmarks, Docker must be running because the benchmark project starts PostgreSQL through Testcontainers
- for RabbitMQ benchmarks such as `4a`, Docker must also be running because the benchmark project starts RabbitMQ through Testcontainers

You do not need to start `./commands/db.sh`, `./commands/db-test.sh`, or `./commands/start-bus.sh` manually for the benchmark project unless you intentionally want your own separate infrastructure running.

## Documentation Guide

The most important folder-level README files are:

- [Topic1/README.md](Topic1/README.md)
- [Topic1/Tests/README.md](Topic1/Tests/README.md)
- [Topic1/Tests/Events.Tests.UITests/README.md](Topic1/Tests/Events.Tests.UITests/README.md)
- [Topic2/README.md](Topic2/README.md)
- [Topic2/Tests/README.md](Topic2/Tests/README.md)
- [Topic2/Benchmarks/README.md](Topic2/Benchmarks/README.md)
- [Topic2/Events.ClientApp/README.md](Topic2/Events.ClientApp/README.md)
- [DataGenerator/README.md](DataGenerator/README.md)
- [Events.EFModel/README.md](Events.EFModel/README.md)
- [docker-definitions/README.md](docker-definitions/README.md)
- [slides/README.md](slides/README.md)

## License

This repository is licensed under [CC BY 4.0](LICENSE).
