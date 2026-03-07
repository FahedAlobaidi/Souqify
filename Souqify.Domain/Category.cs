using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Souqify.Domain
{
    
    public class Category
    {
        public Guid Id { get; set; }

        public required string Name { get; set; } = null!;

        public string Description { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public List<Product> Products { get; set; } = new List<Product>();
    }
}
