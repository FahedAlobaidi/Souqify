using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Exceptions
{
    public class DuplicateIdempotencyKeyException:Exception
    {
        public DuplicateIdempotencyKeyException(string message) : base(message) { }
    }
}
