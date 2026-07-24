# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ClothingPlatform.slnx ./
COPY ClothingPlatform.DB/ClothingPlatform.DB.csproj ClothingPlatform.DB/
COPY ClothingPlatform.Api/ClothingPlatform.Api.csproj ClothingPlatform.Api/
COPY ClothingPlatform.Web/ClothingPlatform.Web.csproj ClothingPlatform.Web/

# Restore dependencies
RUN dotnet restore ClothingPlatform.slnx

# Copy everything else and build
COPY . .
WORKDIR /src/ClothingPlatform.Api
RUN dotnet publish ClothingPlatform.Api.csproj -c Release -o /app/publish/api --no-restore

WORKDIR /src/ClothingPlatform.Web
RUN dotnet publish ClothingPlatform.Web.csproj -c Release -o /app/publish/web --no-restore

# Final Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install Nginx
RUN apt-get update && apt-get install -y nginx && rm -rf /var/lib/apt/lists/*

# Copy configurations and published apps
COPY nginx.conf /app/nginx.conf
COPY entrypoint.sh /app/entrypoint.sh
COPY --from=build /app/publish/api /app/api
COPY --from=build /app/publish/web /app/web

# Strip Windows CRLF line endings (entrypoint.sh was edited on Windows) so bash can run it,
# then make it executable and configure permissions for non-root users
RUN sed -i 's/\r$//' /app/entrypoint.sh /app/nginx.conf && \
    chmod +x /app/entrypoint.sh && \
    chmod -R 777 /var/log/nginx /var/lib/nginx /run

# Environment variables for deployment
ENV ConnectionStrings__DefaultConnection=""
ENV ApiUrl="http://localhost:5000/"
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Back4App: exposed port must match the "Port" field set in the deployment form
EXPOSE 8000

ENTRYPOINT ["/app/entrypoint.sh"]
