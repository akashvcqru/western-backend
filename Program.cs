using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using western_backend;
using western_backend.Models;

var builder = WebApplication.CreateBuilder(args);

// Add Connection String & DbContext
var connectionString = builder.Configuration.GetConnectionString("constr") ?? "Data Source=western.db;Cache=Shared;Busy Timeout=5000";

if (args.Contains("--migrate"))
{
    DataMigrator.Migrate("Data Source=western.db", connectionString);
    return;
}

if (args.Contains("--generate-schema"))
{
    var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
    // Use SQL Server configuration to ensure generating SQL Server compatible schema creation script
    optionsBuilder.UseSqlServer(connectionString);
    using var db = new AppDbContext(optionsBuilder.Options);
    var schema = db.Database.GenerateCreateScript();
    File.WriteAllText("schema.sql", schema);
    Console.WriteLine("[Schema] SQL Server compatible schema script written to schema.sql");
    return;
}


// Check connection and fallback to SQLite if SQL Server is configured but unreachable
var activeConnectionString = connectionString;
bool isSqlite = activeConnectionString.Contains(".db") || activeConnectionString.Contains("filename=", StringComparison.OrdinalIgnoreCase);

if (!isSqlite)
{
    try
    {
        var connBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(activeConnectionString);
        var dataSource = connBuilder.DataSource;
        string host = "localhost";
        int port = 1433;
        
        if (dataSource.Contains(","))
        {
            var parts = dataSource.Split(',');
            host = parts[0].Trim();
            int.TryParse(parts[1].Trim(), out port);
        }
        else
        {
            host = dataSource.Trim();
        }
        
        using var client = new System.Net.Sockets.TcpClient();
        var result = client.BeginConnect(host, port, null, null);
        var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
        if (!success)
        {
            throw new TimeoutException("Connection timed out");
        }
        client.EndConnect(result);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database Setup] SQL Server database at '{activeConnectionString}' is unreachable: {ex.Message}");
        Console.WriteLine("[Database Setup] Falling back to local SQLite database 'western.db' for development.");
        activeConnectionString = "Data Source=western.db";
        isSqlite = true;
    }
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (isSqlite)
    {
        options.UseSqlite(activeConnectionString, sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(30);
        });
    }
    else
    {
        options.UseSqlServer(activeConnectionString);
    }
});

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Secret"] ?? "SuperSecretKeyEnsureThisIsAtLeast32CharactersLongToAvoidCryptographicExceptions!";
var issuer = jwtSettings["Issuer"] ?? "http://localhost:5073";
var audience = jwtSettings["Audience"] ?? "http://localhost:3000";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger with JWT Bearer support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Western CMS API", Version = "v1" });
    
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer {token}' in the text box below.",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true) // Enable Swagger in all environments for ease of review
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Western CMS API v1");
    });
}

app.UseCors("AllowAll");

// Configure Static Files for serving uploads
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}
app.UseStaticFiles(); // Default wwwroot
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Execute Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        
        // Resolve frontend data path
        var dataPath = @"d:\Aditya\western\western-frontend\src\data";
        if (!Directory.Exists(dataPath))
        {
            dataPath = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "western-frontend", "src", "data"));
        }
        
        Console.WriteLine($"[Seeding] Seeding SQLite database from source path: {dataPath}");
        DbInitializer.Initialize(db, dataPath);
        Console.WriteLine("[Seeding] Database initialization & seeding completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
