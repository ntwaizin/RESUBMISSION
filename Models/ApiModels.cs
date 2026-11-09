using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    // ---- DTOs returned by the Part 2 API (case-insensitive JSON binding) ----
    public class EventDto
    {
        public int EventID { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = "";
        public decimal Distance { get; set; }
        public string EventType { get; set; } = "";
        public string? BannerUrl { get; set; }
        public int OrganiserID { get; set; }
    }

    public class CategoryDto
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
    }

    public class EnrolmentDto
    {
        public int EnrolmentID { get; set; }
        public int EventID { get; set; }
        public string? EventName { get; set; }
        public DateTime? EventDate { get; set; }
        public int CategoryID { get; set; }
        public string? CategoryName { get; set; }
        public int ParticipantID { get; set; }
        public string? ParticipantName { get; set; }
        public string? ParticipantEmail { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime? EnrolmentDate { get; set; }
    }

    public class ResultDto
    {
        public int ResultID { get; set; }
        public int EventID { get; set; }
        public string? EventName { get; set; }
        public DateTime? EventDate { get; set; }
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public string? ParticipantName { get; set; }
        public string? CategoryName { get; set; }
        public string? FinishTime { get; set; }
        public int Position { get; set; }
        public int TotalFinishers { get; set; }
    }

    // ---- Form / view models ----
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required."), EmailAddress]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = "";
    }

    public class RegisterViewModel
    {
        [Required, StringLength(50)] public string FirstName { get; set; } = "";
        [Required, StringLength(50)] public string LastName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required, MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string Password { get; set; } = "";
        [Required, Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = "";
        [Required(ErrorMessage = "Please choose a role.")]
        public string Role { get; set; } = "Participant";
    }

    public class EventFormModel
    {
        public int EventID { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = "";
        [Required, StringLength(1000)] public string Description { get; set; } = "";
        [Required] public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);
        [Required, StringLength(150)] public string Location { get; set; } = "";
        [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
        public decimal Distance { get; set; }
        [Required] public string EventType { get; set; } = "Run";
        public string? BannerUrl { get; set; }
        public IFormFile? Banner { get; set; }
    }

    public class ProfileModel
    {
        [Required, StringLength(50)] public string FirstName { get; set; } = "";
        [Required, StringLength(50)] public string LastName { get; set; } = "";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Phone] public string? PhoneNumber { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? Role { get; set; }
    }

    public class EventSummary
    {
        public EventDto Event { get; set; } = new();
        public int Enrolments { get; set; }
    }

    public class CategoriesPageModel
    {
        public EventDto Event { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
    }

    public class EnrolmentsPageModel
    {
        public EventDto Event { get; set; } = new();
        public List<EnrolmentDto> Enrolments { get; set; } = new();
    }

    public class EventDetailsModel
    {
        public EventDto Event { get; set; } = new();
        public List<CategoryDto> Categories { get; set; } = new();
        public bool AlreadyEnrolled { get; set; }
    }

    public class ResultRow
    {
        public int EnrolmentID { get; set; }
        public int ParticipantID { get; set; }
        public string? ParticipantName { get; set; }
        public string? CategoryName { get; set; }
        [RegularExpression(@"^\d{1,2}:[0-5]\d:[0-5]\d$", ErrorMessage = "Use hh:mm:ss")]
        public string? FinishTime { get; set; }
        [Range(1, 100000)] public int? Position { get; set; }
    }

    public class ResultsPageModel
    {
        public EventDto Event { get; set; } = new();
        public List<ResultRow> Rows { get; set; } = new();
    }
}
