namespace PROG_P2.Models
{
    /// <summary>Links a participant to the category they entered.</summary>
    public class Enrolment
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public int CategoryID { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string PaymentStatus { get; set; } = "Pending";

        public Participant Participant { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public Result? Result { get; set; }
    }
}
