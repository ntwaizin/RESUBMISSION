using System.ComponentModel.DataAnnotations;

namespace PROG_P2.DTOs
{
    /// <summary>Data needed to register a new account.</summary>
    public class RegisterDTO
    {
        [Required, StringLength(50)]
        public string FirstName { get; set; } = "";

        [Required, StringLength(50)]
        public string LastName { get; set; } = "";

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = "";

        [StringLength(20)]
        public string? Phone { get; set; }

        /// <summary>Optional, only stored for Participants.</summary>
        public DateTime? DateOfBirth { get; set; }

        [Required, MinLength(8), StringLength(100)]
        public string Password { get; set; } = "";

        /// <summary>Either "Organiser" or "Participant".</summary>
        [Required]
        public string Role { get; set; } = "";
    }

    /// <summary>Returned after a successful register / login.</summary>
    public class AuthResponseDTO
    {
        public string Message { get; set; } = "";
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public int UserID { get; set; }
    }
}
