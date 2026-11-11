using System.Web.Mvc;
using System.Web.Routing;
using WukongWeb.Infrastructure;

namespace WukongWeb
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                constraints: new { controller = new KnownControllerConstraint() }
            );

            // Unknown URLs (typos, deleted records) fall through to a styled 404 page
            // instead of the raw IIS error screen. Registered last on purpose.
            routes.MapRoute(
                name: "NotFound",
                url: "{*url}",
                defaults: new { controller = "Home", action = "NotFound" }
            );
        }
    }
}
