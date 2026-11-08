using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG_P2.Data;
using PROG_P2.DTOs;
using PROG_P2.Helpers;
using PROG_P2.Models;

namespace PROG_P2.Controllers
{
    /// <summary>Registration, login and logout.</summary>
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly PasswordHasher<Account> _hasher = new();

        public AuthController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Register a new Organiser or Participant account.</summary>
        /// <remarks>
        /// The password is hashed with ASP.NET Core Identity's PasswordHasher before it is stored.
        /// Role must be "Organiser" or "Participant". Email must be unique.
        /// </remarks>
        /// <response code="201">Account created.</response>
        /// <response code="400">Invalid data or invalid role.</response>
        /// <response code="409">Email is already registered.</response>
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Register([FromBody] RegisterDTO request)
        {
            if (request.Role != RoleNames.Organiser && request.Role != RoleNames.Participant)
            {
                return BadRequest(new { message = "Role must be Organiser or Participant." });
            }

            var email = request.Email.Trim().ToLowerInvariant();

            if (await _context.Accounts.AnyAsync(a => a.Email == email))
            {
                return Conflict(new { message = "Email already exists." });
            }

            var account = new Account { Email = email, Role = request.Role };
            account.PasswordHash = _hasher.HashPassword(account, request.Password);

            if (request.Role == RoleNames.Organiser)
            {
                account.Organiser = new Organiser
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Email = email,
                    Phone = request.Phone
                };
            }
            else
            {
                account.Participant = new Participant
                {
                    FirstName = request.FirstName.Trim(),
                    LastName = request.LastName.Trim(),
                    Email = email,
                    Phone = request.Phone,
                    DateOfBirth = request.DateOfBirth
                };
            }

            // One SaveChanges = one transaction: profile and account are created together.
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return StatusCode(StatusCodes.Status201Created, new AuthResponseDTO
            {
                Message = "Registration successful.",
                Email = account.Email,
                Role = account.Role,
                UserID = account.OrganiserID ?? account.ParticipantID ?? 0
            });
        }

        /// <summary>Log in and start a server-side session.</summary>
        /// <remarks>
        /// On success the session stores the AccountID, the UserID (OrganiserID or ParticipantID)
        /// and the Role. The browser/Swagger keeps the session cookie automatically.
        /// </remarks>
        /// <response code="200">Login successful.</response>
        /// <response code="401">Wrong email or password.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginDTO request)
        {
            var email = request.Email.Trim().ToLowerInvariant();
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);

            if (account == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var userId = account.OrganiserID ?? account.ParticipantID ?? 0;

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32(AuthHelper.AccountKey, account.AccountID);
            HttpContext.Session.SetInt32(AuthHelper.UserKey, userId);
            HttpContext.Session.SetString(AuthHelper.RoleKey, account.Role);

            return Ok(new AuthResponseDTO
            {
                Message = "Login successful.",
                Email = account.Email,
                Role = account.Role,
                UserID = userId
            });
        }

        /// <summary>Log out and clear the session.</summary>
        /// <response code="200">Session cleared.</response>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Ok(new { message = "Logout successful." });
        }
    }
}
