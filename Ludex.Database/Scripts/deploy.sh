#!/bin/bash
# Publishes Ludex.Database.dacpac to SQL Server (shared-sql on the NAS) as sa, then exits. The post-deployment script
# creates the app's own login (SQL_APP_USER / SQL_APP_PASSWORD), which is what the WebApi connects with.
# SQL Server takes a few seconds to accept connections after it starts, so the publish is retried for up to two minutes.
set -u
: "${SQL_SERVER:=shared-sql}"
: "${SQL_DATABASE:=Ludex}"
: "${SQL_USER:=sa}"
: "${SQL_PASSWORD:?SQL_PASSWORD (the sa password) is required}"
: "${SQL_APP_USER:=ludex}"
: "${SQL_APP_PASSWORD:?SQL_APP_PASSWORD (the password of the ludex login) is required}"

for attempt in $(seq 1 24); do
  if sqlpackage /Action:Publish \
      /SourceFile:/deploy/Ludex.Database.dacpac \
      /TargetServerName:"$SQL_SERVER" /TargetDatabaseName:"$SQL_DATABASE" \
      /TargetUser:"$SQL_USER" /TargetPassword:"$SQL_PASSWORD" /TargetTrustServerCertificate:True \
      /p:BlockOnPossibleDataLoss=True \
      /v:AppUser="$SQL_APP_USER" /v:AppPassword="$SQL_APP_PASSWORD"; then
    echo "Ludex database ($SQL_DATABASE on $SQL_SERVER) is up to date; the WebApi signs in as $SQL_APP_USER."
    exit 0
  fi
  echo "SQL Server not ready yet (attempt $attempt), retrying in 5 s…"
  sleep 5
done

echo "Could not publish the database." >&2
exit 1
