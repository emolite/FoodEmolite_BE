using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class StoreFoodConfiguration : IEntityTypeConfiguration<StoreFood>
{
    public void Configure(EntityTypeBuilder<StoreFood> builder)
    {
        builder
            .HasIndex(x => x.RefCode)
            .IsUnique();

        builder
            .HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreRefCode)
            .HasPrincipalKey(x => x.RefCode);
    }
}