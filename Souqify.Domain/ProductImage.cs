using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Domain
{
    
    public class ProductImage
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }

        public required string ImageUrl { get; set; } = null!;

        public int DisplayOrder { get; set; }//which image shows first

        public bool IsMain { get; set; }//the thumbnail/primary image

        public Product? Product { get; set; }
    }
}
