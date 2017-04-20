using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using Altis.CommonLogin.Web.Controllers.ActionResults;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;

namespace Altis.CommonLogin.Web.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            //Authenticated user, redirect them to where they want to go
            if (this.User.Identity != null && this.User.Identity.IsAuthenticated)
            {
                return this.SafeRedirect(returnUrl);
            }

            //Unauthenticated user, redirect them to identity provider
            var externalProviders = this.HttpContext.GetOwinContext().Authentication.GetExternalAuthenticationTypes().ToList();
            if (!externalProviders.Any())
            {
                throw new Exception("No external authentication provider has been configured.");
            }
            var provider = externalProviders.First().AuthenticationType;
            return new ChallengeResult(provider, this.Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            var loginInfo = await this.HttpContext.GetOwinContext().Authentication.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return this.RedirectToAction("Login");
            }

            //Create the identity for the authenticated user
            var identity = new ClaimsIdentity(DefaultAuthenticationTypes.ApplicationCookie);

            //Custom logic to map the external identity provider claims to the internal identity's claims is done here...

            //Set name ID - we require a name ID to be provided as we use it as the anti-forgery claim type (see Altis.CommonLogin.Owin\Authentication.cs:line 84)
            var nameIdClaim = loginInfo.ExternalIdentity.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier);
            identity.AddClaim(nameIdClaim);

            //Set name - fall back on the name ID if we don't have one
            var name = loginInfo.ExternalIdentity.Claims.SingleOrDefault(c => c.Type == "User.FirstName")?.Value ?? nameIdClaim.Value;
            identity.AddClaim(new Claim(ClaimTypes.Name, name));

            //Set all other claims, except role claims
            foreach (var claim in loginInfo.ExternalIdentity.Claims.Where(c => c.Type != ClaimTypes.Role))
            {
                if (identity.Claims.All(c => c.Type != claim.Type))
                {
                    identity.AddClaim(claim);
                }
            }

            //Take the user's roles from the external identity provider and map them to the internal access roles that they apply to
            //For example, the external identity may provide a "FullAccess" role which gives access to all systems, or a "RestrictedAccess" role which only allows access to some
            foreach (var role in loginInfo.ExternalIdentity.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value))
            {
                var accessRoles = WebConfigurationManager.AppSettings[role + "Roles"];
                if (!string.IsNullOrEmpty(accessRoles))
                {
                    accessRoles.Split(',').ToList().ForEach(r => identity.AddClaim(new Claim(ClaimTypes.Role, r)));
                }
            }

            //Sign the user in
            this.HttpContext.GetOwinContext().Authentication.SignIn(identity);

            //Send the user to where they need to go
            return this.SafeRedirect(returnUrl);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            //Log the user out of common login
            this.HttpContext.GetOwinContext().Authentication.SignOut();

            //If specified, log the user out of the identity provider
            var singleLogoutTargetUrl = WebConfigurationManager.AppSettings["SingleLogoutTargetUrl"];
            if (!string.IsNullOrEmpty(singleLogoutTargetUrl))
            {
                return new RedirectResult(singleLogoutTargetUrl);
            }

            return this.RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public ActionResult InsufficientPrivileges()
        {
            return this.View();
        }

        private ActionResult SafeRedirect(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return new RedirectResult(returnUrl);
            }

            //If we don't have a return Url then just send them to the Common Login homepage
            return this.RedirectToAction("Index", "Home");
        }
    }
}