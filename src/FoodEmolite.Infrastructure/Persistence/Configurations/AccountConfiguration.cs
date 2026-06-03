using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(
        EntityTypeBuilder<Account> builder)
    {
        builder
            .HasIndex(x => x.RefCode)
            .IsUnique();

        builder
            .HasIndex(x => x.Email)
            .IsUnique();
    }
}