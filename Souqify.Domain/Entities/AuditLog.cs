namespace Souqify.Domain.Entities
{
    public class AuditLog
    {
        public Guid Id { get; set; }

        //who
        public Guid? UserId { get; set; }
        public string? UserEmail { get; set; }
        public string? IpAddress { get; set; }

        //what
        public string Action { get; set; } = null!;//"created","updated","deleted"
        public string EntityName { get; set; } = null!;//"product","order" ...etc
        public string? EntityId { get; set; }//PK of effected row

        //changes
        public string? OldValues { get; set; } 
        public string? NewValues { get; set; } 

        //when
        public DateTime Timestamp { get; set; }

        //outcome
        public bool Succeeded { get; set; }
        public string? ErrorMessage { get; set; } 
    }
}
