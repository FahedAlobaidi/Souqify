
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Souqify.Infrastructure.Auditing
{
    internal class AuditEntry
    {
        public EntityEntry Entry { get; set; } = null!;
        public string Action { get; set; } = null!;
        public string EntityName { get; set; } = null!;         
        public Dictionary<string, object?> OldValues { get; set; } = new();
        public Dictionary<string, object?> NewValues { get; set; } = new();

    }
}
