#!/bin/bash

# Railway provides PORT dynamically; fall back to 8080 for local/other platforms
PORT="${PORT:-8080}"

# ─── Parse DB URI into components ────────────────────────────────────────────
parse_uri() {
    local URI="$1"
    local TEMP="${URI#postgresql://}"
    TEMP="${TEMP#postgres://}"
    DB_USER="${TEMP%%:*}"
    TEMP="${TEMP#*:}"
    DB_PASS="${TEMP%%@*}"
    TEMP="${TEMP#*@}"
    DB_HOST="${TEMP%%:*}"
    TEMP="${TEMP#*:}"
    DB_PORT="${TEMP%%/*}"
    DB_NAME="${TEMP#*/}"
    DB_NAME="${DB_NAME%%\?*}"   # strip query params
}

# ─── Set connection string ────────────────────────────────────────────────────
# Priority 1: Explicit connection string (e.g. Supabase)
if [ -n "${ConnectionStrings_DefaultConnection}" ]; then
    CS="${ConnectionStrings_DefaultConnection}"
    if [[ "${CS}" == postgresql://* ]] || [[ "${CS}" == postgres://* ]]; then
        parse_uri "${CS}"
        CS="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS};SSL Mode=Require;Trust Server Certificate=true"
        echo "=== Converted URI to Npgsql format (from ConnectionStrings_DefaultConnection) ==="
    fi
    export ConnectionStrings__DefaultConnection="${CS}"
    echo "=== Database connection configured from ConnectionStrings_DefaultConnection ==="

# Priority 2: Railway's DATABASE_URL
elif [ -n "${DATABASE_URL}" ]; then
    parse_uri "${DATABASE_URL}"
    export ConnectionStrings__DefaultConnection="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS};Trust Server Certificate=true"
    echo "=== Database connection configured from DATABASE_URL ==="
fi

# ─── Run DB migration if needed ──────────────────────────────────────────────
if [ -n "${DB_HOST}" ] && [ -f /app/migration.sql ]; then
    echo "=== Waiting for database to be ready ==="
    export PGPASSWORD="${DB_PASS}"

    # Wait up to 30 seconds for the database to accept connections
    for i in $(seq 1 15); do
        if pg_isready -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" -q; then
            echo "=== Database is ready ==="
            break
        fi
        echo "    Waiting... attempt ${i}/15"
        sleep 2
    done

    # Check if schema already exists (look for 'cart_items' table)
    TABLE_EXISTS=$(psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" \
        -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='cart_items';" 2>/dev/null | tr -d ' \n')

    if [ "${TABLE_EXISTS}" = "0" ] || [ -z "${TABLE_EXISTS}" ]; then
        echo "=== Running database migration ==="
        psql -h "${DB_HOST}" -p "${DB_PORT}" -U "${DB_USER}" -d "${DB_NAME}" \
            --set ON_ERROR_STOP=off \
            -f /app/migration.sql 2>&1 | tail -5
        echo "=== Migration complete ==="
    else
        echo "=== Database already has schema, skipping migration ==="
    fi
fi

# ─── Nginx setup ─────────────────────────────────────────────────────────────
mkdir -p /tmp/client_body /tmp/fastcgi_temp /tmp/proxy_temp /tmp/scgi_temp /tmp/uwsgi_temp
sed "s/PORT_PLACEHOLDER/${PORT}/g" /app/nginx.conf > /tmp/nginx_runtime.conf

echo "=== Testing nginx config (port ${PORT}) ==="
nginx -t -c /tmp/nginx_runtime.conf
echo "=== nginx config OK ==="

# ─── Start services ──────────────────────────────────────────────────────────
echo "=== Starting API on port 5000 ==="
dotnet /app/api/ClothingPlatform.Api.dll --urls "http://127.0.0.1:5000" --contentRoot /app/api &

echo "=== Starting Web on port 5001 ==="
dotnet /app/web/ClothingPlatform.Web.dll --urls "http://127.0.0.1:5001" --contentRoot /app/web &

echo "=== Starting nginx on port ${PORT} ==="
exec nginx -g "daemon off;" -c /tmp/nginx_runtime.conf
