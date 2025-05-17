using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LeanForgeVision.Controllers
{
    public class TablesController : Controller
    {
        // GET: Tables
        public ActionResult Basic()
        {
            return View();
        }
        public ActionResult Datatables()
        {
            return View();
        }
        public ActionResult Editable()
        {
            return View();
        }
    }
}