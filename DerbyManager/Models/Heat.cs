using System.Collections.Generic;

namespace DerbyManager.Models
{
    public class Heat
    {
        public int number { get; set; }
        public string groupName { get; set; }
        public IEnumerable<LaneAssignment> laneAssignments { get; set; }
    }
}