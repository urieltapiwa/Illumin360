#!/bin/bash
# Creates a database per microservice + Keycloak (charter Part 13: DB-per-service).
set -e
create_db() {
  echo "  creating database: $1"
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-SQL
    SELECT 'CREATE DATABASE $1'
    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$1')\gexec
SQL
}
for svc in keycloak identity candidates employers recruitment students professionals admin billing payments notifications support engagement aiassistant; do
  create_db "illumin360_${svc}"
done
# Keycloak expects its own db name from KC_DB_NAME (default illumin360_keycloak)
