#!/usr/bin/env sh

cd "$(dirname "$0")/.."
dotnet test Topic1/Tests/Events.Tests.UnitTests/Events.Tests.UnitTests.csproj "$@"
