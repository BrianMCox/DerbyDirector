using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DerbyManager.Models
{
    public class IndividualResults
    {
        public string Name { get; set; }
        public List<decimal> Times { get; set; }
        public decimal TotalTime { get; set; }
        public decimal AverageTime { get; set; }
        public bool Visible { get; set; }
    }
}