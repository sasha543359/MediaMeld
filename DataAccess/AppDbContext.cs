using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

public class AppDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Video> Videos { get; set; }
    public DbSet<Payment> Payments { get; set; }

    // Добавляем конструктор, принимающий DbContextOptions
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
        Database.EnsureCreated();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Определяем, что у User много Video
        modelBuilder.Entity<User>()
            .HasMany(u => u.Videos)
            .WithOne(v => v.User)
            .HasForeignKey(v => v.UserId);

        modelBuilder.Entity<User>()
        .HasMany(u => u.Payments)
        .WithOne(p => p.User)
        .HasForeignKey(p => p.UserId);

        base.OnModelCreating(modelBuilder);
    }
}