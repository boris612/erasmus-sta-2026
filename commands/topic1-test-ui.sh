#!/usr/bin/env sh

cd "$(dirname "$0")/.."
dotnet test Topic1/Tests/Events.Tests.UITests/Events.Tests.UITests.csproj "$@"
