#!/bin/bash

# Railway provides PORT dynamically; fall back to 8080 for local/other platforms
PORT="${PORT:-8080}"

# Priority 1: Explicit connection string (e.g. Supabase) — single underscore to double underscore
if [ -n "${ConnectionStrings_DefaultConnection}" ]; then
    export ConnectionStrings__DefaultConnection="${ConnectionStrings_DefaultConnection}"
    echo "=== Database connection configured from ConnectionStrings_DefaultConnection ==="
# Priority 2: Convert Railway's DATABASE_URL (postgresql://user:pass@host:port/db)
# into Npgsql key-value format
elif [ -n "${DATABASE_URL}" ]; then
    DB_TEMP="${DATABASE_URL#postgresql://}"
    DB_USER="${DB_TEMP%%:*}"
    DB_TEMP="${DB_TEMP#*:}"
    DB_PASS="${DB_TEMP%%@*}"
    DB_TEMP="${DB_TEMP#*@}"
    DB_HOST="${DB_TEMP%%:*}"
    DB_TEMP="${DB_TEMP#*:}"
    DB_PORT="${DB_TEMP%%/*}"
    DB_NAME="${DB_TEMP#*/}"
    export ConnectionStrings__DefaultConnection="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASS}"
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
