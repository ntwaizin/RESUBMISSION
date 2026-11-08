using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PROG_P2.Helpers
{
    /// <summary>
    /// Protects an endpoint using the server-side session.
    /// No session  -> 401 Unauthorized.
    /// Wrong role  -> 403 Forbidden.
    /// Pass no roles to allow any logged-in user.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class SessionAuthorizeAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public SessionAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;

            if (!AuthHelper.IsLoggedIn(http))
            {
                context.Result = new UnauthorizedObjectResult(new { message = "Please log in first." });
                return;
            }

            if (_roles.Length > 0 && !_roles.Contains(AuthHelper.GetRole(http)))
            {
                context.Result = new ObjectResult(new
                {
                    message = $"This action requires the role: {string.Join(" or ", _roles)}."
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
