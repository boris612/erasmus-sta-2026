#!/usr/bin/env sh

cd "$(dirname "$0")/.."
dotnet test Topic2/Tests/Events.WebAPI.IntegrationTests/Events.WebAPI.IntegrationTests.csproj "$@"
