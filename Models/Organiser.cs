namespace PROG_P2.Models
{
    /// <summary>An event organiser (maps to the Organisers table in the ERD).</summary>
    public class Organiser
    {
        public int OrganiserID { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }

        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}
