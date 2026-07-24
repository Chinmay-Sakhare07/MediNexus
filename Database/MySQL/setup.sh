#!/bin/bash
# =============================================================================
# MediNexus - one-shot MySQL/MariaDB setup for the Oracle Cloud VM.
# Installs MariaDB, loads the schema + seed + users + procedures, creates the
# app DB user, and prints the connection string. No SSH required: paste the
# one-liner below into OCI "Run command".
#
#   curl -fsSL https://raw.githubusercontent.com/Chinmay-Sakhare07/MediNexus/main/Database/MySQL/setup.sh | bash
#
# Optional: preset the app password by exporting MEDINEXUS_DB_PASS first.
# =============================================================================
# NOTE: deliberately NOT using `set -e`. As root, idioms like `[ test ] && cmd`
# return non-zero and would abort the whole script silently. Steps echo their
# progress and the final verify confirms success, so errors are visible anyway.

REPO_RAW="https://raw.githubusercontent.com/Chinmay-Sakhare07/MediNexus/main/Database/MySQL"
if [ "$(id -u)" -eq 0 ]; then SUDO=""; else SUDO="sudo"; fi
APPPASS="${MEDINEXUS_DB_PASS:-$(tr -dc 'A-Za-z0-9' </dev/urandom | head -c 20)}"
echo "== MediNexus DB setup starting (running as $(id -un)) =="

echo "== [1/6] Swap file (safety net on a 1 GB VM) =="
if ! $SUDO swapon --show 2>/dev/null | grep -q swapfile; then
  $SUDO fallocate -l 2G /swapfile && $SUDO chmod 600 /swapfile
  $SUDO mkswap /swapfile && $SUDO swapon /swapfile
  echo '/swapfile none swap sw 0 0' | $SUDO tee -a /etc/fstab >/dev/null
fi

echo "== [2/6] Install MariaDB =="
if command -v dnf >/dev/null 2>&1; then
  $SUDO dnf install -y mariadb-server mariadb
elif command -v apt-get >/dev/null 2>&1; then
  $SUDO apt-get update -y
  $SUDO DEBIAN_FRONTEND=noninteractive apt-get install -y mariadb-server mariadb-client
else
  echo "No supported package manager (dnf/apt) found"; exit 1
fi
$SUDO systemctl enable --now mariadb

MYSQL="$SUDO $(command -v mariadb || command -v mysql)"

echo "== [3/6] Download SQL scripts from GitHub =="
cd /tmp
for f in 01_schema.sql 02_seed_data.sql 03_seed_users.sql 04_procedures.sql; do
  curl -fsSL "$REPO_RAW/$f" -o "$f"
done

echo "== [4/6] Load database =="
for f in 01_schema.sql 02_seed_data.sql 03_seed_users.sql 04_procedures.sql; do
  echo "   loading $f"; $MYSQL < "$f"
done

echo "== [5/6] Create/refresh app DB user =="
$MYSQL <<SQL
CREATE USER IF NOT EXISTS 'medinexus_app'@'localhost' IDENTIFIED BY '${APPPASS}';
ALTER USER 'medinexus_app'@'localhost' IDENTIFIED BY '${APPPASS}';
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE, SHOW VIEW ON medinexus.* TO 'medinexus_app'@'localhost';
FLUSH PRIVILEGES;
SQL

echo "== [6/6] Verify =="
$MYSQL medinexus -e "SELECT (SELECT COUNT(*) FROM PATIENT) AS patients, (SELECT COUNT(*) FROM APPOINTMENT) AS appts, (SELECT COUNT(*) FROM USER_ACCOUNT) AS users;"

cat <<EOF

============================================================
 MediNexus DB is ready on this VM (MariaDB, bound to localhost:3306).
 App DB user : medinexus_app
 App DB pass : ${APPPASS}
 >>> SAVE THIS. Connection string for the API (next phase):
 Server=127.0.0.1;Port=3306;Database=medinexus;User ID=medinexus_app;Password=${APPPASS};
============================================================
EOF
