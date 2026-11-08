namespace PROG_P2.Models
{
    /// <summary>A race participant (maps to the Participants table in the ERD).</summary>
    public class Participant
    {
        public int ParticipantID { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
    }
}
