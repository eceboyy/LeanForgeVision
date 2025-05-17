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
    public class AuditController : Controller
    {
        private DbConnection _dbConnection = new DbConnection();

        [HttpPost]
        public JsonResult InsertAuditLog(AuditLogModel log)
        {
            try
            {
                if (log == null)
                {
                    return Json(new { success = false, message = "Invalid request data." });
                }

                // Menambahkan audit log setelah jadwal dimasukkan
                _dbConnection.InsertAuditLog(log); // Memanggil InsertAuditLog dengan objek log

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


    }
}