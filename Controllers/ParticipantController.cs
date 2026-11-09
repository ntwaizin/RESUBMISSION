using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [RoleRequired("Participant")]
    public class ParticipantController : Controller
    {
        private readonly RaceDayApiService _api;

        public ParticipantController(RaceDayApiService api) => _api = api;

        public async Task<IActionResult> Index()
        {
            var enrol = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.MyEnrolments);
            var results = await _api.GetAsync<List<ResultDto>>(ApiRoutes.MyResults);
            ViewBag.Enrolments = enrol.Data ?? new List<EnrolmentDto>();
            ViewBag.ResultCount = (results.Data ?? new()).Count;
            if (!enrol.Ok) ViewBag.Error = enrol.Error;
            return View();
        }

        public async Task<IActionResult> MyEvents()
        {
            var r = await _api.GetAsync<List<EnrolmentDto>>(ApiRoutes.MyEnrolments);
            if (!r.Ok) ViewBag.Error = r.Error;
            return View((r.Data ?? new()).OrderBy(e => e.EventDate).ToList());
        }

        public async Task<IActionResult> MyResults()
        {
            var r = await _api.GetAsync<List<ResultDto>>(ApiRoutes.MyResults);
            if (!r.Ok) ViewBag.Error = r.Error;
            return View((r.Data ?? new()).OrderByDescending(x => x.EventDate).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var r = await _api.GetAsync<ProfileModel>(ApiRoutes.Profile);
            if (!r.Ok) ViewBag.Error = r.Error;
            return View(r.Data ?? new ProfileModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var r = await _api.PutAsync(ApiRoutes.Profile, new
            {
                model.FirstName, model.LastName, model.Email, model.PhoneNumber
            });

            TempData[r.Ok ? "Success" : "Error"] = r.Ok ? "Profile updated." : r.Error;
            return RedirectToAction(nameof(Profile));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPicture(IFormFile? picture)
        {
            var problem = ImageCheck(picture);
            if (problem != null) { TempData["Error"] = problem; return RedirectToAction(nameof(Profile)); }

            var r = await _api.UploadFileAsync(ApiRoutes.ProfilePicture, picture!);
            TempData[r.Ok ? "Success" : "Error"] = r.Ok ? "Profile picture updated." : r.Error;
            return RedirectToAction(nameof(Profile));
        }

        internal static string? ImageCheck(IFormFile? file)
        {
            if (file == null || file.Length == 0) return "Please choose an image to upload.";
            if (file.Length > 5 * 1024 * 1024) return "Image must be smaller than 5 MB.";
            var ok = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            return ok.Contains(file.ContentType.ToLowerInvariant()) ? null : "Only JPG, PNG, WEBP or GIF images are allowed.";
        }
    }
}
