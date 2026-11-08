using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PROG_P2.Data;
using PROG_P2.DTOs;
using PROG_P2.Helpers;
using PROG_P2.Models;

namespace PROG_P2.Controllers
{
    /// <summary>
    /// View, update and delete categories. Categories are created through
    /// POST /api/events/{eventId}/categories.
    /// </summary>
    [Route("api/categories")]
    public class CategoriesController : ApiControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        internal static CategoryResponseDTO ToResponse(Category c) => new()
        {
            CategoryID = c.CategoryID,
            EventID = c.EventID,
            CategoryName = c.CategoryName,
            DistanceKM = c.DistanceKM,
            EntryFee = c.EntryFee
        };

        /// <summary>View one category (public).</summary>
        /// <response code="200">The category.</response>
        /// <response code="404">Category does not exist.</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCategory(int id)
        {
            var category = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category == null) return NotFound(new { message = "Category not found." });
            return Ok(ToResponse(category));
        }

        /// <summary>Update a race category (Organiser who owns the event).</summary>
        /// <response code="200">Category updated.</response>
        /// <response code="400">Invalid data.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of the event.</response>
        /// <response code="404">Category does not exist.</response>
        [HttpPut("{id:int}")]
        [SessionAuthorize(RoleNames.Organiser)]
        [ProducesResponseType(typeof(CategoryResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] CategoryDTO request)
        {
            var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category == null) return NotFound(new { message = "Category not found." });
            if (category.Event.OrganiserID != CurrentUserID) return Forbidden("You can only change categories of your own events.");

            category.CategoryName = request.CategoryName.Trim();
            category.DistanceKM = request.DistanceKM;
            category.EntryFee = request.EntryFee;
            await _context.SaveChangesAsync();

            return Ok(ToResponse(category));
        }

        /// <summary>Delete a race category (Organiser who owns the event).</summary>
        /// <remarks>A category that already has enrolments cannot be deleted.</remarks>
        /// <response code="200">Category deleted.</response>
        /// <response code="401">Not logged in.</response>
        /// <response code="403">Not an Organiser, or not the owner of the event.</response>
        /// <response code="404">Category does not exist.</response>
        /// <response code="409">Category has enrolments.</response>
        [HttpDelete("{id:int}")]
        [SessionAuthorize(RoleNames.Organiser)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryID == id);
            if (category == null) return NotFound(new { message = "Category not found." });
            if (category.Event.OrganiserID != CurrentUserID) return Forbidden("You can only delete categories of your own events.");

            if (await _context.Enrolments.AnyAsync(e => e.CategoryID == id))
            {
                return Conflict(new { message = "This category already has enrolments and cannot be deleted." });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted successfully." });
        }
    }
}
