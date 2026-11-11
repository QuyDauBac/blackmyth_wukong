using System.Web.Optimization;

namespace WukongWeb
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                "~/Scripts/jquery-3.7.0.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/Scripts/jquery.validate.min.js",
                "~/Scripts/jquery.validate.unobtrusive.min.js"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                "~/Scripts/bootstrap.bundle.min.js"));

            bundles.Add(new StyleBundle("~/Content/bootstrapcss").Include(
                "~/Content/bootstrap.min.css",
                "~/Content/site-admin.css"));

            // Keep the cinematic landing page on its hand-written stylesheet so the
            // original layout is preserved exactly.
            bundles.Add(new StyleBundle("~/Content/landingcss").Include(
                "~/Content/styles.css"));

            bundles.Add(new StyleBundle("~/Content/checkoutcss").Include(
                "~/Content/styles.css",
                "~/Content/checkout.css"));

            BundleTable.EnableOptimizations = false;
        }
    }
}
