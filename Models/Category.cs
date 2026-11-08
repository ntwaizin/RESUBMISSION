namespace PROG_P2.Models
{
    /// <summary>A distance/age category belonging to one event (e.g. Fun Run 5km).</summary>
    public class Category
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal DistanceKM { get; set; }
        public decimal EntryFee { get; set; }

        public Event Event { get; set; } = null!;
        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
