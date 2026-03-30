#!/bin/bash
# Docker init env varovi
USER=${POSTGRES_USER:-postgres}
DB=${POSTGRES_DB:-postgres}

pg_isready -U "$USER" -d "$DB"