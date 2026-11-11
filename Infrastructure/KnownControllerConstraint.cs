using System;
using System.Web;
using System.Web.Routing;

namespace WukongWeb.Infrastructure
{
    /// <summary>
    /// Only lets the default route claim URLs whose first segment is a real controller.
    /// Anything else falls through to the catch-all route and the styled 404 page,
    /// instead of dying inside MVC with an empty IIS error response.
    /// </summary>
    public class KnownControllerConstraint : IRouteConstraint
    {
        public bool Match(HttpContextBase httpContext, Route route, string parameterName,
            RouteValueDictionary values, RouteDirection routeDirection)
        {
            var name = Convert.ToString(values[parameterName]);
            if (string.IsNullOrWhiteSpace(name))
            {
                return true;
            }

            var typeName = "WukongWeb.Controllers." + name + "Controller";
            return typeof(KnownControllerConstraint).Assembly.GetType(typeName, false, true) != null;
        }
    }
}
