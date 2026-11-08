namespace PROG_P2.Helpers
{
    /// <summary>The two roles supported by RaceDay.</summary>
    public static class RoleNames
    {
        public const string Organiser = "Organiser";
        public const string Participant = "Participant";
    }

    /// <summary>
    /// Reads the logged-in user's details from the server-side session
    /// (written by AuthController.Login).
    /// </summary>
    public static class AuthHelper
    {
        public const string AccountKey = "AccountID";
        public const string UserKey = "UserID";
        public const string RoleKey = "Role";

        public static bool IsLoggedIn(HttpContext context)
        {
            return context.Session.GetInt32(AccountKey).HasValue
                   && context.Session.GetString(RoleKey) != null;
        }

        public static int? GetAccountID(HttpContext context) => context.Session.GetInt32(AccountKey);

        /// <summary>The OrganiserID or ParticipantID, depending on the role.</summary>
        public static int? GetUserID(HttpContext context) => context.Session.GetInt32(UserKey);

        public static string? GetRole(HttpContext context) => context.Session.GetString(RoleKey);

        public static bool IsOrganiser(HttpContext context) => GetRole(context) == RoleNames.Organiser;

        public static bool IsParticipant(HttpContext context) => GetRole(context) == RoleNames.Participant;
    }
}
