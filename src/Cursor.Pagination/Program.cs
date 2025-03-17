using CursorPagination.Data;
using CursorPagination.Endpoints;
using CursorPagination.Services;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Configure JSON serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Configure PostgreSQL database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
        npgsqlOptions.EnableRetryOnFailure(3);
        npgsqlOptions.CommandTimeout(30);
    });
});

// Register database seeder
builder.Services.AddScoped<DatabaseSeeder>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Initialize the database with seed data
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        // Create and apply migrations if needed
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();

        // Check if migrations exist
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            // Apply migrations if they exist
            await dbContext.Database.MigrateAsync();
        }
        else
        {
            // No migrations, just create the database
            await dbContext.Database.EnsureCreatedAsync();
        }

        // Seed data
        await DatabaseSeeder.InitializeAsync(serviceProvider);
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while initializing the database.");
    }
}

app.UseHttpsRedirection();

// Add health checks endpoint
app.MapHealthChecks("/health");

// Map API endpoints
app.MapUserNotesEndpoints();

app.Run();