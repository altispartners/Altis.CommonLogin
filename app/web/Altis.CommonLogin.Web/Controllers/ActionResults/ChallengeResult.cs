using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;

namespace Altis.CommonLogin.Web.Controllers.ActionResults
{
    public class ChallengeResult : HttpUnauthorizedResult
    {
        private string LoginProvider { get; }
        private string RedirectUri { get; }

        public ChallengeResult(string provider, string redirectUri)
        {
            this.LoginProvider = provider;
            this.RedirectUri = redirectUri;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var properties = new AuthenticationProperties { RedirectUri = this.RedirectUri };
            context.HttpContext.GetOwinContext().Authentication.Challenge(properties, this.LoginProvider);
        }
    }
}