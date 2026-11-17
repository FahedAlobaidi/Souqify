
namespace Souqify.Application.Exceptions
{
    // The page sent an old RowVersion: someone saved this item after the page loaded it. The middleware turns it into 409.
    public class ConcurrencyConflictException:Exception
    {
        public ConcurrencyConflictException(string message) : base(message) { }
    }
}
