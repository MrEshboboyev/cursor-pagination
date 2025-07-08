using CursorPagination.Milan.Data;
using CursorPagination.Milan.Endpoints;
using CursorPagination.Milan.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database"));
    options.UseSnakeCaseNamingConvention();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddNpgsql())
    .UseOtlpExporter();

builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
});

// Add database seeder
builder.Services.AddHostedService<DatabaseSeeder>();

var app = builder.Build();

// Map endpoints
app.MapUserNotesEndpoints();

app.Run();
