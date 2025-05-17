using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LeanForgeVision.Database;
using LeanForgeVision.Models;
namespace LeanForgeVision.Controllers
{
    public class GateController : Controller
    {
        private DbConnection _dbConnection = new DbConnection();
        // GET: Gate
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetGateLocation()
        {
            try
            {
                var GateLocation = _dbConnection.GetGatesWithStatusName();
                return Json(new
                {
                    success = true,
                    data = GateLocation
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error retrieving data",
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetGateLocationForInactive(int scheduleHeadId)
        {
            try
            {
                var GateLocation = _dbConnection.GetGatesWithStatusNameForInactiveGate(scheduleHeadId);
                return Json(new
                {
                    success = true,
                    data = GateLocation
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Error retrieving data",
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetTotalGateStatus5()
        {
            var result = _dbConnection.GetTotalGateStatus5();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetActiveGateCounts()
        {
            var result = _dbConnection.GetActiveGateCounts();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetDailyPlanGateJson()
        {
            var result = _dbConnection.GetDailyPlanGateData();
            return Json(result,JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetRealtimeHourlyGate()
        {
            var result =_dbConnection.GetTotalSortedPerGateRealtimeHourly();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetWeeklyActiveGateCounts()
        {
            var result = _dbConnection.GetWeeklyActiveGateCounts();
            return Json(result, JsonRequestBehavior.AllowGet);
        }



    }
}