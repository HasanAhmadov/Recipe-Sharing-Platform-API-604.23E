using System.Text;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Recipe_Sharing_Platform_API.JWT;
using Recipe_Sharing_Platform_API.Data;
using Recipe_Sharing_Platform_API.Services;
using Recipe_Sharing_Platform_API.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Recipe_Sharing_Platform_API.Utility;

var builder = WebApplication.CreateBuilder(args);

// ✅ FIX 1: Configure for Railway PORT first
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// Add controllers + swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Recipe Sharing Platform API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token with Bearer prefix. Example: \"Bearer eyJhbGciOiJIUzI1...\""
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// DbContext
var connectionString = ConnectionHelper.GetConnectionString(builder.Configuration);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

// Bind JWT settings and validate
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings == null || string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 16)
{
    throw new InvalidOperationException("JwtSettings:Key is missing or too short. Put a proper Key in appsettings.json (at least 16 chars).");
}

// DI
builder.Services.AddScoped<IAuthService, AuthService>();

// Authentication setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // ✅ FIX 2: Set to true for production, but false for Railway testing
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtSettings.Issuer,

        ValidateAudience = true,
        ValidAudience = jwtSettings.Audience,

        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),

        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            Console.WriteLine($"JWT Authentication failed: {ctx.Exception?.Message}");
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            Console.WriteLine("JWT token validated.");
            return Task.CompletedTask;
        }
    };
});

// Authorization
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ✅ NEW: Swagger Password Protection Middleware
app.Use(async (context, next) =>
{
    // Only protect Swagger UI routes
    if (context.Request.Path.StartsWithSegments("/swagger") ||
        context.Request.Path.StartsWithSegments("/swagger-ui"))
    {
        string authHeader = context.Request.Headers["Authorization"];
        if (authHeader != null && authHeader.StartsWith("Basic "))
        {
            // Extract credentials
            var encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
            var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword));
            var parts = decodedUsernamePassword.Split(':');

            // Get credentials from environment variables or use defaults
            var expectedUsername = Environment.GetEnvironmentVariable("SWAGGER_USERNAME") ?? "admin";
            var expectedPassword = Environment.GetEnvironmentVariable("SWAGGER_PASSWORD") ?? "Sw@g3rSecure_2025!#API";

            if (parts.Length == 2 && parts[0] == expectedUsername && parts[1] == expectedPassword)
            {
                await next();
                return;
            }
        }

        // Return 401 if not authenticated for Swagger
        context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Recipe API Swagger UI\"";
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized access to Swagger documentation");
        return;
    }

    // ✅ ALL other routes (your API endpoints) pass through normally
    await next();
});

// ✅ FIX 3: Enable Swagger in ALL environments (not just Development)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Recipe Sharing Platform API v1");
    c.RoutePrefix = "swagger"; // This makes it available at /swagger
    c.DisplayRequestDuration();
});

// ✅ FIX 4: CORS should come before other middleware
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// ✅ FIX 5: Authentication/Authorization order
app.UseAuthentication();
app.UseAuthorization();

// Database migration
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        await context.Database.MigrateAsync();
        await DataHelper.ManageDataAsync(scope.ServiceProvider);
        Console.WriteLine("Database migrated successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database migration failed: {ex.Message}");
        // Don't throw - let the app start even if migration fails
    }
}

app.MapControllers();

app.Run();