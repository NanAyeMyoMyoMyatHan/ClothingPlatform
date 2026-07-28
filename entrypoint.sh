#!/bin/bash

# Railway provides PORT dynamically; fall back to 8080 for local/other platforms
PORT="${PORT:-8080}"

# Helper: converts a postgresql:// URI to Npgsql key-value format
uri_to_npgsql() {
    local URI="$1"
    local TEMP="${URI#postgresql://}"
    TEMP="${TEMP#postgres://}"
    local USER="${TEMP%%:*}"
    TEMP="${TEMP#*:}"
    local PASS="${TEMP%%@*}"
    TEMP="${TEMP#*@}"
    local HOST="${TEMP%%:*}"
    TEMP="${TEMP#*:}"
    local PORT="${TEMP%%/*}"
    local DB="${TEMP#*/}"
    # Remove any query params (e.g. ?pgbouncer=true)
    DB="${DB%%\?*}"
    echo "Host=${HOST};Port=${PORT};Database=${DB};Username=${USER};Password=${PASS};SSL Mode=Require;Trust Server Certificate=true"
}

# Priority 1: Explicit connection string (e.g. Supabase)
if [ -n "${ConnectionStrings_DefaultConnection}" ]; then
    CS="${ConnectionStrings_DefaultConnection}"
    # If it's a URI, convert it to Npgsql key-value format
    if [[ "${CS}" == postgresql://* ]] || [[ "${CS}" == postgres://* ]]; then
        CS=$(uri_to_npgsql "${CS}")
        echo "=== Converted Supabase URI to Npgsql format ==="
    fi
    export ConnectionStrings__DefaultConnection="${CS}"
    echo "=== Database connection configured from ConnectionStrings_DefaultConnection ==="
# Priority 2: Convert Railway's DATABASE_URL (postgresql://user:pass@host:port/db)
elif [ -n "${DATABASE_URL}" ]; then
    export ConnectionStrings__DefaultConnection=$(uri_to_npgsql "${DATABASE_URL}")
    echo "=== Database connection configured from DATABASE_URL ==="
fi

# Create nginx required temp directories
mkdir -p /tmp/client_body /tmp/fastcgi_temp /tmp/proxy_temp /tmp/scgi_temp /tmp/uwsgi_temp

# Inject the actual PORT into the nginx config at runtime
sed "s/PORT_PLACEHOLDER/${PORT}/g" /app/nginx.conf > /tmp/nginx_runtime.conf

# Test nginx configuration before starting
echo "=== Testing nginx config (port ${PORT}) ==="
nginx -t -c /tmp/nginx_runtime.conf
echo "=== nginx config OK ==="

# Start the API service
echo "=== Starting API on port 5000 ==="
dotnet /app/api/ClothingPlatform.Api.dll --urls "http://127.0.0.1:5000" --contentRoot /app/api &

# Start the Web service
echo "=== Starting Web on port 5001 ==="
dotnet /app/web/ClothingPlatform.Web.dll --urls "http://127.0.0.1:5001" --contentRoot /app/web &

# Start Nginx in the foreground using the runtime config
echo "=== Starting nginx on port ${PORT} ==="
exec nginx -g "daemon off;" -c /tmp/nginx_runtime.conf
