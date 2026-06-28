using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TodoTask> Tasks => Set<TodoTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TBL_User
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("TBL_User");
            e.HasKey(u => u.Id);
            e.Property(u => u.Username).HasMaxLength(100).IsRequired();
            e.Property(u => u.Email).HasMaxLength(255).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.HasQueryFilter(u => !u.IsDeleted);
        });

        // TBL_Project
        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("TBL_Project");
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).HasMaxLength(200).IsRequired();
            e.Property(p => p.Description).HasMaxLength(1000);
            e.Property(p => p.Status).HasMaxLength(50).IsRequired();
            e.Property(p => p.Priority).HasMaxLength(50).IsRequired();
            e.HasIndex(p => new { p.UserId, p.IsDeleted }).HasDatabaseName("IX_TBL_Project_UserId_IsDeleted");
            e.HasOne(p => p.User).WithMany(u => u.Projects).HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(p => !p.IsDeleted);
        });

        // TBL_Task
        modelBuilder.Entity<TodoTask>(e =>
        {
            e.ToTable("TBL_Task");
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).HasMaxLength(300).IsRequired();
            e.Property(t => t.Description).HasMaxLength(2000);
            e.Property(t => t.Priority).HasMaxLength(50).IsRequired();
            e.Property(t => t.Status).HasMaxLength(50).IsRequired();
            e.HasIndex(t => new { t.ProjectId, t.UserId, t.IsDeleted }).HasDatabaseName("IX_TBL_Task_ProjectId_UserId_IsDeleted");
            e.HasIndex(t => new { t.Status, t.Priority }).HasDatabaseName("IX_TBL_Task_Status_Priority");
            e.HasOne(t => t.Project).WithMany(p => p.Tasks).HasForeignKey(t => t.ProjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.User).WithMany(u => u.Tasks).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(t => !t.IsDeleted);
        });
    }
}
