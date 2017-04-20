using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;

namespace Altis.CommonLogin.Owin
{
    public class RoleClaimAttribute : AuthorizeAttribute
    {
        private readonly string roles;

        public RoleClaimAttribute(string roles)
        {
            this.roles = roles;
        }

        protected override bool AuthorizeCore(HttpContextBase context)
        {
            if (base.AuthorizeCore(context))
            {
                var id = (ClaimsIdentity)context.User.Identity;
                return this.roles.Split(',').Any(role => id.HasClaim(ClaimTypes.Role, role));
            }
            return false;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext context)
        {
            if (!context.HttpContext.User.Identity.IsAuthenticated)
            {
                base.HandleUnauthorizedRequest(context);
                return;
            }

            var commonLoginUrl = WebConfigurationManager.AppSettings["CommonLoginUrl"];
            if (commonLoginUrl != null)
            {
                var redirectUrl = commonLoginUrl + "/Account/InsufficientPrivileges";
                context.Result = new RedirectResult(redirectUrl);
            }
        }
    }
}
