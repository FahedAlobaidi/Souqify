using Souqify.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Souqify.Application.Models.BackgroundJob
{
    public class StaleOrdersCancelationResult
    {
        public List<Order> Orders { get; set; }=new List<Order>();

        public int NumberOfOrdersCancelled { get; set; }

        public int NumberOfBatches { get; set; }

        public bool ReachedLimit { get; set; }
    }
}
