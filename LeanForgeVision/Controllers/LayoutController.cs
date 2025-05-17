using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LeanForgeVision.Controllers
{
    public class LayoutController : Controller
    {
        // GET: Layout
        public ActionResult Creative()
        {
            return View();
        }
        public ActionResult Detached()
        {
            return View();
        }
    }
}