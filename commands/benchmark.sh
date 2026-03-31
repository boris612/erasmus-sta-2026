#!/usr/bin/env sh

cd "$(dirname "$0")/.."
mkdir -p Topic2/Benchmarks/results
dotnet run --project Topic2/Benchmarks/Events.WebAPI.Benchmarks/Events.WebAPI.Benchmarks.csproj -c Release "$@"
