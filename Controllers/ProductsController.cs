using Microsoft.AspNetCore.Mvc;

namespace AerodyneCompressors.Controllers
{
    public class ProductsController : Controller
    {
        public IActionResult VariableSpeedPM()
        {
            return View();
        }
        public IActionResult DirectDrive()
        {
            return View();
        }
        public IActionResult AirDryer()
        {
            return View();
        }
    }
}
