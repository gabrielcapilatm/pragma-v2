#!/bin/bash
set -e

STARTUP_PROJECT="../src/LatamPlatform.Api"
INFRA_PROJECT="."

cd "$(dirname "$0")/../src/LatamPlatform.Infrastructure"

echo "Applying migrations to latam_br..."
dotnet ef database update \
  --startup-project $STARTUP_PROJECT \
  --connection "Host=localhost;Port=5432;Database=latam_br;Username=postgres;Password=dev123"

echo "Applying migrations to latam_ar..."
dotnet ef database update \
  --startup-project $STARTUP_PROJECT \
  --connection "Host=localhost;Port=5432;Database=latam_ar;Username=postgres;Password=dev123"

echo "Applying migrations to latam_cl..."
dotnet ef database update \
  --startup-project $STARTUP_PROJECT \
  --connection "Host=localhost;Port=5432;Database=latam_cl;Username=postgres;Password=dev123"

echo "Done! Migrations applied to all tenants."
