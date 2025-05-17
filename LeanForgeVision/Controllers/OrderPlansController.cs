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
    public class OrderPlansController : Controller
    {
        private DbConnection _dbConnection = new DbConnection();
        // GET: OrderPlans

        [HttpPost]
        public JsonResult InsertSchedules(ScheduleRequestModel model)
        {
            try
            {
                // Buat model yang akan dikirim ke DB Helper
                var schedule = new ScheduleModel
                {
                    Planner_ID = model.Planner_ID,
                    Total_Planned = model.Total_Planned,
                    Gate_Responsible_ID = model.Gate_Responsible_ID,
                    Supervisor_ID = model.Supervisor_ID
                };

                // Kirim ke DB Helper dan dapatkan list ID yang berhasil di-insert
                var insertedIds = _dbConnection.InsertSchedules(schedule, model.Toy_Numbers);



                return Json(new { success = true, insertedIds });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        public JsonResult InsertToySortedManualData(int dailyPlanId, string toyNumber, int index)
        {
            var model = new ToySorted
            {
                Toy_Number = toyNumber,
                Sorted_At = DateTime.Now, // waktu dasar
                Sorting_Method = 7, // Asumsi metode sorting default
                Daily_Plan_ID = dailyPlanId
            };

            // Menambahkan delay untuk memastikan bahwa setiap insert memiliki waktu yang unik
            bool success = _dbConnection.InsertToySortedManual(model, index * 100); // delay meningkat dengan index

            return Json(new
            {
                success = success,
                message = success ? "Data berhasil disimpan." : "Gagal menyimpan data."
            });
        }











    }
}