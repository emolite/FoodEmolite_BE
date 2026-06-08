using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration
    : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(
        EntityTypeBuilder<OrderItem> builder)
    {
    }
}