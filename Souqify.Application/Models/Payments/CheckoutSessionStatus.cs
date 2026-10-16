

namespace Souqify.Application.Models.Payments
{
    public enum CheckoutSessionStatus
    {
        Paid,
        Payable,
        Unpayable,
        NotFound,
        Processing,
        Unknown
    }
}
