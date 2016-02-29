using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DerbyManager.Models
{
    public class Race
    {
        public string Name { get; set; }
        public IEnumerable<Heat> Heats { get; set; }
    }
}