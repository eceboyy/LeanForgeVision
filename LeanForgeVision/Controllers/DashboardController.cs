using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LeanForgeVision.Controllers
{
    public class DashboardController : Controller
    {
        // GET: Dashboard
        public ActionResult Index()
        {
            return View();
        }
        public ActionResult Location()
        {
            return View();
        }
        public ActionResult Problem()
        {
            return View();
        }
    }
}