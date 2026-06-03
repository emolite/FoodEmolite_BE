using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class AccountProfileConfiguration : IEntityTypeConfiguration<AccountProfile>
{
    public void Configure(EntityTypeBuilder<AccountProfile> builder)
    {
        builder
            .HasIndex(x => x.RefCode)
            .IsUnique();

        builder
            .HasOne(x => x.Account)
            .WithOne()
            .HasForeignKey<AccountProfile>(
                x => x.AccountId);
    }
}