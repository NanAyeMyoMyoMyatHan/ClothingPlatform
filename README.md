# 👗 Clothing Platform

A full-stack e-commerce web application for clothing retail, built with **ASP.NET Core 8**, **Blazor Server**, and **PostgreSQL**. The platform supports customer shopping, staff order management, and admin dashboards — all in a single deployable solution.

---

## 📋 Table of Contents

- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Project Structure](#-project-structure)
- [Prerequisites](#-prerequisites)
- [Getting Started](#-getting-started)
  - [1. Clone the Repository](#1-clone-the-repository)
  - [2. Set Up PostgreSQL Database](#2-set-up-postgresql-database)
  - [3. Configure Connection Strings](#3-configure-connection-strings)
  - [4. Restore Dependencies & Build](#4-restore-dependencies--build)
  - [5. Run the Application](#5-run-the-application)
- [Deployment with Docker](#-deployment-with-docker)
- [Default Ports](#-default-ports)
- [API Documentation](#-api-documentation)

---

## ✨ Features

### Customer Portal
- Browse products by category with search and filtering
- Product variants (size, color) with image galleries
- Shopping cart and wishlist
- Checkout with multiple payment methods (COD, KPay, Wave Money)
- Payment slip upload for mobile payments
- Order history and tracking
- Promo code support
- Real-time notifications

### Admin Dashboard
- Complete order management with status workflow
- Product & inventory management (CRUD, stock tracking)
- Category management
- Staff and role-based permission system
- Guest/phone order creation
- Sales reports and revenue analytics (CSV export)
- Promotion management
- Contact message viewer

### Staff Portal
- Order fulfillment and processing
- Guest order creation
- Sales logging and activity tracking

---

## 🛠 Tech Stack

| Layer        | Technology                                      |
| ------------ | ----------------------------------------------- |
| **Frontend** | Blazor Server (Interactive SSR)                 |
| **Backend**  | ASP.NET Core 8 Web API                          |
| **Database** | PostgreSQL with Entity Framework Core 8 (Npgsql) |
| **Auth**     | JWT Bearer Authentication + BCrypt password hashing |
| **Real-time**| ASP.NET Core SignalR                            |
| **API Docs** | Swagger / Swashbuckle                           |
| **Deploy**   | Docker + Nginx reverse proxy                    |

---

## 📁 Project Structure

```
ClothingPlatform/
├── ClothingPlatform.Web/        # Blazor Server frontend (SSR)
│   ├── Components/Pages/        # Razor pages (Admin, CustomerView, Login, etc.)
│   ├── Services/                # Session state, HTTP client services
│   └── wwwroot/                 # Static assets (CSS, images, payment slips)
│
├── ClothingPlatform.Api/        # ASP.NET Core Web API
│   ├── Features/                # Feature-based modules
│   │   ├── Auth/                # Login, registration, JWT
│   │   ├── Cart/                # Shopping cart
│   │   ├── Order/               # Order placement & history
│   │   ├── Product/             # Product CRUD
│   │   ├── Staff/               # Staff & guest order management
│   │   ├── Report/              # Sales reports & CSV export
│   │   ├── User/                # User management
│   │   ├── Permission/          # Role-based access control
│   │   ├── Notifications/       # Customer notifications
│   │   └── Category/            # Category management
│   └── Models/                  # DTOs and request/response models
│
├── ClothingPlatform.DB/         # Data access layer
│   ├── AppDbModels/             # EF Core entities & DbContext
│   └── Scripts/                 # Database migration scripts
│
├── ClothingPlatform.slnx        # Solution file
├── script_postgres.sql          # Full database schema + seed data
├── Dockerfile                   # Multi-stage Docker build
├── nginx.conf                   # Nginx reverse proxy config
├── entrypoint.sh                # Docker entrypoint script
└── NuGet.Config                 # NuGet package source config
```

---

## 📌 Prerequisites

Before you begin, make sure you have the following installed:

- [**.NET 8 SDK**](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (version 8.0 or later)
- [**PostgreSQL**](https://www.postgresql.org/download/) (version 14 or later recommended)
- [**Git**](https://git-scm.com/downloads)
- A code editor such as [**Visual Studio 2022**](https://visualstudio.microsoft.com/) or [**VS Code**](https://code.visualstudio.com/)

---

## 🚀 Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/NanAyeMyoMyoMyatHan/ClothingPlatform.git
cd ClothingPlatform
```

### 2. Set Up PostgreSQL Database

Open your PostgreSQL client (pgAdmin, psql, or any tool) and create a new database:

```sql
CREATE DATABASE ClothingPlatformDB;
```

Then run the schema and seed script to create all tables and insert initial data:

```bash
psql -U postgres -d ClothingPlatformDB -f script_postgres.sql
```

> **Note:** If using **pgAdmin**, open the Query Tool on `ClothingPlatformDB`, then open and execute the `script_postgres.sql` file.

### 3. Configure Connection Strings

Update the connection strings in both projects to match your local PostgreSQL setup:

**`ClothingPlatform.Api/appsettings.Development.json`**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ClothingPlatformDB;Username=postgres;Password=YOUR_PASSWORD;Command Timeout=60;"
  }
}
```

**`ClothingPlatform.Web/appsettings.json`**

```json
{
  "ApiUrl": "https://localhost:7065",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ClothingPlatformDB;Username=postgres;Password=YOUR_PASSWORD;SslMode=Prefer;Trust Server Certificate=true;"
  }
}
```

> ⚠️ Replace `YOUR_PASSWORD` with your actual PostgreSQL password.

### 4. Restore Dependencies & Build

From the project root directory, restore all NuGet packages and build the solution:

```bash
dotnet restore
dotnet build
```

### 5. Run the Application

You need to run **both** the API and the Web projects. Open two terminals:

**Terminal 1 — Start the API:**

```bash
cd ClothingPlatform.Api
dotnet run
```

The API will start at `https://localhost:7065` by default.

**Terminal 2 — Start the Web App:**

```bash
cd ClothingPlatform.Web
dotnet run
```

The Web app will start at `https://localhost:7211` (or the port shown in the terminal output).

> **Tip:** If using Visual Studio, you can set up **multiple startup projects** by right-clicking the solution → Properties → Startup Project → Multiple startup projects → set both `ClothingPlatform.Api` and `ClothingPlatform.Web` to "Start".

### 6. Open in Browser

Navigate to the Web app URL shown in the terminal (e.g., `https://localhost:7211`).

- **Customer Shop:** Main page after opening the URL
- **Admin Panel:** `/admin`
- **Staff Portal:** `/staff`

---

## 🐳 Deployment with Docker

The project includes a Docker setup with Nginx as a reverse proxy that serves both the API and Web app from a single container.

### Build and Run

```bash
# Build the Docker image
docker build -t clothing-platform .

# Run the container
docker run -d -p 8080:8080 \
  -e ConnectionStrings_DefaultConnection="Host=YOUR_DB_HOST;Port=5432;Database=ClothingPlatformDB;Username=postgres;Password=YOUR_PASSWORD;SslMode=Require;Trust Server Certificate=true" \
  clothing-platform
```

The app will be available at `http://localhost:8080`.

### Environment Variables

| Variable                            | Description                              |
| ----------------------------------- | ---------------------------------------- |
| `ConnectionStrings_DefaultConnection` | PostgreSQL connection string (required)  |
| `PORT`                               | Port to listen on (default: `8080`)      |
| `ApiUrl`                             | Internal API URL (default: `http://localhost:5000/`) |

---

## 🔌 Default Ports

| Service          | URL                        |
| ---------------- | -------------------------- |
| Web App (Dev)    | `https://localhost:7211`   |
| API (Dev)        | `https://localhost:7065`   |
| Swagger UI       | `https://localhost:7065/swagger` |
| Docker (Prod)    | `http://localhost:8080`    |

---

## 📖 API Documentation

When the API is running in development mode, Swagger UI is available at:

```
https://localhost:7065/swagger
```

Use the **Authorize** button in Swagger to add your JWT token (format: `Bearer <your_token>`) for authenticated endpoints.

---

## 📄 License

This project is developed for educational purposes.
