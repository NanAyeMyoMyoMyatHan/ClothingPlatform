using ClothingPlatform.DB;
using ClothingPlatform.DB.AppDbModels;
using ClothingPlatform.Web.Components;
using ClothingPlatform.Web.Components.Pages;
using ClothingPlatform.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<SessionState>();
builder.Services.AddScoped<CustomerSessionState>();
builder.Services.AddScoped<IPortalSessionBootstrapper, PortalSessionBootstrapper>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ServerCookieService>();

string connectionString = GetConnectionString(builder.Configuration);
connectionString = ParseConnectionString(connectionString);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseNpgsql(connectionString);
}, ServiceLifetime.Scoped);

string apiUrl = builder.Configuration["ApiUrl"] ?? "https://localhost:7065/";

builder.Services.AddHttpClient("admin", client =>
{
    client.BaseAddress = new Uri(apiUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddScoped<HttpClientServices>();

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiUrl)
});
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await SchemaCompatibility.EnsureCancelledOrderStatusSupportAsync(db);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization warning (Web): {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapPost("/api/cookies/set-auth", (SetAuthCookieRequest request, ServerCookieService cookieService) =>
{
    cookieService.SetAuthCookies(request.Token, request.UserId);
    return Results.Ok();
});

app.MapPost("/api/cookies/set-customer", (SetCustomerCookieRequest request, ServerCookieService cookieService) =>
{
    cookieService.SetCustomerIdCookie(request.UserId);
    return Results.Ok();
});

app.MapPost("/api/cookies/clear", (ServerCookieService cookieService) =>
{
    cookieService.ClearAuthCookies();
    return Results.Ok();
});

app.Run();

static string GetConnectionString(IConfiguration configuration)
{
    var candidates = new[]
    {
        Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
        Environment.GetEnvironmentVariable("ConnectionStrings_DefaultConnection"),
        Environment.GetEnvironmentVariable("DATABASE_URL"),
        Environment.GetEnvironmentVariable("POSTGRES_URL"),
        configuration.GetConnectionString("DefaultConnection")
    };
    foreach (var cs in candidates)
        if (!string.IsNullOrWhiteSpace(cs)) return cs;
    return string.Empty;
}

static string ParseConnectionString(string connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return connectionString;

    if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
    {
        try
        {
            var uri = new Uri(connectionString);
            var userInfo = uri.UserInfo.Split(':');
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode=Require;Trust Server Certificate=true;";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing database URI: {ex.Message}");
        }
    }
    return connectionString;
}

public record SetAuthCookieRequest(string Token, int UserId);
public record SetCustomerCookieRequest(int UserId);
