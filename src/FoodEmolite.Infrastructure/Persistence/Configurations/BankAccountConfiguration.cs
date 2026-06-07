using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class BankAccountConfiguration
    : IEntityTypeConfiguration<BankAccount>
{
    public void Configure(
        EntityTypeBuilder<BankAccount> builder)
    {
    }
}