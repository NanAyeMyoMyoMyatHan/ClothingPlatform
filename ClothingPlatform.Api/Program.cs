using ClothingPlatform.Api.Features.Auth;
using ClothingPlatform.Api.Features.Cart;
using ClothingPlatform.Api.Features.Notifications;
using ClothingPlatform.Api.Features.Order;
//using ClothingPlatform.Api.Features.Permission;
using ClothingPlatform.Api.Features.Product;
using ClothingPlatform.Api.Features.Report;
using ClothingPlatform.Api.Features.User;
using ClothingPlatform.Api.Filters;
using ClothingPlatform.DB.AppDbModels;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "JwtWebApi", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement()
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });
});
builder.Services.AddScoped<ClothingPlatform.Api.Features.Staff.IStaffService, ClothingPlatform.Api.Features.Staff.StaffServices>();
builder.Services.AddScoped<IUserService, UserServices>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICustomerAuthService, CustomerAuthService>();
builder.Services.AddScoped<IProductService, ProductServices>();
builder.Services.AddScoped<IOrderService, OrderServices>();
builder.Services.AddScoped<ICartService, CartServices>();
builder.Services.AddScoped<IReportService, ReportServices>();
builder.Services.AddScoped<ICustomerNotificationService, CustomerNotificationService>();
//builder.Services.AddScoped<IPermissionService, PermissionService>();
builder.Services.AddSignalR();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("staff"));
    options.AddPolicy("AdminOrStaff", policy => policy.RequireRole("admin", "staff"));
    options.AddPolicy("Reports.Generate", policy =>
    {
        policy.RequireRole("admin");
        policy.RequireClaim("permission", "Reports.Generate");
    });
});


builder.Services.AddControllers().AddJsonOptions(options =>
{
    // 👇 ဒီကောင်က JSON ပတ်ချာလည် လည်ပြီး 500 Error တက်တာကို အပြီးတိုင် တားဆီးပေးပါတယ်
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

string connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CustomerNotificationHub>("/hubs/customer-notifications");

app.Run();
