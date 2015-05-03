using System.Web.Optimization;

namespace ShibpurConnectWebApp
{
    public class BundleConfig
    {
        // For more information on bundling, visit http://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at http://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                      "~/Scripts/bootstrap.js",
                      "~/Scripts/date.js",
                      "~/Scripts/toastr.js",
                      "~/Scripts/jquery-ui-1.11.4.min.js",
                      "~/Scripts/respond.js"));

            bundles.Add(new ScriptBundle("~/bundles/tokeninput").Include(
                      "~/Scripts/jquery.tokeninput.js"));

            bundles.Add(new ScriptBundle("~/bundles/typehead").Include(
                     "~/Scripts/typehead/typeahead.bundle.js"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                      "~/Content/bootstrap.css",
                      "~/Content/site.css",
                      "~/Content/font-awesome.css",
                      "~/Content/social-button.css",
                      "~/Content/toastr.min.css",                      
                      "~/Content/token-input.css"));

            bundles.Add(new StyleBundle("~/content/themes/base/jquery").Include(
           "~/Content/themes/base/core.css",
           "~/Content/themes/base/resizable.css",
           "~/Content/themes/base/selectable.css",
           "~/Content/themes/base/accordion.css",
           "~/Content/themes/base/autocomplete.css",
           "~/Content/themes/base/button.css",
           "~/Content/themes/base/dialog.css",
           "~/Content/themes/base/slider.css",
           "~/Content/themes/base/tabs.css",
           "~/Content/themes/base/datepicker.css",
           "~/Content/themes/base/progressbar.css",
           "~/Content/themes/base/theme.css"));

            // Set EnableOptimizations to false for debugging. For more information,
            // visit http://go.microsoft.com/fwlink/?LinkId=301862
            BundleTable.EnableOptimizations = true;
        }
    }
}