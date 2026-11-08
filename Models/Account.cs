namespace PROG_P2.Models
{
    /// <summary>
    /// Login account. Links to exactly one Organiser or Participant profile and
    /// stores the role plus the hashed password (never the plain password).
    /// </summary>
    public class Account
    {
        public int AccountID { get; set; }
        public string Email { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = "";

        public int? OrganiserID { get; set; }
        public int? ParticipantID { get; set; }

        public Organiser? Organiser { get; set; }
        public Participant? Participant { get; set; }
    }
}
