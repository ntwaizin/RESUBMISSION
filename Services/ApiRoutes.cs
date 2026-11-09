namespace RaceDay.Web.Services
{
    /// <summary>Every API route used by the MVC app, matching the Part 1 endpoint plan.</summary>
    public static class ApiRoutes
    {
        public const string Login = "api/auth/login";
        public const string Register = "api/auth/register";
        public const string Logout = "api/auth/logout";
        public const string Profile = "api/profile";
        public const string ProfilePicture = "api/profile/picture";
        public const string Events = "api/events";
        public const string Enrolments = "api/enrolments";
        public const string MyEnrolments = "api/enrolments/my";
        public const string MyResults = "api/results/my";

        public static string Event(int id) => $"api/events/{id}";
        public static string EventBanner(int id) => $"api/events/{id}/banner";
        public static string EventCategories(int id) => $"api/events/{id}/categories";
        public static string EventEnrolments(int id) => $"api/events/{id}/enrolments";
        public static string EventResults(int id) => $"api/events/{id}/results";
        public static string Category(int id) => $"api/categories/{id}";
    }
}
