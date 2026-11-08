using Microsoft.AspNetCore.Mvc;
using PROG_P2.Helpers;

namespace PROG_P2.Controllers
{
    /// <summary>Shared helpers for all API controllers.</summary>
    [ApiController]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>OrganiserID or ParticipantID of the logged-in user (from the session).</summary>
        protected int CurrentUserID => AuthHelper.GetUserID(HttpContext) ?? 0;

        /// <summary>Returns 403 with a message (Forbid() would throw without an auth scheme).</summary>
        protected IActionResult Forbidden(string message) =>
            StatusCode(StatusCodes.Status403Forbidden, new { message });
    }
}
