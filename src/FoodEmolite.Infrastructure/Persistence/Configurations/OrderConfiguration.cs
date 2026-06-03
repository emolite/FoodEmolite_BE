using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class OrderConfiguration
    : IEntityTypeConfiguration<Order>
{
    public void Configure(
        EntityTypeBuilder<Order> builder)
    {
        builder
            .HasIndex(x => x.RefCode)
            .IsUnique();

        builder
            .HasOne(x => x.CustomerAccount)
            .WithMany()
            .HasForeignKey(x => x.CustomerAccountId);

        builder
            .HasOne(x => x.Store)
            .WithMany()
            .HasForeignKey(x => x.StoreRefCode)
            .HasPrincipalKey(x => x.RefCode);
    }
}