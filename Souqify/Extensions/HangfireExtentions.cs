using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using Souqify.Application.Options;
using Souqify.Domain.Entities.Enums;
using Souqify.Infrastructure.BackgroundJobs.OrderJobs;

namespace Souqify.Extensions
{
    public static class HangfireExtentions
    {
        //false turns off ALL of Hangfire (storage, server, dashboard, jobs). The integration tests set it,
        //otherwise they would connect to the dev database. One constant, so both methods read the same key
        private const string EnabledKey = "Hangfire:Enabled";

        public static IServiceCollection AddBackgroundJobs(this  IServiceCollection services,IConfiguration configuration,string connectionString)
        {
            if (!configuration.GetValue(EnabledKey, true))
                return services;

            services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString),
            new PostgreSqlStorageOptions
            {
                SchemaName = "hangfire",
                PrepareSchemaIfNecessary = true,
                QueuePollInterval = TimeSpan.FromSeconds(20)
            }));

            if (configuration.GetValue("Hangfire:ServerEnabled", true))
            {
                services.AddHangfireServer(options =>
                {
                    options.ServerName = $"Souqify-{Environment.MachineName}";
                    options.WorkerCount = 5;
                });
            }

            return services;
        }

        public static WebApplication UseBackgroundJobs(this WebApplication webApplication)
        {
            //AddBackgroundJobs registered nothing, so there is no storage for the dashboard or the jobs
            if (!webApplication.Configuration.GetValue(EnabledKey, true))
                return webApplication;

            webApplication.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                DashboardTitle = "Order Dashboard"
            });

            var cleanupOptions = webApplication.Services.GetRequiredService<IOptions<StaleOrderCancellationOptions>>().Value;

            var recurringJob=webApplication.Services.GetRequiredService<IRecurringJobManager>();

            if (cleanupOptions.Enabled)
            {
                recurringJob.AddOrUpdate<IStaleOrderCleanupJob>(
                "cancel-stale-card-order",
                job => job.RunAsync(PaymentMethod.Card, CancellationToken.None),
                cleanupOptions.Schedule);
            }
            else
            {
                recurringJob.RemoveIfExists("cancel-stale-card-order");
            }

            //recurringJob.AddOrUpdate<IStaleOrderCleanupJob>(
            //    "cancel-stale-COD-order",
            //    job => job.RunAsync(PaymentMethod.CashOnDelivery, CancellationToken.None),
            //    cleanupOptions.Schedule);

            return webApplication;
        }
    }
}
