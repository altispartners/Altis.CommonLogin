using System.Security.Principal;
using System.Web;
using System.Web.Mvc;

namespace Altis.CommonLogin.Owin
{
    public class DebugAuthorisationFilter : IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            HttpContext.Current.User = new GenericPrincipal(new GenericIdentity("debug"), new string[] {});
        }
    }
}
