

namespace Souqify.Application.Exceptions
{
    public class InsufficientStockException : Exception
    {
        public StockShortFalls StockShortFalls { get; }

        public InsufficientStockException(StockShortFalls shortFalls,string msg) : base(msg)
        {
            StockShortFalls = shortFalls;
        }
    }


    public record StockShortFalls(Guid VariantId, string ProductName, int Requested, int Available);
}
