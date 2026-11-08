

using Hangfire;
using Microsoft.Extensions.Logging;
using Souqify.Application.Interfaces;
using Souqify.Domain.Entities.Enums;

namespace Souqify.Infrastructure.BackgroundJobs.OrderJobs
{
    public class StaleOrderCleanupJob:IStaleOrderCleanupJob
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<StaleOrderCleanupJob> _logger;

        public StaleOrderCleanupJob(IOrderService orderService, ILogger<StaleOrderCleanupJob> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        
        public async Task RunAsync(PaymentMethod paymentMethod,CancellationToken cn)
        {
            var result=await _orderService.CleanupStaleOrders(paymentMethod,cn);

            if (result.ReachedLimit)
            {
                _logger.LogWarning("Stale {PaymentMethod} cleanup stopped at MaxBatchNumberPerRun;the rest waits for the next run",
                    paymentMethod) ;
            }
        }
    }
}
