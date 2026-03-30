# Events.EFModel

`Events.EFModel` contains the shared EF Core model used across the samples in this repository.

It represents the core sports-events domain and is consumed by:

- Topic 1 MVC application and tests
- the DataGenerator tool
- Topic 2 handler and API layers

## What It Contains

- [Models/EventsContext.cs](Events.EFModel/Models/EventsContext.cs): the EF Core `DbContext`
- entity classes for `Sport`, `Country`, `Person`, `Event`, and `Registration`
- [efpt.config.json](Events.EFModel/efpt.config.json): EF Core Power Tools configuration used for model generation

## Purpose In The Teaching Materials

This project provides a shared schema foundation so the examples can focus on testing techniques, API integration, and infrastructure behavior instead of repeatedly redefining the domain model.

## Regenerating The Model

The presence of [efpt.config.json](Events.EFModel/efpt.config.json) indicates that the model is intended to be generated or refreshed with EF Core Power Tools from the PostgreSQL schema.

You can also regenerate the model from the command line with the `dotnet ef` scaffold command. For example, from the repository root:

```bash
dotnet ef dbcontext scaffold "Host=localhost;Port=5432;Database=eventsdb;Username=postgres;Password=postgres" \
  Npgsql.EntityFrameworkCore.PostgreSQL \
  --project Events.EFModel/Events.EFModel.csproj \
  --context-dir Models \
  --output-dir Models \
  --context EventsContext \
  --namespace Events.EFModel.Models \
  --force
```

Adjust the connection string to match your local PostgreSQL instance, and add or remove scaffold options depending on whether you want all tables, a subset of tables, or preserved naming.

If you regenerate it, make sure:

- the database schema matches the expected sample structure
- downstream projects still compile
- partial classes or custom mappings in dependent projects remain compatible
