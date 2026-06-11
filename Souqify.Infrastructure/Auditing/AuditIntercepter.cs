

using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Souqify.Domain.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace Souqify.Infrastructure.Auditing
{
    public class AuditIntercepter : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private List<AuditEntry> _pendingEntry=new();

        public AuditIntercepter(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is null)
            {
                return base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            _pendingEntry = new List<AuditEntry>();

            var entries = eventData.Context.ChangeTracker
                .Entries()
                .Where(x => x.Entity is not AuditLog && (x.State == EntityState.Added || x.State == EntityState.Modified || x.State == EntityState.Deleted)).ToList();

            foreach(var entityEntry in entries)
            {
                var auditEntry = new AuditEntry
                {
                    Entry = entityEntry,
                    Action = (entityEntry.State).ToString(),
                    EntityName = entityEntry.Entity.GetType().Name,
                };

                foreach (var prop in entityEntry.Properties)
                {
                    switch (entityEntry.State)
                    {
                        case EntityState.Added:
                            auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                            break;

                        case EntityState.Modified:
                            if (prop.IsModified && !Equals(prop.OriginalValue , prop.CurrentValue))
                            {
                                auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                                auditEntry.OldValues[prop.Metadata.Name] = prop.OriginalValue;
                            }
                            break;

                        case EntityState.Deleted:
                            auditEntry.OldValues[prop.Metadata.Name] = prop.OriginalValue;
                            break;
                    }
                }
                _pendingEntry.Add(auditEntry);
            }
                

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {

            if (eventData.Context is null || !_pendingEntry.Any())//checks if the list contains at least 1 item
            {
                return await base.SavedChangesAsync(eventData, result, cancellationToken);
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var userIdClaim = httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? userId = Guid.TryParse(userIdClaim, out var parsed) ? parsed : null;
            var userEmail = httpContext?.User.FindFirstValue(ClaimTypes.Email);
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();

            foreach(var item in _pendingEntry)
            {
                var entityId = item.Entry
                    .Properties
                    .FirstOrDefault(p => p.Metadata.IsPrimaryKey())
                    ?.CurrentValue?
                    .ToString();

                var auditLog = new AuditLog
                {
                    UserId = userId,
                    UserEmail = userEmail,
                    IpAddress = ipAddress,
                    Action = item.Action,
                    EntityName = item.EntityName,
                    EntityId = entityId,
                    NewValues = item.NewValues.Any() ? JsonSerializer.Serialize(item.NewValues) : null,
                    OldValues = item.OldValues.Any() ? JsonSerializer.Serialize(item.OldValues) : null,
                    Timestamp = DateTime.UtcNow,
                    Succeeded = true,
                    ErrorMessage = null
                };

                eventData.Context.Set<AuditLog>().Add(auditLog);
            }

            await eventData.Context.SaveChangesAsync();

            _pendingEntry.Clear();

            return result;
        }

        public override async Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if(eventData.Context is null)
            {
                return;
            }

            var httpContext = _httpContextAccessor.HttpContext;
            var userEmail = httpContext?.User.FindFirstValue(ClaimTypes.Email);
            var ipAddress = httpContext?.Connection.RemoteIpAddress?.ToString();
            var userIdCliam= httpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid? userId = Guid.TryParse(userIdCliam, out var parse) ? parse : null;

            foreach(var item in _pendingEntry)
            {
                var audetLog = new AuditLog
                {
                    UserId = userId,
                    UserEmail = userEmail,
                    IpAddress = ipAddress,
                    EntityName = item.EntityName,
                    Action = item.Action,
                    NewValues = item.NewValues.Any() ? JsonSerializer.Serialize(item.NewValues) : null,
                    OldValues = item.OldValues.Any() ? JsonSerializer.Serialize(item.OldValues) : null,
                    Timestamp = DateTime.UtcNow,
                    Succeeded = false,
                    ErrorMessage = eventData.Exception.Message
                };

                eventData.Context.Set<AuditLog>().Add(audetLog);
            }

            await eventData.Context.SaveChangesAsync();

            _pendingEntry.Clear();
        }
    }
}
