using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG_P2.Data;
using PROG_P2.DTOs;
using PROG_P2.Helpers;

namespace PROG_P2.Controllers
{
    /// <summary>View and update the logged-in user's own profile (both roles).</summary>
    [Route("api/profile")]
    public class ProfileController : ApiControllerBase
    {
        private readonly RaceDayContext _context;

        public ProfileController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>View the logged-in user's profile.</summary>
        /// <response code="200">The profile.</response>
        /// <response code="401">Not logged in.</response>
        [HttpGet]
        [SessionAuthorize]
        [ProducesResponseType(typeof(ProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProfile()
        {
            var role = AuthHelper.GetRole(HttpContext);
            var userId = CurrentUserID;

            if (role == RoleNames.Organiser)
            {
                var o = await _context.Organisers.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.OrganiserID == userId);
                if (o == null) return NotFound(new { message = "Organiser profile not found." });

                return Ok(new ProfileResponseDTO
                {
                    UserID = o.OrganiserID, Role = role, FirstName = o.FirstName,
                    LastName = o.LastName, Email = o.Email, Phone = o.Phone
                });
            }

            var p = await _context.Participants.AsNoTracking()
                .FirstOrDefaultAsync(x => x.ParticipantID == userId);
            if (p == null) return NotFound(new { message = "Participant profile not found." });

            return Ok(new ProfileResponseDTO
            {
                UserID = p.ParticipantID, Role = role!, FirstName = p.FirstName,
                LastName = p.LastName, Email = p.Email, Phone = p.Phone, DateOfBirth = p.DateOfBirth
            });
        }

        /// <summary>Update the logged-in user's profile.</summary>
        /// <remarks>Changing the email also changes the login email.</remarks>
        /// <response code="200">Profile updated.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="409">Email belongs to another account.</response>
        [HttpPut]
        [SessionAuthorize]
        [ProducesResponseType(typeof(ProfileResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateDTO request)
        {
            var accountId = AuthHelper.GetAccountID(HttpContext) ?? 0;
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.AccountID == accountId);
            if (account == null) return Unauthorized(new { message = "Please log in first." });

            var email = request.Email.Trim().ToLowerInvariant();
            if (await _context.Accounts.AnyAsync(a => a.Email == email && a.AccountID != accountId))
            {
                return Conflict(new { message = "Email already exists." });
            }

            account.Email = email;

            if (account.OrganiserID.HasValue)
            {
                var o = await _context.Organisers.FirstAsync(x => x.OrganiserID == account.OrganiserID.Value);
                o.FirstName = request.FirstName.Trim();
                o.LastName = request.LastName.Trim();
                o.Email = email;
                o.Phone = request.Phone;
                await _context.SaveChangesAsync();

                return Ok(new ProfileResponseDTO
                {
                    UserID = o.OrganiserID, Role = account.Role, FirstName = o.FirstName,
                    LastName = o.LastName, Email = o.Email, Phone = o.Phone
                });
            }

            var p = await _context.Participants.FirstAsync(x => x.ParticipantID == account.ParticipantID!.Value);
            p.FirstName = request.FirstName.Trim();
            p.LastName = request.LastName.Trim();
            p.Email = email;
            p.Phone = request.Phone;
            p.DateOfBirth = request.DateOfBirth;
            await _context.SaveChangesAsync();

            return Ok(new ProfileResponseDTO
            {
                UserID = p.ParticipantID, Role = account.Role, FirstName = p.FirstName,
                LastName = p.LastName, Email = p.Email, Phone = p.Phone, DateOfBirth = p.DateOfBirth
            });
        }
    }
}
