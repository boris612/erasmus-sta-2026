#!/usr/bin/env sh

cd "$(dirname "$0")/.."
docker compose -f docker-definitions/postgres-eventsdb/docker-compose.yml "$@"
