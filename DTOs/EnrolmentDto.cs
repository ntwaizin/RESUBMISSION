using System.ComponentModel.DataAnnotations;

namespace PROG_P2.DTOs
{
    /// <summary>A participant enters an event by choosing a category.</summary>
    public class EnrolmentCreateDTO
    {
        [Range(1, int.MaxValue)]
        public int CategoryID { get; set; }
    }

    public class EnrolmentResponseDTO
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public string ParticipantName { get; set; } = "";
        public int EventID { get; set; }
        public string EventName { get; set; } = "";
        public DateTime EventDate { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal DistanceKM { get; set; }
        public decimal EntryFee { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string PaymentStatus { get; set; } = "";
    }
}
