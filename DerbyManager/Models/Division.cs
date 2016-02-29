using System.Collections.Generic;

namespace DerbyManager.Models
{
    public class Division
    {
        public string Name { get; set; }
        public int Awards { get; set; }
        public IEnumerable<Racer> Entrants { get; set; }
    }
}