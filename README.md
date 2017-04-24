# Altis.CommonLogin
Altis.CommonLogin is an OWIN authentication middleware for use ASP.NET websites. It is intended for use with multiple websites operating on the same domain, where you want to have one common authentication system managing access to all sites.

The Altis.CommonLogin.Web project handles federation with a third-party SAML Identity Provider and can be used to provide authentication to any number of websites hosted on the same domain or at different sub domains, such that you can authenticate once and access to all related sites.

Altis.CommonLogin.Web uses *Kentor Authentication Services* to manage SAML implementation and access. Please check out the project [here](https://github.com/KentorIT/authservices "Kentor Authentication Services") for more detailed information and many thanks to the team there for their excellent work!


## Process Overview
An overview of the Altis.CommonLogin process can be seen in the diagram below:

![Altis.CommonLogin](docs/Altis.CommonLogin.png)


## Usage
Below is a complete example of how to customise you website to include Altis.CommonLogin. The prerequisites are an existing .NET website and an external identity provider.


## Project Structure
The Altis.CommonLogin solution contains three projects: one library and two websites.

### Altis.CommonLogin.Owin
This is the core library which needs to be included in all websites implementing Altis.CommonLogin functionality. The library is an OWIN middleware and is specified as the OWIN startup class. For more details on OWIN see the following resources:

- [http://owin.org/](http://owin.org/)
- [https://docs.microsoft.com/en-us/aspnet/core/fundamentals/owin](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/owin)

The [Authentication.cs](app/Altis.CommonLogin.Owin/Authentication.cs) class forms the core functionality of the library and specifies itself as the OwinStartup assembly, configures cookie authentication and applies user roles for the website through the [RoleClaimAttribute.cs](app/Altis.CommonLogin.Owin/RoleClaimAttribute.cs) class.

If you have other OWIN requirements you can override the default OWIN behaviour by specifying the appStartup in you Web.config file:

```
<configuration>
  <appSettings>
    <add key="owin:appStartup" value="MasterOwinStartup" />    
  </appSettings>
```

You can then instantiate Altis.CommonLogin along with any other OWIN middleware required:

```
using Altis.CommonLogin.Owin;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup("MasterOwinStartup", typeof(Altis.CommonLogin.TestSite.Web.Startup))]

namespace Altis.CommonLogin.TestSite.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            Authentication.Configuration(app);
            app.MapSignalR();
        }
    }
}
```

### Altis.CommonLogin.Web
This is the central authentication website and handles the SAML authentication with the third-party identity provider.

Authentication configuration is handled in [Startup.cs](app/web/Altis.CommonLogin.Web/App_Start/Startup.cs), and is primarily achieved through settings in the Web.config file (see the configuration section for more details).

The website exposes a number of endpoints. Of primary interest are the following:

- /Account/Login: This is the endpoint that authenticated sites will use to request user authentication.
- /AuthServices/Acs: This is the endpoint that third-party identity providers should send authentication responses to.

### Altis.CommonLogin.TestSite.Web
This site is a complete example of a site which requires authentication from the Altis.CommonLogin.Web. It contains no logic required for authentication, this being done solely through the inclusion of the Altis.CommonLogin library and configuration in the Web.config file.


## Configuration

### Altis.CommonLogin.TestSite.Web
| Setting | Required | Description |
| --- | --- | --- |
| AuthorisedRoles | If authorisation roles are enabled | A comma separated list of the internally specified roles that are able to access the website |
| CommonLoginUrl | Yes | The URL of the Altis.CommonLogin.Web authentication site |
| CookieDomain | Yes | The root domain that the sites are hosted on and that the authentication cookie will be stored for |
| DisableAuthentication | No | Disable Altis.CommonLogin for the website (useful primarily for debugging in Visual Studio) |
| DisableAuthorisationRoles | No | Disable the use of internal authorisation roles. All authenticated users will be able to access the site |

### Altis.CommonLogin.Web
| Setting | Required | Description |
| --- | --- | --- |
| CommonLoginUrl | Yes | The URL of the Altis.CommonLogin.Web authentication site |
| CookieDomain | Yes | The root domain that the sites are hosted on and that the authentication cookie will be stored for |
| CookieExpiryHours| No | The number of hours that the authentication cookie is valid for. Defaults to 12 hours |
| SingleSignOnIssuerUrl | Yes | External identity provider issuer URL |
| SingleSignOnTargetUrl | Yes | External identity provider SAML 2.0 sign on endpoint |
| SingleLogoutTargetUrl | No  | External identity provider single logout endpoint |
| [IdentityProviderRoleName]Roles | No | You can provide any number of identity provider roles parameters and give a comma separated list of the internal roles that they map to. These are role claims that may be provided by the external identity provider but not recognised by the internal websites. These are used in the Altis.CommonLogin.Web [AccountController.cs](app/web/Altis.CommonLogin.Web/Controllers/AccountController.cs) and the use can be adjusted within that class |


## Customisation
Beyond simple configuration of the application and your Identity provider the most likely area of customisation will be in the processing of the claims from the external identity provider and mapping these to claims on the internal identity. An example implementation taken from  [AccountController.cs](app/web/Altis.CommonLogin.Web/Controllers/AccountController.cs) can be seen below:

```C#
[AllowAnonymous]
public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
{
    ...

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

    ...
}
```

Here we assert that certain claims must be present, set the claims on the internal identity and setup our roles that the user has access to.