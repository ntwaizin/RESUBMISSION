using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly RaceDayApiService _api;

        public AccountController(RaceDayApiService api) => _api = api;

        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("UserRole")))
                return RedirectToAction("Index", "Home");
            return View(new LoginViewModel());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (!await _api.LoginAsync(model.Email, model.Password))
            {
                ModelState.AddModelError("", "Invalid login details, or the service is unavailable. Please try again.");
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View(new RegisterViewModel());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (model.Role != "Organiser" && model.Role != "Participant")
                ModelState.AddModelError(nameof(model.Role), "Please choose a valid role.");
            if (!ModelState.IsValid) return View(model);

            var result = await _api.PostAsync(ApiRoutes.Register, new
            {
                model.FirstName, model.LastName, model.Email, model.Password, model.Role
            });

            if (!result.Ok)
            {
                ModelState.AddModelError("", result.Error ?? "Registration failed.");
                return View(model);
            }

            TempData["Success"] = "Account created. Please log in.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _api.LogoutAsync();
            return RedirectToAction(nameof(Login));
        }
    }
}
