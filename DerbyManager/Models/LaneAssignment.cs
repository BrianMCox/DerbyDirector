namespace DerbyManager.Models
{
    public class LaneAssignment
    {
        public int Lane { get; set; }
        public int CarNumber { get; set; }
        public string Driver { get; set; }
        public decimal Time { get; set; }
        public decimal ScaleSpeed { get; set; }
        public int Place { get; set; }
    }
}