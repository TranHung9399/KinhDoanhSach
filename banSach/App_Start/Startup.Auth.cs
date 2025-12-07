using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using System;

namespace banSach
{
    public partial class Startup
    {
        public void ConfigureAuth(IAppBuilder app)
        {
            // Enable the application to use a cookie to store information for the signed-in user
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new PathString("/Dangnhap/Index"),
                ExpireTimeSpan = TimeSpan.FromDays(7),
                SlidingExpiration = true
            });

            // Use a cookie to temporarily store information about a user logging in with a third party login provider
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ExternalCookie",
                AuthenticationMode = Microsoft.Owin.Security.AuthenticationMode.Passive,
                CookieName = ".AspNet.ExternalCookie",
                ExpireTimeSpan = TimeSpan.FromMinutes(5)
            });

            // Configure Google authentication
            var googleOptions = new GoogleOAuth2AuthenticationOptions()
            {
                ClientId = System.Configuration.ConfigurationManager.AppSettings["GoogleClientId"],
                ClientSecret = System.Configuration.ConfigurationManager.AppSettings["GoogleClientSecret"],
                SignInAsAuthenticationType = "ExternalCookie",
                Provider = new GoogleOAuth2AuthenticationProvider()
                {
                    OnAuthenticated = async context =>
                    {
                        context.Identity.AddClaim(new System.Security.Claims.Claim("urn:google:name", context.Identity.Name));
                        context.Identity.AddClaim(new System.Security.Claims.Claim("urn:google:email", context.Email));
                        if (context.User.GetValue("picture") != null)
                        {
                            context.Identity.AddClaim(new System.Security.Claims.Claim("urn:google:picture", context.User.GetValue("picture").ToString()));
                        }
                    }
                }
            };
            
            app.UseGoogleAuthentication(googleOptions);
        }
    }
}

