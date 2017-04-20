using System;
using System.IdentityModel.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Web.Configuration;
using System.Web.Hosting;
using Altis.CommonLogin.Owin;
using Altis.CommonLogin.Web;
using Kentor.AuthServices;
using Kentor.AuthServices.Configuration;
using Kentor.AuthServices.Owin;
using Kentor.AuthServices.WebSso;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup("MasterOwinStartup", typeof(Startup))]
namespace Altis.CommonLogin.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Authentication.Configuration(app);
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            app.UseKentorAuthServicesAuthentication(CreateAuthServicesOptions());
        }

        private static KentorAuthServicesAuthenticationOptions CreateAuthServicesOptions()
        {
            //Retrieve settings
            var commonLoginUrl = RetrieveAppSetting("CommonLoginUrl");
            var singleSignOnIssuerUrl = RetrieveAppSetting("SingleSignOnIssuerUrl");
            var singleSignOnTargetUrl = RetrieveAppSetting("SingleSignOnTargetUrl");
            var singleLogoutTargetUrl = WebConfigurationManager.AppSettings["SingleLogoutTargetUrl"];

            //Configure service provider
            var serviceProvider = new SPOptions
            {
                EntityId = new EntityId(commonLoginUrl),
                ReturnUrl = new Uri(commonLoginUrl + "/Account/ExternalLoginCallback"),
            };

            //Configure identity provider
            var identityProvider = new IdentityProvider(new EntityId(singleSignOnIssuerUrl), serviceProvider)
            {
                AllowUnsolicitedAuthnResponse = true,
                Binding = Saml2BindingType.HttpRedirect,
                SingleSignOnServiceUrl = new Uri(singleSignOnTargetUrl)
            };

            //Set single logout if it has been specified
            if (!string.IsNullOrEmpty(singleLogoutTargetUrl))
            {
                identityProvider.SingleLogoutServiceUrl = new Uri(singleLogoutTargetUrl);
            }

            //Load certificate
            var certificatePath = HostingEnvironment.MapPath("~/App_Data/IdentityProviderCertificate.cer");
            if (string.IsNullOrEmpty(certificatePath))
            {
                throw new Exception($"Could not find the identity provider certificate path. Value given was: {certificatePath}.");
            }
            identityProvider.SigningKeys.AddConfiguredKey(new X509Certificate2(certificatePath));

            //Return authenticaton options
            var authenticationOptions = new KentorAuthServicesAuthenticationOptions(false) {SPOptions = serviceProvider};
            authenticationOptions.IdentityProviders.Add(identityProvider);
            return authenticationOptions;
        }

        private static string RetrieveAppSetting(string key)
        {
            var appSettingsString = WebConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(appSettingsString))
            {
                throw new Exception($"Could not find the key {appSettingsString} within the Web.Config appSettings.");
            }
            return appSettingsString;
        }
    }
}