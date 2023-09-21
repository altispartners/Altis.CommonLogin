using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace Altis.CommonLogin.Owin.Mvc
{
    public class AuthorizeCommonLoginAttribute : AuthorizeAttribute
    {
        private readonly bool isAuthenticationDisabled;

        public AuthorizeCommonLoginAttribute()
        {
            this.isAuthenticationDisabled = WebConfigurationManager.AppSettings["DisableAuthentication"]?.ToLower().Equals("true") ?? false;
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            return this.isAuthenticationDisabled || base.AuthorizeCore(httpContext);
        }
    }
}
