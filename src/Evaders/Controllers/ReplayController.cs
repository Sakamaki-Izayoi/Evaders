namespace Evaders.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    public class ReplayController : Controller
    {
        public JsonResult Index()
        {
            return new JsonResult(null);
        }

        [HttpGet]
        public JsonResult Get(string id)
        {
            return new JsonResult(new { id, text="hello from c# c:"});
        }
    }
}