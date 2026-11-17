
namespace Souqify.Application.Exceptions
{
    // The request is fine, but the data right now doesn't allow it (CAT-4 AC 6: forms in use). The middleware turns it into 409.
    public class ConflictException:Exception
    {
        public ConflictException(string message) : base(message) { }
    }
}
