# DataGenerator

`DataGenerator` is a small .NET console tool that generates SQL insert statements for the `person` table based on the countries already present in the PostgreSQL database (but limited to the countries currently supported by Bogus, and excluding Chinese, Korean, and Japanese, because transcription was not done correctly).

It is used as supporting material for the teaching examples in this repository and helps produce realistic multilingual seed data for the sports-events domain.

## What It Does

- connects to the shared `events` PostgreSQL database
- reads the available countries from the database
- generates fake participant data with locale-specific names and transliterations
- writes SQL insert statements to a file that can be reused in the Docker initialization scripts

By default, the output file is:

```text
docker-definitions\postgres-eventsdb\init\06-people.sql
```

## Prerequisites

- .NET SDK 8.0
- a running PostgreSQL instance populated with the base schema and countries (e.g `docker-compose up` in `docker-definitions\postgres-eventsdb` folder)

## Configuration

The project uses the shared .NET user secrets id:

```text
Erasmus-STA-2026
```

It expects the `ConnectionStrings:EventDB` value to be available through user secrets or another supported configuration source.

Example:

```powershell
dotnet user-secrets set "ConnectionStrings:EventDB" "Host=localhost;Port=5432;Database=events;Username=sport;Password=your-password;Persist Security Info=True;" --project DataGenerator\DataGenerator.csproj
```

The placeholder value in [appsettings.json](DataGenerator/appsettings.json) is not meant to be used as a real password.

## Running

```powershell
dotnet run --project DataGenerator\DataGenerator.csproj
```

The tool asks for an output path. Press `Enter` to accept the default output file.

## Files

- [Program.cs](DataGenerator/Program.cs): generation logic
- [appsettings.json](DataGenerator/appsettings.json): placeholder connection string configuration
- [create_countries_sql.py](DataGenerator/create_countries_sql.py): Additional python script to generate SQL script with INSERTS to insert countries with their English, Croatian, and Macedonian names
