using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RoleRequired("Organiser")]
    public class OrganiserController : Controller
    {
        private readonly RaceDayApiService _api;

        public OrganiserController(RaceDayApiService api) => _api = api;

        private int UserId => HttpContext.Session.GetInt32("UserID") ?? 0;

        private async Task<List<EventDto>> LoadMyEventsAsync()
        {
            var r = await _api.GetAsync<List<EventDto>>(ApiRoutes.Events);
            if (!r.Ok) ViewBag.Error = r.Error;
            return (r.Data ?? new())
                .Where(e => e.OrganiserID == 0 || e.OrganiserID == UserId)
                .OrderBy(e => e.EventDate).ToList();
        }

        /// <summary>Loads an event and makes sure it belongs to the logged-in organiser.</summary>
        private async Task<EventDto?> LoadOwnedEventAsync(int id)
        {
            var r = await _api.GetAsync<EventDto>(ApiRoutes.Event(id));
            if (!r.Ok || r.Data == null)
            {
                TempData["Error"] = r.Error ?? "Event not found.";
                return null;
            }
            if (r.Data.OrganiserID != 0 && r.Data.OrganiserID != UserId)
            {
                TempData["Error"] = "You can only manage your own events.";
                return null;
            }
            return r.Data;
        }

        // ---------- Dashboard ----------
        public async Task<IActionResult> Index()
        {
            var events = await LoadMyEventsAsync();
            var summaries = new List<EventSummary>();
            foreach (var e in events)
            {
                var en = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.EventEnrolments(e.EventID));
                summaries.Add(new EventSummary { Event = e, Enrolments = en.Data?.Count ?? 0 });
            }
            return View(summaries);
        }

        // ---------- Events CRUD ----------
        public async Task<IActionResult> Events() => View(await LoadMyEventsAsync());

        [HttpGet]
        public IActionResult CreateEvent() => View("EventForm", new EventFormModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(EventFormModel model)
        {
            if (!ValidateEvent(model)) return View("EventForm", model);

            var r = await _api.PostAsync<EventDto>(ApiRoutes.Events, ToBody(model));
            if (!r.Ok)
            {
                ModelState.AddModelError("", r.Error ?? "Could not create the event.");
                return View("EventForm", model);
            }

            var warning = await UploadBannerAsync(r.Data?.EventID ?? 0, model.Banner);
            TempData["Success"] = "Event created." + warning;
            return RedirectToAction(nameof(Events));
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var e = await LoadOwnedEventAsync(id);
            if (e == null) return RedirectToAction(nameof(Events));

            return View("EventForm", new EventFormModel
            {
                EventID = e.EventID, Name = e.Name, Description = e.Description, EventDate = e.EventDate,
                Location = e.Location, Distance = e.Distance, EventType = e.EventType, BannerUrl = e.BannerUrl
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EventFormModel model)
        {
            model.EventID = id;
            if (!ValidateEvent(model)) return View("EventForm", model);

            var r = await _api.PutAsync(ApiRoutes.Event(id), ToBody(model));
            if (!r.Ok)
            {
                ModelState.AddModelError("", r.Error ?? "Could not update the event.");
                return View("EventForm", model);
            }

            var warning = await UploadBannerAsync(id, model.Banner);
            TempData["Success"] = "Event updated." + warning;
            return RedirectToAction(nameof(Events));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var r = await _api.DeleteAsync(ApiRoutes.Event(id));
            TempData[r.Ok ? "Success" : "Error"] = r.Ok ? "Event deleted." : r.Error;
            return RedirectToAction(nameof(Events));
        }

        private bool ValidateEvent(EventFormModel m)
        {
            if (m.Banner != null && m.Banner.Length > 0)
            {
                var problem = ParticipantController.ImageCheck(m.Banner);
                if (problem != null) ModelState.AddModelError(nameof(m.Banner), problem);
            }
            return ModelState.IsValid;
        }

        private static object ToBody(EventFormModel m) => new
        {
            m.Name, m.Description, m.EventDate, m.Location, m.Distance, m.EventType
        };

        private async Task<string> UploadBannerAsync(int eventId, IFormFile? banner)
        {
            if (banner == null || banner.Length == 0) return "";
            if (eventId == 0) return " (Banner not uploaded: the event ID was not returned by the API.)";
            var r = await _api.UploadFileAsync(ApiRoutes.EventBanner(eventId), banner);
            return r.Ok ? "" : $" (Banner not uploaded: {r.Error})";
        }

        // ---------- Categories ----------
        public async Task<IActionResult> Categories(int id)
        {
            var e = await LoadOwnedEventAsync(id);
            if (e == null) return RedirectToAction(nameof(Events));

            var c = await _api.GetAsync<List<CategoryDto>>(ApiRoutes.EventCategories(id));
            if (!c.Ok) ViewBag.Error = c.Error;
            return View(new CategoriesPageModel { Event = e, Categories = c.Data ?? new() });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(int id, string? name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Category name is required.";
                return RedirectToAction(nameof(Categories), new { id });
            }

            var r = await _api.PostAsync(ApiRoutes.EventCategories(id), new { name = name.Trim(), description });
            TempData[r.Ok ? "Success" : "Error"] = r.Ok ? "Category added." : r.Error;
            return RedirectToAction(nameof(Categories), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id, int categoryId)
        {
            var r = await _api.DeleteAsync(ApiRoutes.Category(categoryId));
            TempData[r.Ok ? "Success" : "Error"] = r.Ok ? "Category removed." : r.Error;
            return RedirectToAction(nameof(Categories), new { id });
        }

        // ---------- Enrolments ----------
        public async Task<IActionResult> Enrolments(int id)
        {
            var e = await LoadOwnedEventAsync(id);
            if (e == null) return RedirectToAction(nameof(Events));

            var r = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.EventEnrolments(id));
            if (!r.Ok) ViewBag.Error = r.Error;
            return View(new EnrolmentsPageModel { Event = e, Enrolments = r.Data ?? new() });
        }

        // ---------- Results ----------
        [HttpGet]
        public async Task<IActionResult> Results(int id)
        {
            var e = await LoadOwnedEventAsync(id);
            if (e == null) return RedirectToAction(nameof(Events));

            var en = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.EventEnrolments(id));
            var res = await _api.GetAsync<List<ResultDto>>(ApiRoutes.EventResults(id));
            if (!en.Ok) ViewBag.Error = en.Error;

            var existing = res.Data ?? new();
            var rows = (en.Data ?? new()).Select(x =>
            {
                var prev = existing.FirstOrDefault(r => r.ParticipantID == x.ParticipantID);
                return new ResultRow
                {
                    EnrolmentID = x.EnrolmentID, ParticipantID = x.ParticipantID,
                    ParticipantName = x.ParticipantName, CategoryName = x.CategoryName,
                    FinishTime = prev?.FinishTime, Position = prev?.Position > 0 ? prev.Position : null
                };
            }).ToList();

            return View(new ResultsPageModel { Event = e, Rows = rows });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Results(int id, List<ResultRow> rows)
        {
            var filled = rows.Where(r => !string.IsNullOrWhiteSpace(r.FinishTime) || r.Position.HasValue).ToList();

            if (filled.Any(r => string.IsNullOrWhiteSpace(r.FinishTime) || !r.Position.HasValue))
            {
                TempData["Error"] = "Each result needs both a finish time (hh:mm:ss) and a position.";
                return RedirectToAction(nameof(Results), new { id });
            }

            var bad = filled.FirstOrDefault(r => !System.Text.RegularExpressions.Regex.IsMatch(r.FinishTime!, @"^\d{1,2}:[0-5]\d:[0-5]\d$"));
            if (bad != null)
            {
                TempData["Error"] = $"Finish time for {bad.ParticipantName} must be in hh:mm:ss format.";
                return RedirectToAction(nameof(Results), new { id });
            }

            if (filled.Count == 0)
            {
                TempData["Error"] = "Enter at least one result.";
                return RedirectToAction(nameof(Results), new { id });
            }

            var failures = 0;
            string? lastError = null;
            foreach (var r in filled)
            {
                var res = await _api.PostAsync(ApiRoutes.EventResults(id), new
                {
                    enrolmentID = r.EnrolmentID, participantID = r.ParticipantID,
                    finishTime = r.FinishTime!.PadLeft(8, '0'), position = r.Position
                });
                if (!res.Ok) { failures++; lastError = res.Error; }
            }

            if (failures == 0) TempData["Success"] = $"{filled.Count} result(s) published.";
            else TempData["Error"] = $"{failures} result(s) could not be saved. {lastError}";
            return RedirectToAction(nameof(Results), new { id });
        }
    }
}
