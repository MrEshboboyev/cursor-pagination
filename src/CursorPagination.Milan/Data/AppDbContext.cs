using CursorPagination.Milan.Models;
using Microsoft.EntityFrameworkCore;

namespace CursorPagination.Milan.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserNote> UserNotes => Set<UserNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserNote>(entity =>
        {
            entity.ToTable("user_notes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.Date).IsRequired();

            entity.HasIndex(e => e.UserId);
        });
    }
} 
