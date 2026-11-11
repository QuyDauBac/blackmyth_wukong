using System.Linq;
using System.Web.Mvc;
using WukongWeb.Models;

namespace WukongWeb.Infrastructure
{
    /// <summary>
    /// One Entity Framework ObjectContext per request, plus the signed-in user helper.
    /// The model is generated from the existing SQL Server database (Database First).
    /// </summary>
    public abstract class BaseController : Controller
    {
        protected BaseController()
        {
            Db = new WukongEntities();
        }

        protected WukongEntities Db { get; private set; }

        protected WukongWeb.Models.User CurrentUser
        {
            get
            {
                var principal = base.User;
                if (principal?.Identity == null || !principal.Identity.IsAuthenticated)
                {
                    return null;
                }

                var email = principal.Identity.Name;
                return Db.Users.FirstOrDefault(u => u.NormalizedEmail == email && u.IsActive);
            }
        }

        protected bool IsAdmin
        {
            get { return base.User?.IsInRole("Admin") == true; }
        }

        /// <summary>
        /// Renders the styled 404 page with a real 404 status code, used by actions
        /// that cannot find the requested record and by unmatched action names.
        /// </summary>
        protected ActionResult NotFoundPage()
        {
            Response.StatusCode = 404;
            Response.TrySkipIisCustomErrors = true;
            return View("~/Views/Home/NotFound.cshtml");
        }

        protected override void HandleUnknownAction(string actionName)
        {
            NotFoundPage().ExecuteResult(ControllerContext);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
