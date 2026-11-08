using System.ComponentModel.DataAnnotations;

namespace PROG_P2.DTOs
{
    /// <summary>Fields a user may change on their own profile.</summary>
    public class ProfileUpdateDTO
    {
        [Required, StringLength(50)]
        public string FirstName { get; set; } = "";

        [Required, StringLength(50)]
        public string LastName { get; set; } = "";

        [Required, EmailAddress, StringLength(100)]
        public string Email { get; set; } = "";

        [StringLength(20)]
        public string? Phone { get; set; }

        /// <summary>Only used for Participants.</summary>
        public DateTime? DateOfBirth { get; set; }
    }

    /// <summary>The logged-in user's profile.</summary>
    public class ProfileResponseDTO
    {
        public int UserID { get; set; }
        public string Role { get; set; } = "";
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
