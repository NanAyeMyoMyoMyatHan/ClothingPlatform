#!/bin/bash

# Start the API service
dotnet /app/api/ClothingPlatform.Api.dll --urls "http://localhost:5000" &

# Start the Web service
dotnet /app/web/ClothingPlatform.Web.dll --urls "http://localhost:5001" &

# Start Nginx in the foreground
nginx -g "daemon off;" -c /app/nginx.conf
