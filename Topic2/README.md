# Topic 2

`Topic2` contains the sample code used for the lecture `Web API Integration Testing in Complex Scenarios`. The materials were prepared for Erasmus+ `KA131 STA` teaching activities at the Faculty of Computer Science and Engineering, Ss. Cyril and Methodius University of Skopje.

The topic is centered around an ASP.NET Core Web API and demonstrates integration testing across authentication, authorization scopes, database-backed handlers, messaging workflows, generated files, and benchmarks.

## Solution Overview

The main solution file is [Topic2.sln](Topic2/Topic2.sln).

Projects:

- [Events.WebAPI](Topic2/Events.WebAPI): the ASP.NET Core Web API entry point
- [Events.WebAPI.Contract](Topic2/Events.WebAPI.Contract): shared DTOs, commands, queries, messages, and service contracts
- [Events.WebAPI.Handlers.EF](Topic2/Events.WebAPI.Handlers.EF): EF Core entities, mappings, command handlers, and query handlers
- [Events.WebAPI.CertificateCreator](Topic2/Events.WebAPI.CertificateCreator): certificate file generation service
- [Events.WebAPI.ExcelExporter](Topic2/Events.WebAPI.ExcelExporter): Excel export generation service
- [Events.ClientApp](Topic2/Events.ClientApp): Vue 3 client application
- [Tests](Topic2/Tests/README.md): integration tests and test-host infrastructure
- [Benchmarks](Topic2/Benchmarks/README.md): BenchmarkDotNet performance experiments

## What This Topic Demonstrates

- CRUD-style Web API endpoints for sports, people, events, and registrations
- lookup endpoints for client-side dropdowns and search flows
- JWT bearer authentication and authorization by scope
- MediatR-based command and query dispatch
- FluentValidation pipeline validation
- MassTransit consumers for side effects triggered by registration changes
- generated certificates and Excel files
- benchmark comparisons for projections, inserts, dispatching, and messaging strategies

## API Surface

Main controllers in [Events.WebAPI/Controllers](Topic2/Events.WebAPI/Controllers):

- `SportsController`
- `PeopleController`
- `EventsController`
- `RegistrationsController`
- `LookupController`

Special endpoints include:

- `GET /Events/{id}/RegistrationsExcel`
- `GET /Registrations/{id}/Certificate`
- lookup endpoints under `/Lookup/...`

Swagger UI is exposed at:

```text
https://localhost:7150/docs
```

The exact port may vary depending on your local launch profile.

## Prerequisites

- .NET SDK 8.0
- Docker Desktop
- PostgreSQL, usually via [docker-definitions](docker-definitions/README.md)
- RabbitMQ if you want to run the full API with its real MassTransit transport
- Node.js 20+ for the client app
- An Auth0 tenant if you want to run real bearer-token and browser-login flows outside the test suite

## Configuration

`Events.WebAPI` reads settings from:

- [Events.WebAPI/appsettings.json](Topic2/Events.WebAPI/appsettings.json)
- [Events.WebAPI/appsettings.Development.json](Topic2/Events.WebAPI/appsettings.Development.json)
- the shared .NET user secrets store with id `Erasmus-STA-2026`

Important configuration sections:

- `ConnectionStrings:EventDB`
- `RabbitMq:Host`
- `RabbitMq:Username`
- `RabbitMq:Password`
- `Auth:Authority`
- `Auth:Audience`
- `Paths:Certificates`

The current Auth configuration in [Events.WebAPI/appsettings.json](Topic2/Events.WebAPI/appsettings.json) is:

- `Auth:Authority=https://fer-web2.eu.auth0.com/`
- `Auth:Audience=https://erasmus-sta-2026/events-api`

Set the PostgreSQL connection string:

```powershell
dotnet user-secrets set "ConnectionStrings:EventDB" "Host=localhost;Port=5432;Database=events;Username=sport;Password=your-password;Persist Security Info=True;" --project Topic2\Events.WebAPI\Events.WebAPI.csproj
```

You can also override RabbitMQ and Auth settings with user secrets if you do not want to keep local values in `appsettings.json`.

For the SPA client, copy `Topic2/Events.ClientApp/.env.example` to `.env.local`. The example file already contains the current Auth0 values used by this repository:

- `VITE_AUTH0_DOMAIN=fer-web2.eu.auth0.com`
- `VITE_AUTH0_CLIENT_ID=whed5Hdb8l1b1fGyyAz7Qrdsb2oKcSh3`
- `VITE_AUTH0_AUDIENCE=https://erasmus-sta-2026/events-api`

`Paths:Certificates` points to the directory where generated certificates and Excel files are stored. By default it is:

```text
./Certificates
```

## Running Required Infrastructure

Start PostgreSQL using the repository Docker definitions:

```powershell
docker compose -f docker-definitions\postgres-eventsdb\docker-compose.yml up -d
```

Start RabbitMQ if you want the API to use its real MassTransit transport:

```powershell
docker run -d --name rabbitmq-erasmus-sta -p 5672:5672 -p 15672:15672 rabbitmq:4-management
```

The RabbitMQ management UI is usually available at:

```text
http://localhost:15672
```

## Running The Web API

```powershell
dotnet restore Topic2\Topic2.sln
dotnet build Topic2\Topic2.sln
dotnet run --project Topic2\Events.WebAPI\Events.WebAPI.csproj
```

Once the API is running:

- open Swagger at `/docs`
- test anonymous lookup endpoints
- test secured endpoints with a valid bearer token if your Auth0 configuration is set

## Running The Client App

See [Events.ClientApp/README.md](Topic2/Events.ClientApp/README.md) for full details.

Typical local flow:

```powershell
cd Topic2\Events.ClientApp
npm install
npm run dev
```

The client expects:

- `VITE_API_BASE_URL` pointing to the running API
- Auth0 SPA settings if login is enabled

## Testing

See [Tests/README.md](Topic2/Tests/README.md) for details.

Run all Topic 2 tests:

```powershell
dotnet test Topic2\Tests\Events.WebAPI.IntegrationTests\Events.WebAPI.IntegrationTests.csproj
```

The integration test suite covers:

- authorization and scope behavior
- database-backed endpoint flows
- lookup endpoints
- messaging side effects
- consumer behavior
- JWT bearer validation scenarios

Some tests use in-memory infrastructure, while PostgreSQL-backed factories and Testcontainers are used where realistic behavior matters.

## Benchmarks

See [Benchmarks/README.md](Topic2/Benchmarks/README.md) for available benchmark groups and how to run them.

## Troubleshooting

- If the API fails at startup, verify `ConnectionStrings:EventDB`, RabbitMQ connectivity, and `Paths:Certificates`
- If Swagger opens but secured requests fail, verify `Auth:Authority`, `Auth:Audience`, and the token scopes
- If the client loads but cannot authenticate, verify the values in `.env.local`
- If generated certificates or Excel exports are missing, verify that the output directory exists and is writable
