# 🚀 Cursor Pagination for ASP.NET Core

A high-performance, production-ready implementation of cursor-based pagination for ASP.NET Core 9.0 with PostgreSQL.

## ✨ Features

- 📃 **Cursor-Based Pagination**: Efficient pagination for large datasets using cursor tokens
- 🔍 **Optimized Queries**: Designed specifically for PostgreSQL performance
- 🔄 **Bulk Data Operations**: Fast data seeding with Npgsql binary import
- 📊 **Million Record Test Set**: Includes tools to generate and seed a million test records
- 🛡️ **Type Safety**: Fully type-safe C# implementation with modern language features
- 📝 **Clean API Design**: Minimal API endpoints with clear request/response models
- 🔄 **Health Checks**: Built-in health check endpoint
- 🧪 **Production Ready**: Includes retries, connection pooling, and error handling

## 🔧 Technical Stack

- ASP.NET Core 9.0
- Entity Framework Core 9.0
- PostgreSQL
- Npgsql Provider
- Minimal API pattern

## 🏁 Getting Started

### Prerequisites

- .NET 9.0 SDK
- PostgreSQL 15+ (running locally or in a container)

### Configuration

Update the connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=CursorPagination_DB;Username=postgres;Password=postgres;Port=5432;Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100;Connection Idle Lifetime=300"
}
```

### Running the Application

```bash
cd src/Cursor.Pagination
dotnet run
```

On first run, the application will:
1. Create the database schema
2. Seed 1,000 users
3. Generate 1,000,000 notes records distributed among users
4. Start the API server

## 🌐 API Endpoints

### Notes API

| Method | Endpoint          | Description                               |
|--------|-------------------|-------------------------------------------|
| GET    | `/api/notes`      | Get paginated notes with cursor pagination |
| GET    | `/api/notes/{id}` | Get a specific note by ID                 |
| POST   | `/api/notes`      | Create a new note                         |
| PUT    | `/api/notes/{id}` | Update an existing note                   |
| DELETE | `/api/notes/{id}` | Delete a note                             |

### Health Check

| Method | Endpoint   | Description                     |
|--------|------------|---------------------------------|
| GET    | `/health`  | Check API and database health   |

## 📦 Pagination Request Parameters

```
GET /api/notes?limit=10&cursor=eyJDcmVhdGVkQXQiOiIyMDI0LTAzLTE3VDIzOjU5OjE1LjEyMzQ1NloiLCJJZCI6IjJiM2Y0NTlmLTlhZDEtNDU2Ny04MmY0LWZkMDdjZmVmYTZhZCJ9
```

| Parameter | Description                                      | Default |
|-----------|--------------------------------------------------|---------|
| `limit`   | Number of items per page (1-100)                 | 50      |
| `cursor`  | Cursor token for pagination (from previous page) | null    |
| `userId`  | Optional filter by user ID                       | null    |
| `searchTerm` | Optional search by title or content           | null    |
| `startDate` | Optional filter by creation date (min)         | null    |
| `endDate` | Optional filter by creation date (max)           | null    |

## 📊 Pagination Response Format

```json
{
  "items": [
    {
      "id": "2b3f459f-9ad1-4567-82f4-fd07cfefa6ad",
      "userId": "a1b2c3d4-e5f6-4321-a9b8-c7d6e5f4a3b2",
      "userName": "user42",
      "title": "Note 42",
      "content": "This is content for note 42. Created by user42.",
      "createdAt": "2024-03-16T12:34:56Z",
      "updatedAt": "2024-03-16T12:45:22Z",
      "createdTimeAgo": "1 day ago"
    },
    // ... more items
  ],
  "nextCursor": "eyJDcmVhdGVkQXQiOiIyMDI0LTAzLTE2VDExOjIyOjMzLjQ1NjcxMloiLCJJZCI6ImU3ZDZmNWE0LWMzYjItNDgwNS05NWJlLTc2MTIzZDRlNWY2NyJ9",
  "hasMore": true
}
```

## 🔍 How Cursor Pagination Works

1. The client requests a page of data with a limit (e.g., 50 items)
2. The server returns the items and a cursor token pointing to the next page
3. The client uses the cursor token to request the next page
4. The server uses the cursor to efficiently find the next set of results

Benefits:
- Consistent results even when data changes
- No duplicate records or skipped items
- Efficient for large datasets
- Works well with database indexes

## 🗃️ Database Schema

### Users Table

```sql
CREATE TABLE users (
    id UUID PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    email VARCHAR(100) NOT NULL UNIQUE,
    created_at TIMESTAMP NOT NULL
);
```

### Notes Table

```sql
CREATE TABLE user_notes (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    title VARCHAR(200) NOT NULL,
    content TEXT,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP NOT NULL,
    CONSTRAINT fk_user FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE INDEX ix_user_notes_user_id ON user_notes(user_id);
CREATE INDEX ix_user_notes_created_at_id ON user_notes(created_at, id);
```

## 🔧 Performance Optimizations

- **Composite Index**: Optimized for cursor pagination with `(created_at, id)` index
- **Bulk Import**: Uses Npgsql binary import for efficient data seeding
- **Connection Pooling**: Configured for optimal connection reuse
- **AsNoTracking**: Used for read-only queries to reduce memory usage
- **Efficient Filtering**: Cursor conditions use indexes effectively

## 📚 Key Code Components

### Cursor Implementation

```csharp
public class Cursor
{
    public DateTime CreatedAt { get; set; }
    public Guid Id { get; set; }

    // Convert cursor to string
    public string Encode()
    {
        var json = JsonSerializer.Serialize(this);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    // Try to decode cursor from string
    public static bool TryDecode(string? encodedCursor, out Cursor? result)
    {
        // Implementation...
    }
}
```

### Pagination Query

```csharp
// Apply cursor filtering if cursor is provided
if (cursor != null)
{
    // This query is optimized for PostgreSQL and uses the composite index
    query = query.Where(n =>
        n.CreatedAt < cursor.CreatedAt ||
        n.CreatedAt == cursor.CreatedAt && n.Id.CompareTo(cursor.Id) > 0);
}
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.
