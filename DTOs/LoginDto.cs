using System.ComponentModel.DataAnnotations;

namespace PROG_P2.DTOs
{
    /// <summary>Credentials used to log in.</summary>
    public class LoginDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public string Password { get; set; } = "";
    }
}
