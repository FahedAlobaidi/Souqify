using Hangfire;
using Souqify.Domain.Entities.Enums;

namespace Souqify.Infrastructure.BackgroundJobs.OrderJobs
{
    public interface IStaleOrderCleanupJob
    {
        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(timeoutInSeconds: 30)]
        public Task RunAsync(PaymentMethod paymentMethod, CancellationToken cn);
    }
}
