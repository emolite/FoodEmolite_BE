using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class FileUrlConfiguration
    : IEntityTypeConfiguration<FileUrl>
{
    public void Configure(
        EntityTypeBuilder<FileUrl> builder)
    {
        builder
            .HasIndex(x => x.RefCode)
            .IsUnique();
    }
}