using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Web.Filters
{
    /// <summary>Blocks unauthenticated users; optionally restricts to one role.</summary>
    public class RoleRequiredAttribute : ActionFilterAttribute
    {
        private readonly string? _role;

        public RoleRequiredAttribute(string? role = null) => _role = role;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            if (_role != null && role != _role)
            {
                context.Result = new RedirectToActionResult(
                    "Index", role == "Organiser" ? "Organiser" : "Participant", null);
            }
        }
    }
}
