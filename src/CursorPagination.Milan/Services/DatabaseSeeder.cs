using CursorPagination.Milan.Data;
using CursorPagination.Milan.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CursorPagination.Milan.Services;

public class DatabaseSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(IServiceProvider serviceProvider, ILogger<DatabaseSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Ensure database is created with latest migrations
        await dbContext.Database.MigrateAsync(cancellationToken);

        // Check if we need to seed
        if (await dbContext.UserNotes.AnyAsync(cancellationToken))
        {
            var count = await dbContext.UserNotes.CountAsync(cancellationToken);
            _logger.LogInformation("Database already contains {Count} records, skipping seeding", count);
            return;
        }

        _logger.LogInformation("Starting database seeding...");

        // Generate 1M records
        var records = GenerateRecords(1_000_000).ToList();
        
        // Get the connection to use Npgsql bulk copy (much faster than EF Core for bulk inserts)
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        // Bulk copy
        await using (var writer = await connection.BeginBinaryImportAsync(
                         "COPY user_notes (id, user_id, note, date) FROM STDIN (FORMAT BINARY)",
                         cancellationToken))
        {
            foreach (var record in records)
            {
                await writer.StartRowAsync(cancellationToken);
                await writer.WriteAsync(record.Id, NpgsqlTypes.NpgsqlDbType.Uuid, cancellationToken);
                await writer.WriteAsync(record.UserId, NpgsqlTypes.NpgsqlDbType.Uuid, cancellationToken);
                await writer.WriteAsync(record.Note, NpgsqlTypes.NpgsqlDbType.Text, cancellationToken);
                await writer.WriteAsync(record.Date, NpgsqlTypes.NpgsqlDbType.Date, cancellationToken);
            }

            await writer.CompleteAsync(cancellationToken);
        }

        _logger.LogInformation("Database seeding completed");
    }

    private static IEnumerable<UserNote> GenerateRecords(int count)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var startDate = new DateOnly(2020, 1, 1);
        var possibleNotes = new[] 
        { 
            "Important meeting notes", 
            "Todo list", 
            "Project ideas",
            "Meeting minutes",
            "Random thoughts",
            null
        };

        for (var i = 0; i < count; i++)
        {
            yield return new UserNote
            {
                Id = Guid.CreateVersion7(),
                UserId = Guid.CreateVersion7(),
                Note = possibleNotes[random.Next(possibleNotes.Length)],
                Date = startDate.AddDays(random.Next(1500)) // Random date within ~4 years
            };
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
