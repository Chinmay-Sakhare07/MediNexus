# MediNexus — MySQL Database

MySQL/MariaDB port of the MediNexus schema, with multi-user role-based login
and a pharmacy dispensing workflow. Ported from the original SQL Server scripts.

Validated on **MySQL 8.0** and **MariaDB 10.11** (loads clean, dispensing tested).

## Scripts (run in this order)

| # | File | Creates |
|---|------|---------|
| 1 | `01_schema.sql` | 26 core tables + `USER_ACCOUNT` (auth) + `MEDICINE_DISPENSE` (dispensing log) |
| 2 | `02_seed_data.sql` | Sample data (incl. a Pharmacy dept + pharmacist) |
| 3 | `03_seed_users.sql` | 7 login accounts, one per role (BCrypt-hashed passwords) |
| 4 | `04_procedures.sql` | `usp_DispenseMedicine` + `vw_InventoryStatus` + `vw_PharmacyPrescriptionQueue` |

Database name: **`medinexus`**.

## Roles & default logins

All demo accounts share the password **`MediNexus@2026`** (BCrypt, cost 11).
**Change these before any public deployment.**

| Username | Role | Linked to |
|----------|------|-----------|
| `admin` | Admin | Jennifer O'Brien (staff) |
| `dr.sharma` | Doctor | Dr. Rajesh Sharma |
| `nurse.anderson` | Nurse | James Anderson |
| `lab.kumar` | LabTech | Anil Kumar |
| `pharmacist` | Pharmacist | Olivia Bennett |
| `reception` | Receptionist | Christopher Garcia |
| `patient.shah` | Patient | Amit Shah (patient portal) |

## Pharmacy dispensing

`CALL usp_DispenseMedicine(prescriptionId, medicineId, qty, userId, notes, @dispenseId, @msg);`

Inside one transaction it: verifies the medicine is on that prescription, checks
total stock, FIFO-decrements `INVENTORY` (soonest expiry first) and
`MEDICINE.StockQuantity`, and writes a `MEDICINE_DISPENSE` row. Insufficient stock
rolls back with an error message in `@msg`.

---

## Local quickstart (Docker)

```bash
docker run -d --name medinexus-mysql -e MYSQL_ROOT_PASSWORD=rootpw mysql:8.0
# wait ~15s for startup, then from this folder:
for f in 01_schema.sql 02_seed_data.sql 03_seed_users.sql 04_procedures.sql; do
  docker exec -i medinexus-mysql mysql -uroot -prootpw < "$f"
done
```

---

## Deploy to the Oracle Cloud VM

**Recommended layout:** run the database **on the same VM as the .NET API**
(`129.153.7.145`). The API then connects over `localhost`, so MySQL is never
exposed to the internet and **no Oracle firewall / security-list change is needed**.
On the 1 GB E2.1.Micro, use **MariaDB** (lighter than MySQL 8) + a swap file.
(If you use the larger ARM free VM instead, MySQL 8.0 is fine too — same scripts.)

### 1. Copy the scripts up

From your machine (repo root):

```bash
scp -i ~/ssh-key.key -r Database/MySQL opc@129.153.7.145:~/medinexus-db
```

### 2. SSH in and add swap (1 GB VM safety net)

```bash
ssh -i ~/ssh-key.key opc@129.153.7.145

sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile
sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

### 3. Install MariaDB and lock it down

```bash
sudo apt update && sudo apt install -y mariadb-server
sudo systemctl enable --now mariadb
sudo mysql_secure_installation      # set root pw, remove anon users/test db, disallow remote root
```

MariaDB on Ubuntu binds to `127.0.0.1` by default — keep it that way (local-only).
Add a small low-memory tuning drop-in:

```bash
sudo tee /etc/mysql/mariadb.conf.d/99-medinexus.cnf >/dev/null <<'EOF'
[mysqld]
bind-address = 127.0.0.1
innodb_buffer_pool_size = 128M
performance_schema = OFF
max_connections = 50
EOF
sudo systemctl restart mariadb
```

### 4. Create the app database user

```bash
sudo mariadb <<'SQL'
CREATE DATABASE IF NOT EXISTS medinexus CHARACTER SET utf8mb4;
CREATE USER IF NOT EXISTS 'medinexus_app'@'localhost' IDENTIFIED BY 'REPLACE_WITH_STRONG_PASSWORD';
GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE, SHOW VIEW ON medinexus.* TO 'medinexus_app'@'localhost';
FLUSH PRIVILEGES;
SQL
```

### 5. Load the scripts (as root — needs CREATE privileges)

```bash
cd ~/medinexus-db
for f in 01_schema.sql 02_seed_data.sql 03_seed_users.sql 04_procedures.sql; do
  echo "Loading $f"; sudo mariadb < "$f"; done

# sanity check
sudo mariadb medinexus -e "SELECT COUNT(*) AS patients FROM PATIENT; SELECT COUNT(*) AS users FROM USER_ACCOUNT;"
```

### 6. Point the API at MySQL

The repositories read `ConnectionStrings__HospitalDb`. Update the systemd unit's
environment (same variable name as before) with a **MySqlConnector** connection string:

```
Server=127.0.0.1;Port=3306;Database=medinexus;User ID=medinexus_app;Password=REPLACE_WITH_STRONG_PASSWORD;
```

```bash
sudo systemctl edit medinexus     # add under [Service]:
# Environment=ConnectionStrings__HospitalDb=Server=127.0.0.1;Port=3306;Database=medinexus;User ID=medinexus_app;Password=REPLACE_WITH_STRONG_PASSWORD;
sudo systemctl daemon-reload
sudo systemctl restart medinexus
```

> **Note:** the API won't actually talk to MySQL until the code is switched from
> `Microsoft.Data.SqlClient` to `MySqlConnector` and the ~24 T-SQL tokens are
> adjusted (the next phase of this migration). Until then this DB stands ready
> and the connection string is in place.

## Security checklist

- [ ] Change all 7 demo passwords (or delete accounts you don't need)
- [ ] Strong, unique password for `medinexus_app`; never commit it
- [ ] Keep MariaDB bound to `127.0.0.1` (no public 3306)
- [ ] `mysql_secure_installation` completed
- [ ] Nightly backup: `mysqldump medinexus | gzip > ~/backups/medinexus-$(date +%F).sql.gz` (cron)
