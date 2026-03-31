# Tests

This folder contains test projects for Topic 2.

Current project:

- `Events.WebAPI.IntegrationTests`

## Events.WebAPI.IntegrationTests

This project contains integration tests for the Web API and its surrounding infrastructure.

Based on the current project structure, it includes:

- application factory infrastructure under `ApplicationFactories\Common`
- JWT bearer test setup under `ApplicationFactories\JwtBearer`
- MassTransit messaging test setup under `ApplicationFactories\Messaging`
- test-auth based factories under `ApplicationFactories\TestAuth`

The project uses:

- `Microsoft.AspNetCore.Mvc.Testing` for in-process application hosting
- `MassTransit.TestFramework` for messaging-oriented tests
- `Microsoft.EntityFrameworkCore.InMemory` for lightweight test database scenarios
- `Testcontainers.PostgreSql` for PostgreSQL-backed integration tests
- `xUnit` as the test framework

## What The Tests Cover

The test suite is designed to demonstrate different integration testing strategies for a Web API in realistic conditions:

- authorization behavior for anonymous, read-only, and write-capable callers
- JWT bearer token validation, including valid and invalid audience scenarios
- PostgreSQL-backed API behavior for CRUD and lookup flows
- message publication from registration commands
- end-to-end host-level messaging flows
- consumer side effects for certificate synchronization and Excel export synchronization

Test classes are grouped under:

- `Tests\SportsController`
- `Tests\PeopleController`
- `Tests\EventsController`
- `Tests\RegistrationsController`
- `Tests\LookupController`
- `Tests\Messaging`

## Test Infrastructure

The custom factories under `ApplicationFactories` intentionally use different setups depending on the scenario:

- `TestAuthWebApplicationFactory`
  Replaces real JWT authentication with a test auth handler and uses PostgreSQL-backed hosting for realistic endpoint tests.

- `TestAuthInMemoryWebApplicationFactory`
  Uses in-memory persistence where a lightweight host is enough.

- `JwtBearerWebApplicationFactory`
  Exercises real JWT bearer middleware behavior with test-generated tokens.

- `MessagingWebApplicationFactory`
  Swaps the real transport with the MassTransit test harness so message publishing and consumer flows can be verified in-process.

## Prerequisites

- .NET SDK 8.0
- Docker Desktop for PostgreSQL Testcontainers-based scenarios

No Auth0 tenant or RabbitMQ instance is required for the test suite because the tests provide their own controlled authentication and messaging infrastructure where needed.

## Running

Run all Topic 2 integration tests:

```powershell
dotnet test Topic2\Tests\Events.WebAPI.IntegrationTests\Events.WebAPI.IntegrationTests.csproj
```

Run a subset by test class name:

```powershell
dotnet test Topic2\Tests\Events.WebAPI.IntegrationTests\Events.WebAPI.IntegrationTests.csproj --filter UserWithWriteScopeShould
```

## Troubleshooting

- If PostgreSQL-backed tests fail to start, verify Docker Desktop is running
- If bearer-token tests fail unexpectedly, make sure no local changes override the test authentication setup
- If messaging tests fail, check whether the test host still references the real transport instead of the MassTransit test harness
