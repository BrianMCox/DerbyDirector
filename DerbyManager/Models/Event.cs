using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DerbyManager.Models
{
    public class Event
    {
        public string Name { get; set; }
        public int Lanes { get; set; }
        public int TrackLength { get; set; }
        public IEnumerable<Group> Groups { get; set; }
    }
}