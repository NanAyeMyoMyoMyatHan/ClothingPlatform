# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY NuGet.Config ./
COPY ClothingPlatform.DB/ClothingPlatform.DB.csproj ClothingPlatform.DB/
COPY ClothingPlatform.Api/ClothingPlatform.Api.csproj ClothingPlatform.Api/
COPY ClothingPlatform.Web/ClothingPlatform.Web.csproj ClothingPlatform.Web/

# Restore dependencies
RUN dotnet restore ClothingPlatform.Api/ClothingPlatform.Api.csproj --no-cache
RUN dotnet restore ClothingPlatform.Web/ClothingPlatform.Web.csproj --no-cache

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

# Ensure scripts are executable and configure permissions for non-root users (Hugging Face port 7860/user 1000)
RUN chmod +x /app/entrypoint.sh && \
    chmod -R 777 /var/log/nginx /var/lib/nginx /run

# Environment variables for deployment
ENV ConnectionStrings_DefaultConnection=""
ENV ApiUrl="http://localhost:5000/"
ENV ASPNETCORE_FORWARDEDHEADERS_ENABLED=true

# Back4App runs on port 8080
EXPOSE 8080

ENTRYPOINT ["/app/entrypoint.sh"]
