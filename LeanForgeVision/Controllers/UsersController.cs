using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace LeanForgeVision.Controllers
{
    public class UsersController : Controller
    {
        // GET: Users
        public ActionResult Profile()
        {
            return View();
        }
        public ActionResult Settings()
        {
            return View();
        }
    }
}