using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoodEmolite.Shared.Entities
{
    public class BaseSearchRequest<T>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        public string? SortBy { get; set; }
        public bool Asc { get; set; } = false;

        public T? SearchParams { get; set; }
    }
}
