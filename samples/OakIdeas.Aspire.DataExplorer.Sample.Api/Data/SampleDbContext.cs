using Microsoft.EntityFrameworkCore;

namespace OakIdeas.Aspire.DataExplorer.Sample.Api.Data;

public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options) : DbContext(options)
{
    public DbSet<TodoItem> TodoItems => Set<TodoItem>();
    public DbSet<TodoList> TodoLists => Set<TodoList>();
    public DbSet<TodoCategory> TodoCategories => Set<TodoCategory>();
    public DbSet<TodoTag> TodoTags => Set<TodoTag>();
    public DbSet<TodoItemTag> TodoItemTags => Set<TodoItemTag>();
    public DbSet<TodoPriority> TodoPriorities => Set<TodoPriority>();
    public DbSet<TodoStatus> TodoStatuses => Set<TodoStatus>();
    public DbSet<TodoComment> TodoComments => Set<TodoComment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoList>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(300);
            entity.Property(e => e.IsArchived).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new TodoList { Id = 1, Name = "Work", Description = "Tasks related to DataExplorer development", CreatedAt = new DateTimeOffset(2026, 1, 5, 9, 0, 0, TimeSpan.Zero) },
                new TodoList { Id = 2, Name = "Personal", Description = "Personal goals and errands", CreatedAt = new DateTimeOffset(2026, 1, 5, 9, 30, 0, TimeSpan.Zero) });
        });

        modelBuilder.Entity<TodoCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(80).IsRequired();
            entity.Property(e => e.ColorHex).HasMaxLength(12).IsRequired().HasDefaultValue("#64748b");
            entity.Property(e => e.IsSystem).HasDefaultValue(false);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new TodoCategory { Id = 1, Name = "Engineering", ColorHex = "#2563eb", IsSystem = true },
                new TodoCategory { Id = 2, Name = "Household", ColorHex = "#16a34a", IsSystem = false },
                new TodoCategory { Id = 3, Name = "Learning", ColorHex = "#7c3aed", IsSystem = false });
        });

        modelBuilder.Entity<TodoTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(60).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(200);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new TodoTag { Id = 1, Name = "aspire", Description = "Aspire integration" },
                new TodoTag { Id = 2, Name = "ef-core", Description = "Entity Framework changes" },
                new TodoTag { Id = 3, Name = "docs", Description = "Documentation updates" },
                new TodoTag { Id = 4, Name = "urgent", Description = "Needs immediate attention" },
                new TodoTag { Id = 5, Name = "backend", Description = "API and service work" });
        });

        modelBuilder.Entity<TodoPriority>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(40).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new TodoPriority { Id = 1, Name = "Low", SortOrder = 1, IsDefault = false },
                new TodoPriority { Id = 2, Name = "Medium", SortOrder = 2, IsDefault = true },
                new TodoPriority { Id = 3, Name = "High", SortOrder = 3, IsDefault = false },
                new TodoPriority { Id = 4, Name = "Critical", SortOrder = 4, IsDefault = false });
        });

        modelBuilder.Entity<TodoStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).HasMaxLength(40).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasData(
                new TodoStatus { Id = 1, Name = "Not Started", SortOrder = 1, IsClosed = false },
                new TodoStatus { Id = 2, Name = "In Progress", SortOrder = 2, IsClosed = false },
                new TodoStatus { Id = 3, Name = "Blocked", SortOrder = 3, IsClosed = false },
                new TodoStatus { Id = 4, Name = "Completed", SortOrder = 4, IsClosed = true });
        });

        modelBuilder.Entity<TodoItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.IsArchived).HasDefaultValue(false);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasOne(e => e.TodoList)
                .WithMany(e => e.TodoItems)
                .HasForeignKey(e => e.TodoListId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TodoCategory)
                .WithMany(e => e.TodoItems)
                .HasForeignKey(e => e.TodoCategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TodoPriority)
                .WithMany(e => e.TodoItems)
                .HasForeignKey(e => e.TodoPriorityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TodoStatus)
                .WithMany(e => e.TodoItems)
                .HasForeignKey(e => e.TodoStatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.TodoListId, e.TodoStatusId, e.DueAt });
            entity.HasIndex(e => e.CreatedAt);
            entity.ToTable(t => t.HasCheckConstraint("CK_TodoItems_Title_NotBlank", "LEN(LTRIM(RTRIM([Title]))) > 0"));

            entity.HasData(
                new TodoItem
                {
                    Id = 1,
                    Title = "Instrument sample metadata discovery",
                    Description = "Add richer schema objects so DataExplorer shows meaningful relationships and constraints.",
                    TodoListId = 1,
                    TodoCategoryId = 1,
                    TodoPriorityId = 3,
                    TodoStatusId = 2,
                    DueAt = new DateTimeOffset(2026, 1, 15, 17, 0, 0, TimeSpan.Zero),
                    CreatedAt = new DateTimeOffset(2026, 1, 10, 9, 0, 0, TimeSpan.Zero),
                    UpdatedAt = new DateTimeOffset(2026, 1, 11, 11, 0, 0, TimeSpan.Zero),
                    IsArchived = false
                },
                new TodoItem
                {
                    Id = 2,
                    Title = "Write migration walkthrough docs",
                    Description = "Document local migration commands and seeded data expectations.",
                    TodoListId = 1,
                    TodoCategoryId = 3,
                    TodoPriorityId = 2,
                    TodoStatusId = 1,
                    DueAt = null,
                    CreatedAt = new DateTimeOffset(2026, 1, 11, 8, 30, 0, TimeSpan.Zero),
                    UpdatedAt = null,
                    IsArchived = false
                },
                new TodoItem
                {
                    Id = 3,
                    Title = "Refactor sample page styles",
                    Description = null,
                    TodoListId = 1,
                    TodoCategoryId = 1,
                    TodoPriorityId = 2,
                    TodoStatusId = 3,
                    DueAt = new DateTimeOffset(2026, 1, 18, 15, 30, 0, TimeSpan.Zero),
                    CreatedAt = new DateTimeOffset(2026, 1, 12, 10, 15, 0, TimeSpan.Zero),
                    UpdatedAt = null,
                    IsArchived = false
                },
                new TodoItem
                {
                    Id = 4,
                    Title = "Plan weekend errands",
                    Description = "Buy groceries and prep for Monday.",
                    TodoListId = 2,
                    TodoCategoryId = 2,
                    TodoPriorityId = 1,
                    TodoStatusId = 4,
                    DueAt = new DateTimeOffset(2026, 1, 13, 13, 0, 0, TimeSpan.Zero),
                    CompletedAt = new DateTimeOffset(2026, 1, 13, 12, 0, 0, TimeSpan.Zero),
                    CreatedAt = new DateTimeOffset(2026, 1, 10, 7, 45, 0, TimeSpan.Zero),
                    UpdatedAt = new DateTimeOffset(2026, 1, 13, 12, 0, 0, TimeSpan.Zero),
                    IsArchived = false
                });
        });

        modelBuilder.Entity<TodoItemTag>(entity =>
        {
            entity.HasKey(e => new { e.TodoItemId, e.TodoTagId });
            entity.Property(e => e.AddedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.TodoItem)
                .WithMany(e => e.TodoItemTags)
                .HasForeignKey(e => e.TodoItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TodoTag)
                .WithMany(e => e.TodoItemTags)
                .HasForeignKey(e => e.TodoTagId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.TodoTagId);

            entity.HasData(
                new TodoItemTag { TodoItemId = 1, TodoTagId = 1, AddedAt = new DateTimeOffset(2026, 1, 10, 9, 5, 0, TimeSpan.Zero) },
                new TodoItemTag { TodoItemId = 1, TodoTagId = 2, AddedAt = new DateTimeOffset(2026, 1, 10, 9, 5, 0, TimeSpan.Zero) },
                new TodoItemTag { TodoItemId = 2, TodoTagId = 3, AddedAt = new DateTimeOffset(2026, 1, 11, 8, 40, 0, TimeSpan.Zero) },
                new TodoItemTag { TodoItemId = 3, TodoTagId = 4, AddedAt = new DateTimeOffset(2026, 1, 12, 10, 20, 0, TimeSpan.Zero) },
                new TodoItemTag { TodoItemId = 3, TodoTagId = 5, AddedAt = new DateTimeOffset(2026, 1, 12, 10, 20, 0, TimeSpan.Zero) },
                new TodoItemTag { TodoItemId = 4, TodoTagId = 2, AddedAt = new DateTimeOffset(2026, 1, 10, 8, 0, 0, TimeSpan.Zero) });
        });

        modelBuilder.Entity<TodoComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AuthorName).HasMaxLength(80).IsRequired();
            entity.Property(e => e.Body).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("SYSUTCDATETIME()");

            entity.HasOne(e => e.TodoItem)
                .WithMany(e => e.TodoComments)
                .HasForeignKey(e => e.TodoItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.TodoItemId, e.CreatedAt });

            entity.HasData(
                new TodoComment { Id = 1, TodoItemId = 1, AuthorName = "System", Body = "Kickoff item seeded for demo.", CreatedAt = new DateTimeOffset(2026, 1, 10, 9, 10, 0, TimeSpan.Zero) },
                new TodoComment { Id = 2, TodoItemId = 1, AuthorName = "Sam", Body = "Remember to validate indexes and constraints in DataExplorer.", CreatedAt = new DateTimeOffset(2026, 1, 11, 11, 30, 0, TimeSpan.Zero) },
                new TodoComment { Id = 3, TodoItemId = 3, AuthorName = "Alex", Body = "Blocked waiting for color palette review.", CreatedAt = new DateTimeOffset(2026, 1, 12, 10, 30, 0, TimeSpan.Zero) },
                new TodoComment { Id = 4, TodoItemId = 4, AuthorName = "Taylor", Body = "Errands complete early.", CreatedAt = new DateTimeOffset(2026, 1, 13, 12, 5, 0, TimeSpan.Zero) });
        });
    }
}
