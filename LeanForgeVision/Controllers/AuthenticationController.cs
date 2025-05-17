using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Razor.Tokenizer;
using LeanForgeVision.Database;
using LeanForgeVision.Models;
using Newtonsoft.Json;

namespace LeanForgeVision.Controllers
{
    public class AuthenticationController : Controller
    {
        private DbConnection _dbConnection = new DbConnection();
        // GET: Authentication
        public ActionResult Signin()
        {
            return View();
        }
        public ActionResult Signup()
        {
            return View();
        }
        public ActionResult ResetPw()
        {
            return View();
        }

        [HttpPost]
        public JsonResult ValidateUser()
        {
            try
            {
                // 🔍 Step 1: Baca Request Body
                using (var reader = new StreamReader(Request.InputStream))
                {
                    string requestBody = reader.ReadToEnd();
                    Console.WriteLine($"[DEBUG] Request Body: {requestBody}");
                    Debug.WriteLine($"[DEBUG] Request Body: {requestBody}");

                    dynamic data = JsonConvert.DeserializeObject(requestBody);

                    // 🔍 Step 2: Ambil Employee_ID dan Password dari request
                    string employeeId = data.Employee_ID;
                    string password = data.Password;
                    Console.WriteLine($"[DEBUG] Received Employee_ID: {employeeId}, Password: {password}");
                    Debug.WriteLine($"[DEBUG] Received Employee_ID: {employeeId}, Password: {password}");

                    // 🔍 Step 3: Validasi user di database
                    bool isValid = _dbConnection.CheckUserInDatabase(employeeId, password);
                    Console.WriteLine($"[DEBUG] CheckUserInDatabase Result: {isValid}");
                    Debug.WriteLine($"[DEBUG] CheckUserInDatabase Result: {isValid}");

                    if (isValid)
                    {
                        // 🔍 Step 4: Ambil Name dan Email dari Employee_Master
                        var employeeData = _dbConnection.GetEmployeeById(employeeId);
                        if (employeeData.HasValue)
                        {
                            string username = employeeData.Value.Name;
                            string email = employeeData.Value.Email;
                            Console.WriteLine($"[DEBUG] Employee Found: Name = {username}, Email = {email}");
                            Debug.WriteLine($"[DEBUG] Employee Found: Name = {username}, Email = {email}");

                            // 🔍 Step 5: Insert user ke tabel User jika belum ada
                            _dbConnection.InsertUserIfNotExists(employeeId, username, email);
                            Console.WriteLine($"[DEBUG] User Inserted: {employeeId}");
                            Debug.WriteLine($"[DEBUG] User Inserted: {employeeId}");
                        }
                        else
                        {
                            Console.WriteLine($"[WARNING] Employee ID {employeeId} not found in Employee_Master.");
                            Debug.WriteLine($"[WARNING] Employee ID {employeeId} not found in Employee_Master.");
                        }

                        // 🔍 Step 6: Return success response
                        Console.WriteLine("[DEBUG] Login Success");
                        Debug.WriteLine("[DEBUG] Login Success");
                        return Json(new { success = true });
                    }
                    else
                    {
                        // 🔍 Step 7: Jika tidak valid, return false
                        Console.WriteLine("[DEBUG] Login Failed - Invalid ID or Password");
                        Debug.WriteLine("[DEBUG] Login Failed - Invalid ID or Password");
                        return Json(new { success = false });
                    }
                }
            }
            catch (Exception ex)
            {
                // 🔍 Step 8: Tangkap error dan tampilkan pesan
                Console.WriteLine($"[ERROR] Exception: {ex.Message}");
                Debug.WriteLine($"[ERROR] Exception: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }




    }
}