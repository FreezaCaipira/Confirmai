#!/usr/bin/env bash
# backup.sh — PostgreSQL backup script for OtServMarket
#
# Usage:
#   ./backup.sh
#
# Environment variables (all optional — fall back to defaults):
#   PGHOST        PostgreSQL host          (default: localhost)
#   PGPORT        PostgreSQL port          (default: 5432)
#   PGDATABASE    Database name            (default: otservmarket)
#   PGUSER        PostgreSQL user          (default: postgres)
#   PGPASSWORD    PostgreSQL password      (set via env or .pgpass — never hardcode)
#   BACKUP_DIR    Destination directory    (default: /var/backups/otservmarket)
#   RETAIN_DAYS   Days to keep backups     (default: 7)
#
# Recommended cron (daily at 02:00):
#   0 2 * * * /opt/otservmarket/scripts/backup.sh >> /var/log/otservmarket-backup.log 2>&1

set -euo pipefail

# ── Configuration ─────────────────────────────────────────────────────────────
PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGDATABASE="${PGDATABASE:-otservmarket}"
PGUSER="${PGUSER:-postgres}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/otservmarket}"
RETAIN_DAYS="${RETAIN_DAYS:-7}"

TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
FILENAME="${PGDATABASE}_${TIMESTAMP}.dump"
FILEPATH="${BACKUP_DIR}/${FILENAME}"

# ── Helpers ───────────────────────────────────────────────────────────────────
log() { echo "[$(date -u +%Y-%m-%dT%H:%M:%SZ)] $*"; }

# ── Pre-flight ────────────────────────────────────────────────────────────────
if ! command -v pg_dump &>/dev/null; then
    log "ERROR: pg_dump not found. Install postgresql-client and retry."
    exit 1
fi

mkdir -p "${BACKUP_DIR}"
chmod 700 "${BACKUP_DIR}"

# ── Dump ──────────────────────────────────────────────────────────────────────
log "Starting backup of '${PGDATABASE}' on ${PGHOST}:${PGPORT} → ${FILEPATH}"

PGPASSWORD="${PGPASSWORD:-}" \
pg_dump \
    --host="${PGHOST}" \
    --port="${PGPORT}" \
    --username="${PGUSER}" \
    --dbname="${PGDATABASE}" \
    --format=custom \
    --compress=9 \
    --no-password \
    --file="${FILEPATH}"

BACKUP_SIZE="$(du -sh "${FILEPATH}" | cut -f1)"
log "Backup complete: ${FILENAME} (${BACKUP_SIZE})"

# ── Retention ─────────────────────────────────────────────────────────────────
log "Removing backups older than ${RETAIN_DAYS} days from ${BACKUP_DIR}"
REMOVED=0
while IFS= read -r -d '' old_file; do
    rm -f "${old_file}"
    log "Removed: $(basename "${old_file}")"
    REMOVED=$((REMOVED + 1))
done < <(find "${BACKUP_DIR}" -maxdepth 1 -name "${PGDATABASE}_*.dump" \
         -mtime "+${RETAIN_DAYS}" -print0)

if [ "${REMOVED}" -eq 0 ]; then
    log "No old backups to remove."
else
    log "Removed ${REMOVED} old backup(s)."
fi

# ── Summary ───────────────────────────────────────────────────────────────────
TOTAL="$(find "${BACKUP_DIR}" -maxdepth 1 -name "${PGDATABASE}_*.dump" | wc -l | tr -d ' ')"
log "Done. ${TOTAL} backup(s) retained in ${BACKUP_DIR}."
