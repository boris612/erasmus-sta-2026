# Benchmarks

This folder contains the benchmark project for Topic 2:

- `Events.WebAPI.Benchmarks`

## Purpose

The benchmark project measures the cost of common implementation choices used in the Web API solution:

- projection approaches
- insert approaches
- MediatR vs direct handler dispatch
- processing flow with and without a message bus
- RabbitMQ transport overhead compared to in-process alternatives

These benchmarks support the teaching material for `Web API Integration Testing in Complex Scenarios` by making architecture trade-offs measurable instead of purely theoretical.

## Project

`Events.WebAPI.Benchmarks` is a BenchmarkDotNet-based console project. It uses Testcontainers for .NET to:

- start PostgreSQL for database-backed benchmarks
- start RabbitMQ for `Question4A`

It reuses seed data from:

- `docker-definitions\postgres-eventsdb\init`

Configuration is read from [Events.WebAPI.Benchmarks/appsettings.json](Topic2/Benchmarks/Events.WebAPI.Benchmarks/appsettings.json), especially:

- `RepositoryRoot`
- `Database:PostgreSqlImage`
- `Database:InitScriptsPath`
- `Database:EnvFilePath`
- `Question4:OutputRoot`

`RepositoryRoot` is resolved first, and the other benchmark paths can be configured relative to it. This avoids coupling the benchmark setup to the `bin/...` output directory.

## Benchmark Groups

- `Question1`: read/projection benchmarks
- `Question2`: insert benchmarks
- `Question3`: MediatR vs direct dispatch on PostgreSQL
- `Question3A`: MediatR vs direct dispatch on EF Core InMemory
- `Question4`: processing benchmarks with MassTransit InMemory transport
- `Question4A`: processing benchmarks with RabbitMQ via Testcontainers

## Running

Run all benchmarks:

```powershell
./commands/benchmark.sh
```

Run a specific benchmark group:

- `-- 1`
- `-- 2`
- `-- 3`
- `-- 3a`
- `-- 4`
- `-- 4a`

Example:

```powershell
./commands/benchmark.sh 4a
```

## Outputs

BenchmarkDotNet writes reports to:

- [Topic2/Benchmarks/results](Topic2/Benchmarks/results)

Some processing benchmarks also write generated files under:

- [Topic2/.artifacts/perf](Topic2/.artifacts/perf)

## Prerequisites

- .NET SDK 8.0
- Docker Desktop for PostgreSQL and RabbitMQ Testcontainers scenarios

## Notes

- `Question4A` requires RabbitMQ containers managed by the benchmark code itself through Testcontainers
- benchmark numbers are most useful when run on a quiet machine and repeated several times
