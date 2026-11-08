using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.DTOs.Category
{
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Slug { get; set; }
        public int DisplayOrder { get; set; }
        public Guid? ParentId { get; set; }
        public string? Description { get; set; }
    }
}
