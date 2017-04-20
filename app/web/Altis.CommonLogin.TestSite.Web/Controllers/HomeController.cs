using System.Web.Mvc;

namespace Altis.CommonLogin.TestClient.Web.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return this.View();
        }
    }
}