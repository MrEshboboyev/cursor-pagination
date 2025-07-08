using CursorPagination.Milan.Cursors;
using CursorPagination.Milan.Data;
using Microsoft.EntityFrameworkCore;

namespace CursorPagination.Milan.Endpoints;

public static class UserNotesEndpoints
{
    public static void MapUserNotesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/user-notes");

        // Offset pagination
        group.MapGet("/offset", async (
            AppDbContext dbContext,
            int page = 1,
            int pageSize = 10,
            CancellationToken cancellationToken = default) =>
        {
            if (page < 1)
            {
                return Results.BadRequest("Page must be greater than 0");
            }

            if (pageSize < 1)
            {
                return Results.BadRequest("Page size must be greater than 0");
            }

            if (pageSize > 100)
            {
                return Results.BadRequest("Page size must be less than or equal to 100");
            }

            var query = dbContext.UserNotes
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id);

            var totalCount = await query.CountAsync(cancellationToken);
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                HasNextPage = page < totalPages,
                HasPreviousPage = page > 1
            });
        });

        // Cursor pagination
        group.MapGet("/cursor", async (
            AppDbContext dbContext,
            string? cursor = null,
            int limit = 10,
            CancellationToken cancellationToken = default) =>
        {
            if (limit < 1)
            {
                return Results.BadRequest("Limit must be greater than 0");
            }

            if (limit > 100)
            {
                return Results.BadRequest("Limit must be less than or equal to 100");
            }

            var query = dbContext.UserNotes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(cursor))
            {
                var decodedCursor = Cursor.Decode(cursor);

                if (decodedCursor is null)
                {
                    return Results.BadRequest("Invalid cursor");
                }

                //query = query.Where(x => x.Date < decodedCursor.Date ||
                //                         x.Date == decodedCursor.Date && x.Id <= decodedCursor.LastId);

                query = query.Where(x => EF.Functions.LessThanOrEqual(
                    ValueTuple.Create(x.Date, x.Id),
                    ValueTuple.Create(decodedCursor.Date, decodedCursor.LastId)));
            }

            var items = await query
                .OrderByDescending(x => x.Date)
                .ThenByDescending(x => x.Id)
                .Take(limit + 1)
                .ToListAsync(cancellationToken);

            var hasMore = items.Count > limit;
            DateOnly? nextDate = hasMore ? items[^1].Date : null;
            Guid? nextId = hasMore ? items[^1].Id : null;

            items.RemoveAt(items.Count - 1);

            return Results.Ok(new
            {
                Items = items,
                Cursor = nextDate is not null && nextId is not null ?
                    Cursor.Encode(nextDate.Value, nextId.Value)
                    : null,
                HasMore = hasMore
            });
        });
    }
}
