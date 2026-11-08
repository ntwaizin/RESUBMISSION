using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG_P2.Data;
using PROG_P2.DTOs;
using PROG_P2.Helpers;
using PROG_P2.Models;

namespace PROG_P2.Controllers
{
    /// <summary>Race results: organisers record them, everyone can view them.</summary>
    [Route("api/results")]
    public class ResultsController : ApiControllerBase
    {
        private static readonly string[] AllowedStatuses = { "Finished", "DNF", "DNS", "Pending" };

        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        private static readonly Expression<Func<Result, ResultResponseDTO>> ToResponse = r => new ResultResponseDTO
        {
            ResultID = r.ResultID,
            EnrolmentID = r.EnrolmentID,
            ParticipantID = r.Enrolment.ParticipantID,
            ParticipantName = r.Enrolment.Participant.FirstName + " " + r.Enrolment.Participant.LastName,
            EventID = r.Enrolment.Category.EventID,
            EventName = r.Enrolment.Category.Event.EventName,
            EventDate = r.Enrolment.Category.Event.EventDate,
            CategoryName = r.Enrolment.Category.CategoryName,
            FinishTime = r.FinishTime,
            Position = r.Position,
            ResultStatus = r.ResultStatus
        };

        /// <summary>View all published race results (public).</summary>
        /// <response code="200">List of results.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<ResultResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResults()
        {
            var results = await _context.Results.AsNoTracking()
                .OrderBy(r => r.Enrolment.Category.EventID).ThenBy(r => r.Position)
                .Select(ToResponse).ToListAsync();
            return Ok(results);
        }

        /// <summary>View the logged-in participant's own results.</summary>
        /// <response code="200">The participant's results.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not a Participant.</response>
        [HttpGet("mine")]
        [SessionAuthorize(RoleNames.Participant)]
        [ProducesResponseType(typeof(List<ResultResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyResults()
        {
            var participantId = CurrentUserID;
            var results = await _context.Results.AsNoTracking()
                .Where(r => r.Enrolment.ParticipantID == participantId)
                .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
                .Select(ToResponse).ToListAsync();
            return Ok(results);
        }

        /// <summary>View one specific race result (public).</summary>
        /// <response code="200">The result.</response>
        /// <response code="404">Result does not exist.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ResultResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetResult(int id)
        {
            var dto = await _context.Results.AsNoTracking()
                .Where(r => r.ResultID == id).Select(ToResponse).FirstOrDefaultAsync();
            if (dto == null) return NotFound(new { message = "Result not found." });
            return Ok(dto);
        }

        /// <summary>Record a participant's race result (Organiser who owns the event).</summary>
        /// <remarks>ResultStatus must be Finished, DNF, DNS or Pending. One result per enrolment.</remarks>
        /// <response code="201">Result recorded.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of the event.</response>
        /// <response code="404">Enrolment does not exist.</response>
        /// <response code="409">A result already exists for this enrolment.</response>
        [HttpPost]
        [SessionAuthorize(RoleNames.Organiser)]
        [ProducesResponseType(typeof(ResultResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateResult([FromBody] ResultCreateDTO request)
        {
            if (!AllowedStatuses.Contains(request.ResultStatus))
            {
                return BadRequest(new { message = "Result status must be Finished, DNF, DNS or Pending." });
            }

            var enrolment = await _context.Enrolments.AsNoTracking()
                .Include(e => e.Category).ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(e => e.EnrolmentID == request.EnrolmentID);
            if (enrolment == null) return NotFound(new { message = "Enrolment does not exist." });

            if (enrolment.Category.Event.OrganiserID != CurrentUserID)
            {
                return Forbidden("You can only record results for your own events.");
            }

            if (await _context.Results.AnyAsync(r => r.EnrolmentID == request.EnrolmentID))
            {
                return Conflict(new { message = "A result has already been recorded for this enrolment." });
            }

            var result = new Result
            {
                EnrolmentID = request.EnrolmentID,
                FinishTime = request.FinishTime,
                Position = request.Position,
                ResultStatus = request.ResultStatus
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var dto = await _context.Results.AsNoTracking()
                .Where(r => r.ResultID == result.ResultID).Select(ToResponse).FirstAsync();

            return CreatedAtAction(nameof(GetResult), new { id = result.ResultID }, dto);
        }

        /// <summary>Update a race result (Organiser who owns the event).</summary>
        /// <response code="200">Result updated.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of the event.</response>
        /// <response code="404">Result does not exist.</response>
        [HttpPut("{id:int}")]
        [SessionAuthorize(RoleNames.Organiser)]
        [ProducesResponseType(typeof(ResultResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateResult(int id, [FromBody] ResultUpdateDTO request)
        {
            if (!AllowedStatuses.Contains(request.ResultStatus))
            {
                return BadRequest(new { message = "Result status must be Finished, DNF, DNS or Pending." });
            }

            var result = await _context.Results
                .Include(r => r.Enrolment).ThenInclude(e => e.Category).ThenInclude(c => c.Event)
                .FirstOrDefaultAsync(r => r.ResultID == id);
            if (result == null) return NotFound(new { message = "Result not found." });

            if (result.Enrolment.Category.Event.OrganiserID != CurrentUserID)
            {
                return Forbidden("You can only update results for your own events.");
            }

            result.FinishTime = request.FinishTime;
            result.Position = request.Position;
            result.ResultStatus = request.ResultStatus;
            await _context.SaveChangesAsync();

            var dto = await _context.Results.AsNoTracking()
                .Where(r => r.ResultID == id).Select(ToResponse).FirstAsync();
            return Ok(dto);
        }
    }
}
