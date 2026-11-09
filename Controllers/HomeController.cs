using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using System.Diagnostics;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role switch
            {
                "Organiser" => RedirectToAction("Index", "Organiser"),
                "Participant" => RedirectToAction("Index", "Events"),
                _ => RedirectToAction("Login", "Account")
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
