namespace Evaders.Controllers
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;

    public class GameController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}