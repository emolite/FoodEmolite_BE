using FoodEmolite.Domain.Entities;
using FoodEmolite.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace FoodEmolite.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(
    ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entityTypes = typeof(Account).Assembly
            .GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && typeof(BaseEntity).IsAssignableFrom(t));

        foreach (var type in entityTypes)
        {
            modelBuilder.Entity(type);
        }

        ConfigureDateTime(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(AppDbContext).Assembly);
    }

    private static void ConfigureDateTime(
        ModelBuilder modelBuilder)
    {
        var properties = modelBuilder
            .Model
            .GetEntityTypes()
            .SelectMany(x => x.GetProperties())
            .Where(x =>
                x.ClrType == typeof(DateTime) ||
                x.ClrType == typeof(DateTime?));

        foreach (var property in properties)
        {
            property.SetColumnType(
                "timestamp without time zone");
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.Now;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.Now;
            }
        }

        return base.SaveChangesAsync(
            cancellationToken);
    }
}