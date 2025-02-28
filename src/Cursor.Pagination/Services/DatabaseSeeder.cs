using CursorPagination.Data;
using CursorPagination.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace CursorPagination.Services;

public class DatabaseSeeder(ILogger<DatabaseSeeder> logger, AppDbContext dbContext, CancellationTokenSource cancellationTokenSource = default!)
{
    private readonly CancellationToken _cancellationToken = cancellationTokenSource?.Token ?? CancellationToken.None;

    public async Task SeedAsync()
    {
        // Check if we already have data
        if (await dbContext.UserNotes.AnyAsync(_cancellationToken))
        {
            logger.LogInformation("Database already contains data, skipping seeding");
            return;
        }

        logger.LogInformation("Starting database seeding...");

        // Ensure the database is created
        await dbContext.Database.EnsureCreatedAsync(_cancellationToken);

        // Seed users first (using regular EF Core since it's a smaller dataset)
        await SeedUsersAsync();

        // Generate 1M records
        var records = GenerateRecords(1_000_000).ToList();

        // Get the connection to use Npgsql bulk copy (much faster than EF Core for bulk inserts)
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(_cancellationToken);

        // Bulk copy
        await using (var writer = await connection.BeginBinaryImportAsync(
            "COPY user_notes (id, user_id, title, content, created_at, updated_at) FROM STDIN (FORMAT BINARY)",
            _cancellationToken))
        {
            foreach (var record in records)
            {
                await writer.StartRowAsync(_cancellationToken);
                await writer.WriteAsync(record.Id, NpgsqlDbType.Uuid, _cancellationToken);
                await writer.WriteAsync(record.UserId, NpgsqlDbType.Uuid, _cancellationToken);
                await writer.WriteAsync(record.Title, NpgsqlDbType.Text, _cancellationToken);
                await writer.WriteAsync(record.Content, NpgsqlDbType.Text, _cancellationToken);
                await writer.WriteAsync(record.CreatedAt, NpgsqlDbType.Timestamp, _cancellationToken);
                await writer.WriteAsync(record.UpdatedAt, NpgsqlDbType.Timestamp, _cancellationToken);
            }

            await writer.CompleteAsync(_cancellationToken);
        }

        logger.LogInformation("Database seeding completed successfully");
    }

    private async Task SeedUsersAsync()
    {
        logger.LogInformation("Seeding users...");

        var users = new List<User>();
        for (int i = 1; i <= 1000; i++)
        {
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                Username = $"user{i}",
                Email = $"user{i}@example.com",
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 365))
            });
        }

        await dbContext.Users.AddRangeAsync(users, _cancellationToken);
        await dbContext.SaveChangesAsync(_cancellationToken);

        // Store users in local cache for reference when generating notes
        Users = users;

        logger.LogInformation($"Seeded {users.Count} users");
    }

    // Local cache of users for reference when generating notes
    private List<User> Users { get; set; } = new();

    private IEnumerable<UserNote> GenerateRecords(int count)
    {
        logger.LogInformation($"Generating {count} records...");

        for (int i = 0; i < count; i++)
        {
            var randomUser = Users[Random.Shared.Next(Users.Count)];
            var createdDate = DateTime.UtcNow
                .AddDays(-Random.Shared.Next(0, 365))
                .AddHours(-Random.Shared.Next(0, 24))
                .AddMinutes(-Random.Shared.Next(0, 60));

            yield return new UserNote
            {
                Id = Guid.NewGuid(),
                UserId = randomUser.Id,
                Title = $"Note {i + 1}",
                Content = $"This is content for note {i + 1}. Created by {randomUser.Username}.",
                CreatedAt = createdDate,
                UpdatedAt = createdDate.AddMinutes(Random.Shared.Next(0, 60))
            };

            if (i > 0 && i % 100000 == 0)
            {
                logger.LogInformation($"Generated {i} records so far...");
            }
        }
    }

    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<DatabaseSeeder>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var seeder = new DatabaseSeeder(logger, dbContext, new CancellationTokenSource());
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}