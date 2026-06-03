using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class OrderHistoryConfiguration
    : IEntityTypeConfiguration<OrderHistory>
{
    public void Configure(
        EntityTypeBuilder<OrderHistory> builder)
    {
        builder
            .HasIndex(x => x.RefCode)
            .IsUnique();

        builder
            .HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId);
    }
}