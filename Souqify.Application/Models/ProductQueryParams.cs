using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Models
{
    public class ProductQueryParams
    {
        public string? Brand { get; set; }

        public string? Category { get; set; }

        public decimal? MinPrice { get; set; }

        public decimal? MaxPrice { get; set; }

        public string? Sort { get; set; }

        public int CurrentPage { get; set; } = 1;

        public int PageSize { get; set; } = 12;
    }
}
