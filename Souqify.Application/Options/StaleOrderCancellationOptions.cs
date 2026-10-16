
namespace Souqify.Application.Options
{
    public class StaleOrderCancellationOptions
    {
        public const string suctionName = "StaleOrderCancellation";

        public bool Enabled { get; set; }

        public int BatchSize { get; set; }
        public string Schedule { get; set; } = null!;

        public int MaxBatchNumberPerRun { get; set; }

        public int CashOnDeliveryTime { get; set; }

        public int CardTime { get; set; }
    }
}
