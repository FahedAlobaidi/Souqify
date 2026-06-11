

namespace Souqify.Application.Exceptions
{
    public class LockoutException:Exception
    {
        public LockoutException(string message) : base(message)
        {

        }
    }
}
