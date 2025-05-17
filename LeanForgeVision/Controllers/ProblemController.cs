using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using LeanForgeVision.Database;
using LeanForgeVision.Models;

namespace LeanForgeVision.Controllers 
{
    public class ProblemController : Controller
    {
        private LeanForgeVision.Database.DbConnection _dbConnection = new LeanForgeVision.Database.DbConnection();

        [HttpGet]
        public JsonResult GetProblemList()
        {
            var result = _dbConnection.GetProblems();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
            
        [HttpGet]
        public JsonResult GetTodayProblemCount()
        {
            var result = _dbConnection.GetTodayProblemCount();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetProblemCountsTodayAndYesterday()
        {
            var counts = _dbConnection.GetProblemCountsTodayAndYesterday();

            int today = counts.today;
            int yesterday = counts.yesterday;

            string trend = "equal";
            double percentageChange = 0;

            if (yesterday == 0 && today > 0)
            {
                trend = "increase";
                percentageChange = 100; // avoid divide by zero
            }
            else if (yesterday == 0 && today == 0)
            {
                trend = "equal";
                percentageChange = 0;
            }
            else if (today > yesterday)
            {
                trend = "increase";
                percentageChange = ((double)(today - yesterday) / yesterday) * 100;
            }
            else if (today < yesterday)
            {
                trend = "decrease";
                percentageChange = ((double)(yesterday - today) / yesterday) * 100;
            }

            return Json(new
            {
                today,
                yesterday,
                trend,
                percentage = Math.Round(percentageChange, 2)
            }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTop3MonthlyProblems()
        {
            var result = _dbConnection.GetTop3MonthlyProblems();
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetProblemReportCounts()
        {
            var result = _dbConnection.GetProblemReportCounts(); // asumsi Anda punya service ini
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetGateProblemReports()
        {
            var result = _dbConnection.GetGateProblemReports();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult GetTodayProblemReports()
        {
            var reports = _dbConnection.GetTodayProblemReports(); // Mendapatkan data dari database
            return Json(reports, JsonRequestBehavior.AllowGet); // Mengembalikan data dalam format JSON
        }
        [HttpGet]
        public JsonResult GetWeeklyProblemReports()
        {
            var result = _dbConnection.GetWeeklyProblemReports();
            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpGet]
        public JsonResult GetProblemReportsPaged(int page = 1, int pageSize = 10)
        {
            var allReports = _dbConnection.GetProblemReports(); // Fungsi dari jawaban sebelumnya
            int totalRecords = allReports.Count;

            // Hitung data yang akan ditampilkan berdasarkan page dan pageSize
            var pagedData = allReports
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Return JSON dengan metadata pagination
            var result = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalRecords = totalRecords,
                totalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                data = pagedData
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public JsonResult InsertProblem(int dailyPlanDetailId, int dailyPlanHeadId, int reportTotal, int problemId, string reporterId, int gateId)
        {
            try
            {
                // Step 1: Check if parameters are valid
                if (dailyPlanDetailId <= 0 || dailyPlanHeadId <= 0 || reportTotal <= 0 || problemId <= 0 || string.IsNullOrEmpty(reporterId) || gateId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request data." });
                }

                // Step 2: Insert the problem report using the individual parameters
                _dbConnection.InsertProblemReport(dailyPlanDetailId, dailyPlanHeadId, reportTotal, problemId, reporterId, gateId);

                // Step 3: Return success response
                return Json(new { success = true, message = "Problem report inserted successfully." });
            }
            catch (SqlException sqlEx)
            {
                // Log SQL exception and return error message
                Debug.Print($"SQL Error: {sqlEx.Message}");
                return Json(new { success = false, message = "Database Error: " + sqlEx.Message });
            }
            catch (Exception ex)
            {
                // Log general exception and return error message
                Debug.Print($"Exception: {ex.Message}");
                return Json(new { success = false, message = "Server Error: " + ex.Message });
            }
        }





    }
}