using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG_P2.Data;
using PROG_P2.DTOs;
using PROG_P2.Helpers;
using PROG_P2.Models;

namespace PROG_P2.Controllers
{
    /// <summary>Participants enter events by enrolling in a category.</summary>
    [Route("api/enrolments")]
    public class EnrolmentsController : ApiControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        // Shared projection: entity -> response DTO
        internal static readonly Expression<Func<Enrolment, EnrolmentResponseDTO>> ToResponse = e => new EnrolmentResponseDTO
        {
            EnrolmentID = e.EnrolmentID,
            ParticipantID = e.ParticipantID,
            ParticipantName = e.Participant.FirstName + " " + e.Participant.LastName,
            EventID = e.Category.EventID,
            EventName = e.Category.Event.EventName,
            EventDate = e.Category.Event.EventDate,
            CategoryID = e.CategoryID,
            CategoryName = e.Category.CategoryName,
            DistanceKM = e.Category.DistanceKM,
            EntryFee = e.Category.EntryFee,
            EnrolmentDate = e.EnrolmentDate,
            PaymentStatus = e.PaymentStatus
        };

        /// <summary>View the logged-in participant's own enrolments.</summary>
        /// <response code="200">List of the participant's enrolments.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not a Participant.</response>
        [HttpGet]
        [SessionAuthorize(RoleNames.Participant)]
        [ProducesResponseType(typeof(List<EnrolmentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyEnrolments()
        {
            var participantId = CurrentUserID;

            var enrolments = await _context.Enrolments.AsNoTracking()
                .Where(e => e.ParticipantID == participantId)
                .OrderBy(e => e.Category.Event.EventDate)
                .Select(ToResponse)
                .ToListAsync();

            return Ok(enrolments);
        }

        /// <summary>View one enrolment (the participant who owns it, or the organiser of the event).</summary>
        /// <response code="200">The enrolment.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">The enrolment belongs to someone else's event/account.</response>
        /// <response code="404">Enrolment does not exist.</response>
        [HttpGet("{id:int}")]
        [SessionAuthorize]
        [ProducesResponseType(typeof(EnrolmentResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEnrolment(int id)
        {
            var enrolment = await _context.Enrolments.AsNoTracking()
                .Include(e => e.Category).ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == id);

            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

            var allowed = AuthHelper.IsParticipant(HttpContext)
                ? enrolment.ParticipantID == CurrentUserID
                : enrolment.Category.Event.OrganiserID == CurrentUserID;

            if (!allowed) return Forbidden("You cannot view this enrolment.");

            var dto = await _context.Enrolments.AsNoTracking()
                .Where(e => e.EnrolmentID == id).Select(ToResponse).FirstAsync();
            return Ok(dto);
        }

        /// <summary>Enrol the logged-in participant in an event category.</summary>
        /// <remarks>
        /// Records the link between the participant, the event and the chosen category.
        /// Payment status starts as "Pending". A participant can enrol in an event only once.
        /// </remarks>
        /// <response code="201">Enrolment created.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not a Participant.</response>
        /// <response code="404">Category does not exist.</response>
        /// <response code="409">Already enrolled in this event.</response>
        [HttpPost]
        [SessionAuthorize(RoleNames.Participant)]
        [ProducesResponseType(typeof(EnrolmentResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateEnrolment([FromBody] EnrolmentCreateDTO request)
        {
            var participantId = CurrentUserID;

            var category = await _context.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.CategoryID == request.CategoryID);
            if (category == null) return NotFound(new { message = "Category does not exist." });

            var alreadyEnrolled = await _context.Enrolments.AnyAsync(e =>
                e.ParticipantID == participantId && e.Category.EventID == category.EventID);
            if (alreadyEnrolled)
            {
                return Conflict(new { message = "You are already enrolled in this event." });
            }

            var enrolment = new Enrolment
            {
                ParticipantID = participantId,
                CategoryID = category.CategoryID,
                EnrolmentDate = DateTime.UtcNow,
                PaymentStatus = "Pending"
            };

            _context.Enrolments.Add(enrolment);
            await _context.SaveChangesAsync();

            var dto = await _context.Enrolments.AsNoTracking()
                .Where(e => e.EnrolmentID == enrolment.EnrolmentID).Select(ToResponse).FirstAsync();

            return CreatedAtAction(nameof(GetEnrolment), new { id = enrolment.EnrolmentID }, dto);
        }

        /// <summary>Cancel one of the logged-in participant's own enrolments.</summary>
        /// <response code="200">Enrolment cancelled.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not a Participant.</response>
        /// <response code="404">Enrolment does not exist (or belongs to someone else).</response>
        /// <response code="409">A result has already been recorded for this enrolment.</response>
        [HttpDelete("{id:int}")]
        [SessionAuthorize(RoleNames.Participant)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CancelEnrolment(int id)
        {
            var participantId = CurrentUserID;

            var enrolment = await _context.Enrolments
                .FirstOrDefaultAsync(e => e.EnrolmentID == id && e.ParticipantID == participantId);
            if (enrolment == null) return NotFound(new { message = "Enrolment not found." });

            if (await _context.Results.AnyAsync(r => r.EnrolmentID == id))
            {
                return Conflict(new { message = "A result has already been recorded, so this enrolment cannot be cancelled." });
            }

            _context.Enrolments.Remove(enrolment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Enrolment cancelled successfully." });
        }
    }
}
