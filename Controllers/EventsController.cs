using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RoleRequired]
    public class EventsController : Controller
    {
        private readonly RaceDayApiService _api;

        public EventsController(RaceDayApiService api) => _api = api;

        public async Task<IActionResult> Index(string? search, string? type)
        {
            var r = await _api.GetAsync<List<EventDto>>(ApiRoutes.Events);
            if (!r.Ok) ViewBag.Error = r.Error;

            var events = (r.Data ?? new()).Where(e => e.EventDate.Date >= DateTime.Today);
            if (!string.IsNullOrWhiteSpace(search))
                events = events.Where(e =>
                    e.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                    e.Location.Contains(search, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(type))
                events = events.Where(e => string.Equals(e.EventType, type, StringComparison.OrdinalIgnoreCase));

            ViewBag.Search = search;
            ViewBag.Type = type;
            return View(events.OrderBy(e => e.EventDate).ToList());
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await _api.GetAsync<EventDto>(ApiRoutes.Event(id));
            if (!ev.Ok || ev.Data == null)
            {
                TempData["Error"] = ev.Error ?? "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            var cats = await _api.GetAsync<List<CategoryDto>>(ApiRoutes.EventCategories(id));
            var model = new EventDetailsModel { Event = ev.Data, Categories = cats.Data ?? new() };

            if (HttpContext.Session.GetString("UserRole") == "Participant")
            {
                var mine = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.MyEnrolments);
                model.AlreadyEnrolled = (mine.Data ?? new()).Any(e => e.EventID == id);
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken, RoleRequired("Participant")]
        public async Task<IActionResult> Enrol(int eventId, int categoryId)
        {
            if (categoryId <= 0)
            {
                TempData["Error"] = "Please select a category before entering.";
                return RedirectToAction(nameof(Details), new { id = eventId });
            }

            var r = await _api.PostAsync(ApiRoutes.Enrolments, new { eventID = eventId, categoryID = categoryId });

            if (r.Ok)
            {
                TempData["Success"] = "You are entered! Check My Events for your enrolment status.";
                return RedirectToAction("MyEvents", "Participant");
            }

            TempData["Error"] = r.Error;
            return RedirectToAction(nameof(Details), new { id = eventId });
        }
    }
}
