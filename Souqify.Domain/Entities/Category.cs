using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Souqify.Domain.Entities
{

    public class Category
    {
        public Guid Id { get; set; }

        public required string Name { get; set; } = null!; // "Graphic Cards"

        public string Slug { get; set; } = null!;          //"graphic-cards"
        public Guid? ParentId { get; set; }
        public int DisplayOrder { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public Category? Parent { get; set; }
        public List<Category> Children { get; set; } = new();
        //public List<CategorySpec> Specs { get; set; } = new();

        public List<Product> Products { get; set; } = new List<Product>();
    }
}
