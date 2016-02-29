using System.Collections.Generic;

namespace DerbyManager.Models
{
    public class Group
    {
        public string Name { get; set; }
        public int Awards { get; set; }
        public IEnumerable<Division> Divisions { get; set; }
        public IEnumerable<Racer> Entrants { get; set; }
    }
}