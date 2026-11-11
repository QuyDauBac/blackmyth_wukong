using System;
using System.Security.Principal;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;

namespace WukongWeb
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // Forms authentication stores the role in the ticket UserData so that
        // [Authorize(Roles = "Admin")] can be enforced on the server.
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var cookie = Request.Cookies[FormsAuthentication.FormsCookieName];
            if (cookie == null) return;

            var ticket = FormsAuthentication.Decrypt(cookie.Value);
            if (ticket == null || ticket.Expired) return;

            var parts = (ticket.UserData ?? string.Empty).Split('|');
            var role = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : "User";

            var identity = new FormsIdentity(ticket);
            Context.User = new GenericPrincipal(identity, new[] { role });
        }
    }
}
