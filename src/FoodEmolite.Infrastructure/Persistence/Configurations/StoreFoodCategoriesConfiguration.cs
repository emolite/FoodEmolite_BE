using FoodEmolite.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Infrastructure.Persistence.Configurations
{
    public class StoreFoodCategoriesConfiguration : IEntityTypeConfiguration<StoreFoodCategories>
    {
        public void Configure(EntityTypeBuilder<StoreFoodCategories> builder)
        {
        }
    }
}
