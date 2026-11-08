using System.ComponentModel.DataAnnotations;

namespace PROG_P2.DTOs
{
    /// <summary>Data needed to create or update an event.</summary>
    public class EventDTO
    {
        [Required, StringLength(100)]
        public string EventName { get; set; } = "";

        public DateTime EventDate { get; set; }

        [Required, StringLength(100)]
        public string Location { get; set; } = "";

        [StringLength(255)]
        public string? Description { get; set; }

        [Range(0.01, 999.99)]
        public decimal DistanceKM { get; set; }

        /// <summary>Run, Walk or Cycle.</summary>
        [Required]
        public string EventType { get; set; } = "Run";
    }

    public class EventResponseDTO
    {
        public int EventID { get; set; }
        public int OrganiserID { get; set; }
        public string OrganiserName { get; set; } = "";
        public string EventName { get; set; } = "";
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = "";
        public string? Description { get; set; }
        public decimal DistanceKM { get; set; }
        public string EventType { get; set; } = "";
        public string? BannerUrl { get; set; }
        public List<CategoryResponseDTO> Categories { get; set; } = new();
    }
}
