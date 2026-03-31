#!/usr/bin/env sh

cd "$(dirname "$0")/.."

dotnet run --project Topic2/Events.WebAPI/Events.WebAPI.csproj &
api_pid=$!

cleanup() {
  kill "$api_pid" 2>/dev/null || true
}

trap cleanup EXIT INT TERM

cd Topic2/Events.ClientApp
npm run dev
