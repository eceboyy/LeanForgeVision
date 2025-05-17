using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LeanForgeVision.Controllers
{
    public class PagesController : Controller
    {
        // GET: Pages
        public ActionResult StarterPage()
        {
            return View();
        }
        public ActionResult Maintenance()
        {
            return View();
        }
        public ActionResult ComingSoon()
        {
            return View();
        }
        public ActionResult Error404()
        {
            return View();
        }
        public ActionResult Error500()
        {
            return View();
        }
        public ActionResult Error503()
        {
            return View();
        }
    }
}