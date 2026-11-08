using Souqify.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Domain.Entities
{
    public class SpecAttribute
    {
        public Guid Id { get; set; }
        public required string Key { get; set; }    // "vram_gb"

        public required string Name { get; set; }   // "Video Memory"
        public SpecDataType DataType { get; set; }  // Number
        public string? Unit { get; set; }  // "GB"
        public List<SpecOption> Options { get; set; } = new();
    }
}
