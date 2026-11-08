namespace PROG_P2.Models
{
    /// <summary>A race event created by an organiser.</summary>
    public class Event
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string EventName { get; set; } = "";
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = "";
        public string? Description { get; set; }
        public decimal DistanceKM { get; set; }

        /// <summary>Run, Walk or Cycle.</summary>
        public string EventType { get; set; } = "Run";

        /// <summary>Banner image URL (used from Part 3 with Azure Blob Storage).</summary>
        public string? BannerUrl { get; set; }

        public Organiser Organiser { get; set; } = null!;
        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
