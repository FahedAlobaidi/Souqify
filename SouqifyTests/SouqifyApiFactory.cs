using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Souqify;
using Souqify.Infrastructure;
using Testcontainers.PostgreSql;


namespace SouqifyTests
{
    public class SouqifyApiFactory : WebApplicationFactory<IApiMarker>,IAsyncLifetime
    {
        //A class from the Testcontainers.PostgreSql NuGet package.
        //Represents ONE PostgreSQL instance running in Docker.
        private readonly PostgreSqlContainer testcontainersDatabase = new PostgreSqlBuilder().WithDatabase("testdb").WithUsername("test").WithPassword("test").Build();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            });

            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<SouqifyDbContext>));

                if (descriptor != null)
                    services.Remove(descriptor);

                // Add DbContext with test container connection string
                services.AddDbContext<SouqifyDbContext>(options =>
                options.UseNpgsql(testcontainersDatabase.GetConnectionString()));

                var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<SouqifyDbContext>();
                db.Database.Migrate();
            });
        }


        public async Task InitializeAsync()
        {
            await testcontainersDatabase.StartAsync();
        }


        async Task IAsyncLifetime.DisposeAsync()
        {
            await testcontainersDatabase.StopAsync();
        }
    }
}
