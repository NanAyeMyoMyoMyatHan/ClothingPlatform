#!/bin/bash

# Map Back4App env var (single underscore) to ASP.NET Core format (double underscore)
# Back4App does not allow __ in env var names, so we map it here
if [ -n "${ConnectionStrings_DefaultConnection}" ]; then
    export ConnectionStrings__DefaultConnection="${ConnectionStrings_DefaultConnection}"
fi

# Start the API service
dotnet /app/api/ClothingPlatform.Api.dll --urls "http://localhost:5000" &

# Start the Web service
dotnet /app/web/ClothingPlatform.Web.dll --urls "http://localhost:5001" &

# Start Nginx in the foreground
exec nginx -g "daemon off;" -c /app/nginx.conf
