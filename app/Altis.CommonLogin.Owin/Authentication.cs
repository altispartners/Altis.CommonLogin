using System;
using System.Configuration;
using System.Security.Claims;
using System.Web;
using System.Web.Configuration;
using System.Web.Helpers;
using System.Web.Mvc;
using Altis.CommonLogin.Owin;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using SameSiteMode = Microsoft.Owin.SameSiteMode;

[assembly: OwinStartup(typeof(Authentication))]
namespace Altis.CommonLogin.Owin
{
    public static class Authentication
    {
        const double DefaultCookieExpiryHours = 12;

        public static void Configuration(IAppBuilder app)
        {
            //Authentication can be disabled via a flag in the app settings
            var disableAuthentication = WebConfigurationManager.AppSettings["DisableAuthentication"];
            if (disableAuthentication == null || !disableAuthentication.ToLower().Equals("true"))
            {
                ConfigureAuthentication(app);
            }
            else
            {
                //Authentication disabled, create a default debugging identity
                GlobalFilters.Filters.Add(new DebugAuthorisationFilter());
            }
        }

        private static void ConfigureAuthentication(IAppBuilder app)
        {
            //Register filters for authentication
            RegisterFilters(GlobalFilters.Filters);

            //Create cookie authentication provider
            var provider = new CookieAuthenticationProvider();
            var originalHandler = provider.OnApplyRedirect;
            var commonLoginUrl = WebConfigurationManager.AppSettings["CommonLoginUrl"];
            if (commonLoginUrl != null)
            {
                provider.OnApplyRedirect = context =>
                {
                    if (IsApiRequest(context.Request))
                    {
                        return;
                    }
                    var redirectUri = commonLoginUrl + "/Account/Login" + new QueryString(context.Options.ReturnUrlParameter, context.Request.Uri.AbsoluteUri);
                    context.RedirectUri = redirectUri;
                    originalHandler.Invoke(context);
                };
            }

            //Authentication cookie expiry timespan
            if (!double.TryParse(WebConfigurationManager.AppSettings["CookieExpiryHours"], out var cookieExpiryHours))
            {
                cookieExpiryHours = DefaultCookieExpiryHours;
            }

            //Authentication cookie domain
            var cookieDomain = WebConfigurationManager.AppSettings["CookieDomain"];
            if (string.IsNullOrEmpty(cookieDomain))
            {
                throw new ArgumentException("No cookie domain was specified. Unable to proceed with authentication configuration.");
            }

            //Setup the application to use cookie authentication, information for the signed in user is stored in the cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                CookieSecure = CookieSecureOption.Always,
                SlidingExpiration = false,
                CookieName = "CommonLoginPage",
                ExpireTimeSpan = TimeSpan.FromHours(cookieExpiryHours),
                CookieDomain = cookieDomain,
                Provider = provider,
                CookieSameSite = SameSiteMode.None
            });

            //Set the anti-forgery claim type
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
        }

        private static void RegisterFilters(GlobalFilterCollection filters)
        {
            //Check if role authorisation has been disabled in the app settings
            var disableAuthorisationRoles = WebConfigurationManager.AppSettings["DisableAuthorisationRoles"];
            if (disableAuthorisationRoles == null || !disableAuthorisationRoles.ToLower().Equals("true"))
            {
                //Setup role authorisation. Only users with this role will be able to access the relevant application.
                var roles = ConfigurationManager.AppSettings["AuthorisedRoles"];
                if (roles == null)
                {
                    throw new Exception("No authorised roles found in app settings. Unable to proceed with authentication configuration.");
                }

                //Register authorise filter
                filters.Add(new RoleClaimAttribute(roles));
            }
        }

        private static bool IsApiRequest(IOwinRequest request)
        {
            var apiPath = WebConfigurationManager.AppSettings["SkipCookieRedirectForApiPath"] ?? "~/api/";
            return !string.IsNullOrWhiteSpace(apiPath) && request.Uri.LocalPath.ToLower().StartsWith(VirtualPathUtility.ToAbsolute(apiPath));
        }
    }
}
