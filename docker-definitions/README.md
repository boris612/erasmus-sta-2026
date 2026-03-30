# Docker Definitions

This folder contains local Docker-based infrastructure definitions used by the teaching materials in this repository.

## Available Setups

- [postgres-eventsdb](docker-definitions/postgres-eventsdb): the main PostgreSQL database for application runs
- [postgres-eventsdb-test](docker-definitions/postgres-eventsdb-test): the additional PostgreSQL database used by Topic 1 PostgreSQL-backed tests

## What The Containers Provide

- initialized PostgreSQL instances
- schema creation through startup scripts in each `init` directory
- seed data used by the examples
- pgAdmin for the main database setup

## Important Files

For each PostgreSQL setup:

- `.env.example`: committed template for local container settings such as ports, usernames, and passwords
- `.env`: local copy created from `.env.example`
- `docker-compose.yml`: service definitions
- `init`: startup scripts copied into `/docker-entrypoint-initdb.d`
- `backup`: optional location for backup files

Before starting a setup for the first time, copy the example file in that folder:

```powershell
Copy-Item docker-definitions\postgres-eventsdb\.env.example docker-definitions\postgres-eventsdb\.env
Copy-Item docker-definitions\postgres-eventsdb-test\.env.example docker-definitions\postgres-eventsdb-test\.env
```

On Linux and macOS, make sure the shell scripts in each `init` folder are executable before starting the containers:

```bash
chmod +x docker-definitions/postgres-eventsdb/init/*.sh
chmod +x docker-definitions/postgres-eventsdb-test/init/*.sh
```

## Running The Main Database

```powershell
docker compose -f docker-definitions\postgres-eventsdb\docker-compose.yml up -d
```

Default services:

- PostgreSQL on port `5432`
- pgAdmin on port `5050`

## Running The Test Database

```powershell
docker compose -f docker-definitions\postgres-eventsdb-test\docker-compose.yml up -d
```

Default service:

- PostgreSQL on port `5433`

## Configuration Notes

- review the `.env` files before sharing or reusing the setup outside local teaching and demo environments
- application projects connect with the `sport` user created by the initialization scripts
- if you change ports or passwords in `.env`, update the matching `dotnet user-secrets` connection strings as well

## Troubleshooting

- If the database is not initialized as expected, remove the Docker volume and recreate the container so the init scripts run again
- If pgAdmin cannot connect, confirm the main PostgreSQL container is healthy before opening the UI
