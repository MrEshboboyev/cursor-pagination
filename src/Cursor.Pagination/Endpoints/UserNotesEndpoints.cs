using CursorPagination.Data;
using CursorPagination.Models;
using CursorPagination.Models.DTOs;
using CursorPagination.Models.Pagination;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace CursorPagination.Endpoints;

public static class UserNotesEndpoints
{
    public static void MapUserNotesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/notes").WithTags("Notes");

        // Get notes with cursor pagination
        group.MapGet("/", GetNotesAsync)
            .WithName("GetNotes")
            .WithOpenApi(operation =>
            {
                operation.Description = "Gets a paginated list of notes using cursor pagination";
                return operation;
            });

        // Get a single note
        group.MapGet("/{id:guid}", GetNoteAsync)
            .WithName("GetNote")
            .WithOpenApi();

        // Create a note
        group.MapPost("/", CreateNoteAsync)
            .WithName("CreateNote")
            .WithOpenApi();

        // Update a note
        group.MapPut("/{id:guid}", UpdateNoteAsync)
            .WithName("UpdateNote")
            .WithOpenApi();

        // Delete a note
        group.MapDelete("/{id:guid}", DeleteNoteAsync)
            .WithName("DeleteNote")
            .WithOpenApi();
    }

    private static async Task<Results<Ok<CursorPagedResponse<UserNoteDto>>, BadRequest<string>>> GetNotesAsync(
        AppDbContext db,
        ILogger<Program> logger,
        [AsParameters] CursorPagedRequest request)
    {
        try
        {
            logger.LogInformation("Getting notes with cursor: {Cursor}, limit: {Limit}",
                request.Cursor, request.Limit);

            // Parse the cursor if provided
            Cursor? cursor = null;
            if (!string.IsNullOrEmpty(request.Cursor) && !Cursor.TryDecode(request.Cursor, out cursor))
            {
                return TypedResults.BadRequest("Invalid cursor format");
            }

            // Start with the base query
            IQueryable<UserNote> query = db.UserNotes
                .OrderByDescending(n => n.CreatedAt)
                .ThenBy(n => n.Id)
                .AsNoTracking(); // Performance optimization for read-only queries

            // Apply cursor filtering if cursor is provided
            if (cursor != null)
            {
                // This query is optimized for PostgreSQL and uses the composite index on (created_at, id)
                query = query.Where(n =>
                    n.CreatedAt < cursor.CreatedAt ||
                    n.CreatedAt == cursor.CreatedAt && n.Id.CompareTo(cursor.Id) > 0);
            }

            // Get one extra item to determine if there are more pages
            var notes = await query
                .Take(request.Limit + 1)
                .ToListAsync();

            // Determine if there are more items
            bool hasMore = notes.Count > request.Limit;
            if (hasMore)
            {
                notes.RemoveAt(notes.Count - 1);
            }

            // Get usernames for the notes - use a dictionary for efficient lookups
            var userIds = notes.Select(n => n.UserId).Distinct().ToList();
            var users = await db.Users
                .Where(u => userIds.Contains(u.Id))
                .AsNoTracking()
                .ToDictionaryAsync(u => u.Id, u => u.Username);

            // Create DTOs
            var dtos = notes.Select(n => UserNoteDto.FromModel(n,
                users.TryGetValue(n.UserId, out var username) ? username : "Unknown")).ToList();

            // Create the next cursor
            string? nextCursor = null;
            if (hasMore && notes.Count > 0)
            {
                var lastNote = notes.Last();
                var nextCursorObj = new Cursor
                {
                    CreatedAt = lastNote.CreatedAt,
                    Id = lastNote.Id
                };
                nextCursor = nextCursorObj.Encode();
            }

            // Create and return the response
            var response = new CursorPagedResponse<UserNoteDto>
            {
                Items = dtos,
                NextCursor = nextCursor
            };

            return TypedResults.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving notes");
            return TypedResults.BadRequest($"Error retrieving notes: {ex.Message}");
        }
    }

    private static async Task<Results<Ok<UserNoteDto>, NotFound, BadRequest<string>>> GetNoteAsync(
        Guid id,
        AppDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Getting note with ID: {Id}", id);

            var note = await db.UserNotes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
            {
                return TypedResults.NotFound();
            }

            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == note.UserId);

            var username = user?.Username ?? "Unknown";

            return TypedResults.Ok(UserNoteDto.FromModel(note, username));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving note with ID: {Id}", id);
            return TypedResults.BadRequest($"Error retrieving note: {ex.Message}");
        }
    }

    private static async Task<Results<Created<UserNoteDto>, BadRequest<string>>> CreateNoteAsync(
        UserNote note,
        AppDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Creating new note for user ID: {UserId}", note.UserId);

            // Ensure ID is set
            if (note.Id == Guid.Empty)
            {
                note.Id = Guid.NewGuid();
            }

            // Set timestamps
            var now = DateTime.UtcNow;
            note.CreatedAt = now;
            note.UpdatedAt = now;

            // Validate user exists
            var userExists = await db.Users.AnyAsync(u => u.Id == note.UserId);
            if (!userExists)
            {
                return TypedResults.BadRequest($"User with ID {note.UserId} not found");
            }

            db.UserNotes.Add(note);
            await db.SaveChangesAsync();

            // Get the username for the DTO
            var username = await db.Users
                .Where(u => u.Id == note.UserId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "Unknown";

            var dto = UserNoteDto.FromModel(note, username);
            return TypedResults.Created($"/api/notes/{note.Id}", dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating note");
            return TypedResults.BadRequest($"Error creating note: {ex.Message}");
        }
    }

    private static async Task<Results<Ok<UserNoteDto>, NotFound, BadRequest<string>>> UpdateNoteAsync(
        Guid id,
        UserNote updatedNote,
        AppDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Updating note with ID: {Id}", id);

            var existingNote = await db.UserNotes.FindAsync(id);
            if (existingNote == null)
            {
                return TypedResults.NotFound();
            }

            // Update properties but preserve others
            existingNote.Title = updatedNote.Title;
            existingNote.Content = updatedNote.Content;
            existingNote.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            // Get the username for the DTO
            var username = await db.Users
                .Where(u => u.Id == existingNote.UserId)
                .Select(u => u.Username)
                .FirstOrDefaultAsync() ?? "Unknown";

            return TypedResults.Ok(UserNoteDto.FromModel(existingNote, username));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating note with ID: {Id}", id);
            return TypedResults.BadRequest($"Error updating note: {ex.Message}");
        }
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> DeleteNoteAsync(
        Guid id,
        AppDbContext db,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Deleting note with ID: {Id}", id);

            var note = await db.UserNotes.FindAsync(id);
            if (note == null)
            {
                return TypedResults.NotFound();
            }

            db.UserNotes.Remove(note);
            await db.SaveChangesAsync();

            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting note with ID: {Id}", id);
            return TypedResults.BadRequest($"Error deleting note: {ex.Message}");
        }
    }
}