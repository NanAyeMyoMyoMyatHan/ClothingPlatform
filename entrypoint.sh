#!/bin/bash

# Map Back4App env var (single underscore) to ASP.NET Core format (double underscore)
if [ -n "${ConnectionStrings_DefaultConnection}" ]; then
    export ConnectionStrings__DefaultConnection="${ConnectionStrings_DefaultConnection}"
fi

# Create nginx required temp directories
mkdir -p /tmp/client_body /tmp/fastcgi_temp /tmp/proxy_temp /tmp/scgi_temp /tmp/uwsgi_temp

# Test nginx configuration before starting
echo "=== Testing nginx config ==="
nginx -t -c /app/nginx.conf
echo "=== nginx config OK ==="

# Start the API service
echo "=== Starting API on port 5000 ==="
dotnet /app/api/ClothingPlatform.Api.dll --urls "http://127.0.0.1:5000" --contentRoot /app/api &

# Start the Web service
echo "=== Starting Web on port 5001 ==="
dotnet /app/web/ClothingPlatform.Web.dll --urls "http://127.0.0.1:5001" --contentRoot /app/web &

# Start Nginx in the foreground
echo "=== Starting nginx on port 8080 ==="
exec nginx -g "daemon off;" -c /app/nginx.conf
