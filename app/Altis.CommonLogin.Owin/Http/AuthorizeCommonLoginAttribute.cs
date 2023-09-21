using System.Web.Configuration;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Altis.CommonLogin.Owin.Http
{
    public class AuthorizeCommonLoginAttribute : AuthorizeAttribute
    {
        private readonly bool isAuthenticationDisabled;

        public AuthorizeCommonLoginAttribute()
        {
            this.isAuthenticationDisabled = WebConfigurationManager.AppSettings["DisableAuthentication"]?.ToLower().Equals("true") ?? false;
        }

        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            return this.isAuthenticationDisabled || base.IsAuthorized(actionContext);
        }
    }
}
