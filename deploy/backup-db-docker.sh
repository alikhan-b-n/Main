#!/usr/bin/env bash
# Nightly PostgreSQL dump from the compose stack.
#
#   sudo install -m 755 backup-db-docker.sh /usr/local/bin/iconicu-backup
#   crontab -e   →   15 3 * * * COMPOSE_DIR=/opt/iconicu/Main/deploy /usr/local/bin/iconicu-backup
#
# A docker volume is not a backup: it dies with the server. Copy these dumps
# elsewhere (another host, object storage) as well.
set -euo pipefail

COMPOSE_DIR="${COMPOSE_DIR:-/opt/iconicu/Main/deploy}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/iconicu}"
KEEP_DAYS="${KEEP_DAYS:-14}"

cd "$COMPOSE_DIR"
# Database name and user come from the same .env the stack uses
set -a && . ./.env && set +a
DB_NAME="${POSTGRES_DB:-LamaCRM}"
DB_USER="${POSTGRES_USER:-iconicu}"

mkdir -p "$BACKUP_DIR"
target="$BACKUP_DIR/${DB_NAME}_$(date +%Y-%m-%d_%H%M).sql.gz"

# Write to a temporary file first: a crash must not leave a half-written backup
tmp="$target.part"
docker compose exec -T db pg_dump --username="$DB_USER" --dbname="$DB_NAME" --format=plain --no-owner \
  | gzip -9 > "$tmp"
mv "$tmp" "$target"
chmod 600 "$target"

find "$BACKUP_DIR" -name "${DB_NAME}_*.sql.gz" -mtime "+$KEEP_DAYS" -delete

echo "$(date -Is) backup ok: $target ($(du -h "$target" | cut -f1))"
