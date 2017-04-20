using System.Web.Mvc;

namespace Altis.CommonLogin.Web.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public ActionResult Index()
        {
            return this.View();
        }
    }
}