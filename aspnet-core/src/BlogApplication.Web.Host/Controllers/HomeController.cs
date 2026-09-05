using Abp;
using Abp.Timing;
using BlogApplication.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BlogApplication.Web.Host.Controllers
{
    public class HomeController : BlogApplicationControllerBase
    {
        public IActionResult Index()
        {
            return Redirect("/swagger");
        }
    }
}
