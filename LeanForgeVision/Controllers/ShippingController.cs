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
    public class ShippingController : Controller
    {
        private DbConnection _dbConnection = new DbConnection();
        // GET: Apps
        public ActionResult OrderPlans()
        {
            return View();
        }
        public ActionResult Order()
        {
            return View();
        }

       

        public JsonResult GetTodayPlanAndSortedTotals()
        {
            var plannedTotals = _dbConnection.GetTodayPlanTotals();
            var sortedTotal = _dbConnection.GetTotalToySortedToday();

            int totalPlanned = plannedTotals?.Sum(x => x.Total_Planned) ?? 0;
            int totalSorted = sortedTotal?.TotalToySortedToday ?? 0;

            return Json(new
            {
                planned = totalPlanned,
                sorted = totalSorted,
                pending = Math.Max(totalPlanned - totalSorted, 0) // supaya nggak minus
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetYesterdayAndTodayPlanAndSortedTotals()
        {
            // Ambil data perencanaan dan sorted untuk hari ini
            var plannedTotalsToday = _dbConnection.GetTodayPlanTotals();
            var sortedTotalToday = _dbConnection.GetTotalToySortedToday();

            // Ambil data perencanaan dan sorted untuk kemarin
            var plannedTotalsYesterday = _dbConnection.GetYesterdayAndTodayPlanTotals();
            var sortedTotalYesterday = _dbConnection.GetTotalToySortedCategories();

            // Hitung total untuk hari ini
            int totalPlannedToday = plannedTotalsToday?.Sum(x => x.Total_Planned) ?? 0;
            int totalSortedToday = sortedTotalToday?.TotalToySortedToday ?? 0;

            // Hitung total untuk kemarin
            int totalPlannedYesterday = plannedTotalsYesterday
                ?.Where(x => x.PlanDateCategory == "Yesterday")
                .Sum(x => x.Total_Planned) ?? 0;

            int totalSortedYesterday = sortedTotalYesterday
                ?.Where(x => x.PlanDateCategory == "Yesterday")
                .Sum(x => x.TotalSorted) ?? 0;

            // Hitung pending untuk hari ini dan kemarin
            int pendingToday = Math.Max(totalPlannedToday - totalSortedToday, 0);
            int pendingYesterday = Math.Max(totalPlannedYesterday - totalSortedYesterday, 0);

            // Hitung perbedaan pending dalam persentase
            double pendingDifferencePercentage = 0;
            string pendingStatus = "";

            if (pendingYesterday > 0)
            {
                pendingDifferencePercentage = Math.Abs((double)(pendingToday - pendingYesterday) / pendingYesterday) * 100;
                pendingStatus = pendingToday < pendingYesterday ? "Increase" : "Decrease";
            }
            else
            {
                if (pendingToday > 0)
                {
                    pendingDifferencePercentage = 100;
                    pendingStatus = "Increase";
                }
                else if (pendingToday == 0)
                {
                    pendingDifferencePercentage = 0;
                    pendingStatus = "No Change";
                }
                else
                {
                    pendingDifferencePercentage = 0;
                    pendingStatus = "No Data";
                }
            }


            // Kembalikan response dalam bentuk JSON
            return Json(new
            {
                today = new
                {
                    planned = totalPlannedToday,
                    sorted = totalSortedToday,
                    pending = pendingToday
                },
                yesterday = new
                {
                    planned = totalPlannedYesterday,
                    sorted = totalSortedYesterday,
                    pending = pendingYesterday
                },
                comparison = new
                {
                    plannedDifference = totalPlannedToday - totalPlannedYesterday,
                    sortedDifference = totalSortedToday - totalSortedYesterday,
                    pendingDifference = pendingToday - pendingYesterday,
                    pendingPercentageChange = pendingDifferencePercentage,
                    pendingStatus = pendingStatus
                }
            }, JsonRequestBehavior.AllowGet);
        }




        [HttpGet]
        public JsonResult GetYesterdayAndTodayPlanTotalsJson()
        {
            try
            {
                var planTotals = _dbConnection.GetYesterdayAndTodayPlanTotals();

                // Initialize variables for Today's and Yesterday's totals
                decimal todayTotal = 0;
                decimal yesterdayTotal = 0;

                // Check if data for "Today" exists
                var todayData = planTotals.FirstOrDefault(pt => pt.PlanDateCategory == "Today");
                if (todayData != null)
                {
                    todayTotal = todayData.Total_Planned;
                }

                // Check if data for "Yesterday" exists
                var yesterdayData = planTotals.FirstOrDefault(pt => pt.PlanDateCategory == "Yesterday");
                if (yesterdayData != null)
                {
                    yesterdayTotal = yesterdayData.Total_Planned;
                }

                // Calculate percentage change if yesterdayTotal is not zero
                decimal percentageChange = 0;
                string changeDirection = "No Change";

                if (yesterdayTotal == 0)
                {
                    if (todayTotal > 0)
                    {
                        percentageChange = 100; // Or use 999 to show "full increase"
                        changeDirection = "Increased";
                    }
                    else if (todayTotal < 0)
                    {
                        percentageChange = 100;
                        changeDirection = "Decreased";
                    }
                }
                else
                {
                    percentageChange = ((todayTotal - yesterdayTotal) / yesterdayTotal) * 100;

                    if (percentageChange > 0)
                    {
                        changeDirection = "Increased";
                    }
                    else if (percentageChange < 0)
                    {
                        changeDirection = "Decreased";
                    }
                }


                return Json(new
                {
                    success = true,
                    data = planTotals,
                    percentageChange = Math.Abs(percentageChange), // Display absolute value of percentage change
                    changeDirection = changeDirection // Indicates whether the change is increased or decreased
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetWeeklyPlannedTotals()
        {
            var result = _dbConnection.GetWeeklyPlannedTotals(); // Panggil method dari class DB Connection
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetYesterdayAndTodayTotalSortedJson()
        {
            try
            {
                var planTotals = _dbConnection.GetTotalToySortedCategories();

                // Initialize variables for Today's and Yesterday's totals
                decimal todayTotal = 0;
                decimal yesterdayTotal = 0;

                // Check if data for "Today" exists
                var todayData = planTotals.FirstOrDefault(pt => pt.PlanDateCategory == "Today");
                if (todayData != null)
                {
                    todayTotal = todayData.TotalSorted;
                }

                // Check if data for "Yesterday" exists
                var yesterdayData = planTotals.FirstOrDefault(pt => pt.PlanDateCategory == "Yesterday");
                if (yesterdayData != null)
                {
                    yesterdayTotal = yesterdayData.TotalSorted;
                }

                // Calculate percentage change if yesterdayTotal is not zero
                decimal percentageChange = 0;
                string changeDirection = "No Change";

                if (yesterdayTotal == 0)
                {
                    if (todayTotal > 0)
                    {
                        percentageChange = 100; // Or use 999 to show "full increase"
                        changeDirection = "Increased";
                    }
                    else if (todayTotal < 0)
                    {
                        percentageChange = 100;
                        changeDirection = "Decreased";
                    }
                }
                else
                {
                    percentageChange = ((todayTotal - yesterdayTotal) / yesterdayTotal) * 100;

                    if (percentageChange > 0)
                    {
                        changeDirection = "Increased";
                    }
                    else if (percentageChange < 0)
                    {
                        changeDirection = "Decreased";
                    }
                }

                return Json(new
                {
                    success = true,
                    data = planTotals,
                    percentageChange = Math.Abs(percentageChange), // Display absolute value of percentage change
                    changeDirection = changeDirection // Indicates whether the change is increased or decreased
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetWeeklySortedTotals()
        {
            var result = _dbConnection.GetWeeklySortedTotals(); // atau instance dari class DB Anda
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetWeeklyPlannedSortedPending()
        {
            // Ambil data dari database
            var planned = _dbConnection.GetWeeklyPlannedTotals() ?? new List<DailyPlanSummaryWeekly>();
            var sorted = _dbConnection.GetWeeklySortedTotals() ?? new List<SortedSummaryWeekly>();

            // Debug log (opsional, untuk pengecekan)
            Console.WriteLine("=== PLANNED ===");
            foreach (var p in planned)
            {
                Console.WriteLine($"{p.PlanDate.ToShortDateString()} = {p.TotalPlanned}");
            }

            Console.WriteLine("=== SORTED ===");
            foreach (var s in sorted)
            {
                Console.WriteLine($"{s.PlanDate.ToShortDateString()} = {s.TotalSorted}");
            }

            // Merge data dengan join berdasarkan tanggal
            var merged = (from p in planned
                          join s in sorted on p.PlanDate.Date equals s.PlanDate.Date into ps
                          from s in ps.DefaultIfEmpty()
                          select new WeeklyPlanSortedPendingModel
                          {
                              PlanDate = p.PlanDate.Date, // pastikan hanya tanggal
                              TotalPlanned = p.TotalPlanned,
                              TotalSorted = s?.TotalSorted ?? 0,
                              TotalPending = p.TotalPlanned - (s?.TotalSorted ?? 0)
                          }).ToList();

            return Json(merged, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSchedules(int page = 1, int pageSize = 10)
        {
            int totalRecords;
            var schedules = _dbConnection.GetSchedules(page, pageSize, out totalRecords);

            return Json(new
            {
                data = schedules,
                total = totalRecords
            }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetDailyPlanSummaries()
        {
            try
            {
                var summaries = _dbConnection.GetDailyPlanSummaries();
                return Json(new
                {
                    success = true,
                    data = summaries
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


        [HttpPost]
        public JsonResult InsertDailySchedule(ScheduleDailyPlan schedule)
        {
            try
            {
                if (schedule == null)
                {
                    return Json(new { success = false, message = "Invalid request data." });
                }

                bool isInserted = _dbConnection.InsertDailySchedule(
                    schedule.ScheduleId,
                    schedule.StartDateTime,
                    schedule.EndDateTime,
                    schedule.GateLocation
                );

                if (!isInserted)
                {
                    return Json(new { success = false, message = "Failed to insert schedule detail." });
                }

                return Json(new { success = true });
            }
            catch (SqlException sqlEx)
            {
                return Json(new { success = false, message = "Database Error: " + sqlEx.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Server Error: " + ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetActiveGateResponsibleAndSupervisor()
        {
            var result = _dbConnection.GetActiveGateResponsibleAndSupervisor();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        [HttpGet]
        public JsonResult GetDailyPlanLocationMonitoring()
        {
            try
            {
              
                var data = _dbConnection.GetDailyPlanLoactionMonitoring();
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetDailyPlanShortDropdown()
        {
            try
            {
                var data = _dbConnection.GetDailyPlanShortData(); // Memanggil method dari service atau repository
                return Json(data, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public JsonResult GetToyNumbers()
        {
            var data = _dbConnection.GetToyNumbers();
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetDailyPlanDetailTotalSortedAndTotalPlanned(int dailyPlanDetailId)
        {
            // Memanggil metode yang ada untuk mendapatkan data
            List<DailyPlanDetailTotalSortedAndTotalPlanned> result = _dbConnection.GetDailyPlanDetailTotalSortedAndTotalPlanned(dailyPlanDetailId);

            // Mengembalikan hasil dalam format JSON
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetToySortedByDailyPlanId(int dailyPlanId)
        {
            try
            {
              
                List<ToySorted> sortedToys = _dbConnection.GetToySortedByDailyPlanId(dailyPlanId);

                return Json(new
                {
                    success = true,
                    data = sortedToys
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }


    }
}