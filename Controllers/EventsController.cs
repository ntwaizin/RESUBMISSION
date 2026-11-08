using System.Linq.Expressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG_P2.Data;
using PROG_P2.DTOs;
using PROG_P2.Helpers;
using PROG_P2.Models;

namespace PROG_P2.Controllers
{
    /// <summary>Events, plus the categories and enrolments that belong to an event.</summary>
    [Route("api/events")]
    public class EventsController : ApiControllerBase
    {
        private static readonly string[] AllowedTypes = { "Run", "Walk", "Cycle" };

        private readonly RaceDayContext _context;

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        // Shared projection: entity -> response DTO (avoids JSON cycles and over-posting)
        private static readonly Expression<Func<Event, EventResponseDTO>> ToResponse = e => new EventResponseDTO
        {
            EventID = e.EventID,
            OrganiserID = e.OrganiserID,
            OrganiserName = e.Organiser.FirstName + " " + e.Organiser.LastName,
            EventName = e.EventName,
            EventDate = e.EventDate,
            Location = e.Location,
            Description = e.Description,
            DistanceKM = e.DistanceKM,
            EventType = e.EventType,
            BannerUrl = e.BannerUrl,
            Categories = e.Categories.Select(c => new CategoryResponseDTO
            {
                CategoryID = c.CategoryID,
                EventID = c.EventID,
                CategoryName = c.CategoryName,
                DistanceKM = c.DistanceKM,
                EntryFee = c.EntryFee
            }).ToList()
        };

        /// <summary>View all available events (public).</summary>
        /// <param name="eventType">Optional filter: Run, Walk or Cycle.</param>
        /// <response code="200">List of events ordered by date.</response>
        [HttpGet]
        [ProducesResponseType(typeof(List<EventResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvents([FromQuery] string? eventType = null)
        {
            var query = _context.Events.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            var events = await query.OrderBy(e => e.EventDate).Select(ToResponse).ToListAsync();
            return Ok(events);
        }

        /// <summary>View the details of one event, including its categories (public).</summary>
        /// <response code="200">The event.</response>
        /// <response code="404">Event does not exist.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(EventResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEvent(int id)
        {
            var dto = await _context.Events.AsNoTracking()
                .Where(e => e.EventID == id).Select(ToResponse).FirstOrDefaultAsync();

            if (dto == null) return NotFound(new { message = "Event not found." });
            return Ok(dto);
        }

        /// <summary>Create a new event (Organiser only).</summary>
        /// <remarks>EventType must be Run, Walk or Cycle. The organiser is taken from the session.</remarks>
        /// <response code="201">Event created.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser.</response>
        [HttpPost]
        [SessionAuthorize(RoleNames.Organiser)]
        [ProducesResponseType(typeof(EventResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateEvent([FromBody] EventDTO request)
        {
            var error = ValidateEvent(request);
            if (error != null) return BadRequest(new { message = error });

            var eventItem = new Event
            {
                OrganiserID = CurrentUserID,
                EventName = request.EventName.Trim(),
                EventDate = request.EventDate,
                Location = request.Location.Trim(),
                Description = request.Description,
                DistanceKM = request.DistanceKM,
                EventType = request.EventType
            };

            _context.Events.Add(eventItem);
            await _context.SaveChangesAsync();

            var dto = await _context.Events.AsNoTracking()
                .Where(e => e.EventID == eventItem.EventID).Select(ToResponse).FirstAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = eventItem.EventID }, dto);
        }

        /// <summary>Update an existing event (Organiser who owns it).</summary>
        /// <response code="200">Event updated.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of this event.</response>
        /// <response code="404">Event does not exist.</response>
        [HttpPut("{id:int}")]
        [SessionAuthorize(RoleNames.Organiser)]
        [ProducesResponseType(typeof(EventResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventDTO request)
        {
            var existing = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
            if (existing == null) return NotFound(new { message = "Event not found." });
            if (existing.OrganiserID != CurrentUserID) return Forbidden("You can only change your own events.");

            var error = ValidateEvent(request);
            if (error != null) return BadRequest(new { message = error });

            existing.EventName = request.EventName.Trim();
            existing.EventDate = request.EventDate;
            existing.Location = request.Location.Trim();
            existing.Description = request.Description;
            existing.DistanceKM = request.DistanceKM;
            existing.EventType = request.EventType;
            await _context.SaveChangesAsync();

            var dto = await _context.Events.AsNoTracking()
                .Where(e => e.EventID == id).Select(ToResponse).FirstAsync();
            return Ok(dto);
        }

        /// <summary>Delete an event (Organiser who owns it).</summary>
        /// <remarks>An event that already has enrolments cannot be deleted.</remarks>
        /// <response code="200">Event deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of this event.</response>
        /// <response code="404">Event does not exist.</response>
        /// <response code="409">Event has enrolments.</response>
        [HttpDelete("{id:int}")]
        [SessionAuthorize(RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var existing = await _context.Events.FirstOrDefaultAsync(e => e.EventID == id);
            if (existing == null) return NotFound(new { message = "Event not found." });
            if (existing.OrganiserID != CurrentUserID) return Forbidden("You can only delete your own events.");

            var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.Category.EventID == id);
            if (hasEnrolments)
            {
                return Conflict(new { message = "This event already has enrolments and cannot be deleted." });
            }

            var categories = _context.Categories.Where(c => c.EventID == id);
            _context.Categories.RemoveRange(categories);
            _context.Events.Remove(existing);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event deleted successfully." });
        }

        /// <summary>View all categories for an event (public).</summary>
        /// <response code="200">List of categories.</response>
        /// <response code="404">Event does not exist.</response>
        [HttpGet("{eventId:int}/categories")]
        [ProducesResponseType(typeof(List<CategoryResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategories(int eventId)
        {
            if (!await _context.Events.AnyAsync(e => e.EventID == eventId))
            {
                return NotFound(new { message = "Event not found." });
            }

            var categories = await _context.Categories.AsNoTracking()
                .Where(c => c.EventID == eventId)
                .Select(c => new CategoryResponseDTO
                {
                    CategoryID = c.CategoryID,
                    EventID = c.EventID,
                    CategoryName = c.CategoryName,
                    DistanceKM = c.DistanceKM,
                    EntryFee = c.EntryFee
                }).ToListAsync();

            return Ok(categories);
        }

        /// <summary>Add a category to an event (Organiser who owns the event).</summary>
        /// <response code="201">Category created.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of this event.</response>
        /// <response code="404">Event does not exist.</response>
        [HttpPost("{eventId:int}/categories")]
        [SessionAuthorize(RoleNames.Organiser)]
        [ProducesResponseType(typeof(CategoryResponseDTO), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateCategory(int eventId, [FromBody] CategoryDTO request)
        {
            var eventItem = await _context.Events.FirstOrDefaultAsync(e => e.EventID == eventId);
            if (eventItem == null) return NotFound(new { message = "Event not found." });
            if (eventItem.OrganiserID != CurrentUserID) return Forbidden("You can only add categories to your own events.");

            var category = new Category
            {
                EventID = eventId,
                CategoryName = request.CategoryName.Trim(),
                DistanceKM = request.DistanceKM,
                EntryFee = request.EntryFee
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(CategoriesController.GetCategory),
                "Categories",
                new { id = category.CategoryID },
                CategoriesController.ToResponse(category));
        }

        /// <summary>View everyone enrolled in one of your events (Organiser who owns the event).</summary>
        /// <response code="200">List of enrolments for the event.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of this event.</response>
        /// <response code="404">Event does not exist.</response>
        [HttpGet("{eventId:int}/enrolments")]
        [SessionAuthorize(RoleNames.Organiser)]
        [ProducesResponseType(typeof(List<EnrolmentResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEventEnrolments(int eventId)
        {
            var eventItem = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventID == eventId);
            if (eventItem == null) return NotFound(new { message = "Event not found." });
            if (eventItem.OrganiserID != CurrentUserID) return Forbidden("You can only view enrolments for your own events.");

            var enrolments = await _context.Enrolments.AsNoTracking()
                .Where(e => e.Category.EventID == eventId)
                .OrderBy(e => e.EnrolmentDate)
                .Select(EnrolmentsController.ToResponse)
                .ToListAsync();

            return Ok(enrolments);
        }

        private static string? ValidateEvent(EventDTO request)
        {
            if (!AllowedTypes.Contains(request.EventType))
            {
                return "Event type must be Run, Walk or Cycle.";
            }

            if (request.EventDate == default)
            {
                return "Event date is required.";
            }

            return null;
        }
    }
}
