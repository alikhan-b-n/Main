#!/usr/bin/env bash
# Nightly PostgreSQL dump for the IconicU CRM.
#
# Install:
#   sudo install -m 755 backup-db.sh /usr/local/bin/iconicu-backup
#   sudo install -d -o iconicu -g iconicu /var/backups/iconicu
#   sudo crontab -u iconicu -e   →   15 3 * * * /usr/local/bin/iconicu-backup
#
# Credentials come from ~/.pgpass (chmod 600), not from this file:
#   127.0.0.1:5432:LamaCRM:iconicu:<password>
set -euo pipefail

DB_NAME="${DB_NAME:-LamaCRM}"
DB_USER="${DB_USER:-iconicu}"
DB_HOST="${DB_HOST:-127.0.0.1}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/iconicu}"
KEEP_DAYS="${KEEP_DAYS:-14}"

mkdir -p "$BACKUP_DIR"
stamp=$(date +%Y-%m-%d_%H%M)
target="$BACKUP_DIR/${DB_NAME}_${stamp}.sql.gz"

# Dump to a temporary file first: a crash must not leave a half-written backup
tmp="$target.part"
pg_dump --host="$DB_HOST" --username="$DB_USER" --dbname="$DB_NAME" --format=plain --no-owner \
  | gzip -9 > "$tmp"
mv "$tmp" "$target"
chmod 600 "$target"

# Keep the last KEEP_DAYS days
find "$BACKUP_DIR" -name "${DB_NAME}_*.sql.gz" -mtime "+$KEEP_DAYS" -delete

echo "$(date -Is) backup ok: $target ($(du -h "$target" | cut -f1))"
