using Microsoft.AspNetCore.Mvc;

namespace TestTaskWebstick.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImagesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        //[HttpPost]
        //[ActionName("CreateImage")]

    }
}
