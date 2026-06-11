using Microsoft.EntityFrameworkCore;
using Souqify.Infrastructure;
using Souqify.Infrastructure.Auditing;

namespace Souqify.Extensions
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services,string connectionString)
        {
            services.AddDbContext<SouqifyDbContext>((serviceProvidor,options) =>
            {
                options.UseNpgsql(connectionString);
                options.AddInterceptors(serviceProvidor.GetRequiredService<AuditIntercepter>());
            });

            return services;
        }
    }
}
