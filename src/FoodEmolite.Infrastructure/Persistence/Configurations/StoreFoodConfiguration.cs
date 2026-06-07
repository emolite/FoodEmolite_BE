using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class StoreFoodConfiguration : IEntityTypeConfiguration<StoreFood>
{
    public void Configure(EntityTypeBuilder<StoreFood> builder)
    {
    }
}