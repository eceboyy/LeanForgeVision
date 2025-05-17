using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LeanForgeVision.Database;
using LeanForgeVision.Models;


namespace LeanForgeVision.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        private DbConnection _dbConnection = new DbConnection();

        public ActionResult GetEmployeesMaster()
        {
            List<EmployeeModel> employees = _dbConnection.GetEmployeesMasterDB();
            return Json(employees, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetUserName()
        {
            List<UserModel> employees = _dbConnection.GetAllUsersWithRoleName();
            return Json(employees, JsonRequestBehavior.AllowGet);
        }

    }
}