using Microsoft.EntityFrameworkCore;
using Souqify.Infrastructure;

namespace Souqify.Extensions
{
    public static class DatabaseExtensions
    {
        public static IServiceCollection AddDatabase(this IServiceCollection services,string connectionString)
        {
            services.AddDbContext<SouqifyDbContext>(options => options.UseNpgsql(connectionString));

            return services;
        }
    }
}
