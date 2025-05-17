using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Configuration;
using LeanForgeVision.Models;
using System.Diagnostics;

namespace LeanForgeVision.Database
{
    public class DbConnection
    {
        private string dbLeanForge = ConfigurationManager.ConnectionStrings["dbLeanForge"].ConnectionString;

        public List<EmployeeModel> GetEmployeesMasterDB()
        {
            List<EmployeeModel> employees = new List<EmployeeModel>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = "SELECT [Employee_ID], [Name], [Email], [Birth_Date] FROM [LeanForgeVision].[dbo].[Employee_Master]";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            employees.Add(new EmployeeModel
                            {
                                Employee_ID = reader.GetString(0), // Sebelumnya GetInt32, sekarang GetString
                                Name = reader.GetString(1),
                                Email = reader.GetString(2),
                                Birth_Date = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3) // Cek NULL sebelum membaca
                            });
                        }
                    }
                }
            }
            return employees;
        }

        public bool CheckUserInDatabase(string employeeId, string password)
        {
            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = "SELECT COUNT(*) FROM Employee_Master WHERE Employee_ID = @Employee_ID AND FORMAT(Birth_Date, 'yyyyMMdd') = @Password";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Employee_ID", employeeId);
                    cmd.Parameters.AddWithValue("@Password", password);

                    int count = (int)cmd.ExecuteScalar();
                    return count > 0;
                }
            }
        }


        public (string Name, string Email)? GetEmployeeById(string employeeId)
        {
            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = "SELECT Name, Email FROM Employee_Master WHERE Employee_ID = @Employee_ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Employee_ID", employeeId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string name = reader.GetString(0);
                            string email = reader.IsDBNull(1) ? "default@example.com" : reader.GetString(1);
                            return (name, email);
                        }
                    }
                }
            }
            return null;
        }

        public void InsertUserIfNotExists(string employeeId, string username, string email)
        {
            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string checkUserQuery = "SELECT COUNT(*) FROM [LeanForgeVision].[dbo].[User] WHERE User_ID = @User_ID";
                using (SqlCommand checkCmd = new SqlCommand(checkUserQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@User_ID", employeeId);
                    int userExists = (int)checkCmd.ExecuteScalar();

                    if (userExists == 0) // Jika belum ada, lakukan INSERT
                    {
                        string insertQuery = @"
                        INSERT INTO [LeanForgeVision].[dbo].[User] 
                        ([User_ID], [Username], [Email], [Created_Date], [Role_ID]) 
                        VALUES (@User_ID, @Username, @Email, @Created_Date, @Role_ID)";

                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, conn))
                        {
                            insertCmd.Parameters.AddWithValue("@User_ID", employeeId);
                            insertCmd.Parameters.AddWithValue("@Username", username);
                            insertCmd.Parameters.AddWithValue("@Email", email);
                            insertCmd.Parameters.AddWithValue("@Created_Date", DateTime.Now);
                            insertCmd.Parameters.AddWithValue("@Role_ID", 1); // Role default 1

                            insertCmd.ExecuteNonQuery();
                        }
                    }
                }
            }
        }
        public List<UserModel> GetAllUsersWithRoleName()
        {
            var users = new List<UserModel>();
            var roles = GetAllUserRoles(); // Ambil semua role terlebih dahulu

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                string query = "SELECT [Username], [Email], [Created_Date], [Role_ID], [User_ID] FROM [LeanForgeVision].[dbo].[User]";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var roleId = Convert.ToInt32(reader["Role_ID"]);
                        var roleName = roles.FirstOrDefault(r => r.Role_ID == roleId)?.Role_Name ?? "Unknown";

                        users.Add(new UserModel
                        {
                            Username = reader["Username"].ToString(),
                            Email = reader["Email"].ToString(),
                            Created_Date = Convert.ToDateTime(reader["Created_Date"]),
                            Role_ID = roleId,
                            RoleName = roleName,
                            User_ID = reader["User_ID"].ToString()
                        });
                    }
                }
            }

            return users;
        }

        public List<UserRoleModel> GetAllUserRoles()
        {
            var roles = new List<UserRoleModel>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                string query = "SELECT [Role_ID], [Role_Name], [Role_Desc] FROM [LeanForgeVision].[dbo].[User_Role]";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        roles.Add(new UserRoleModel
                        {
                            Role_ID = Convert.ToInt32(reader["Role_ID"]),
                            Role_Name = reader["Role_Name"].ToString(),
                            Role_Desc = reader["Role_Desc"].ToString()
                        });
                    }
                }
            }

            return roles;
        }




        public List<int> InsertSchedules(ScheduleModel schedule, List<ToyNumberPlannedModel> toyNumbers)

        {
            List<int> insertedDetailIds = new List<int>();

            try
            {
                using (SqlConnection conn = new SqlConnection(dbLeanForge))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        // 1. Insert ke DailyPlan_Head
                        int headId;
                        string insertHeadQuery = @"
                    INSERT INTO [LeanForgeVision].[dbo].[DailyPlan_Head]
                    ([DailyPlanHead_CreatedAt], [DailyPlanHead_TotalPlanned], [DailyPlanHead_PlannerID],
                     [DailyPlanHead_GateResponsibleID], [DailyPlanHead_SupervisorID], [DailyPlanHead_StatusID])
                    VALUES
                    (@CreatedAt, @TotalPlanned, @PlannerID, @GateResponsibleID, @SupervisorID, @StatusID);
                    SELECT SCOPE_IDENTITY();";

                        using (SqlCommand cmdHead = new SqlCommand(insertHeadQuery, conn, transaction))
                        {
                            cmdHead.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                            cmdHead.Parameters.AddWithValue("@TotalPlanned", (object)schedule.Total_Planned ?? DBNull.Value);
                            cmdHead.Parameters.AddWithValue("@PlannerID", (object)schedule.Planner_ID ?? DBNull.Value);
                            cmdHead.Parameters.AddWithValue("@GateResponsibleID", (object)schedule.Gate_Responsible_ID ?? DBNull.Value);
                            cmdHead.Parameters.AddWithValue("@SupervisorID", (object)schedule.Supervisor_ID ?? DBNull.Value);
                            cmdHead.Parameters.AddWithValue("@StatusID", 4); // Default status

                            headId = Convert.ToInt32(cmdHead.ExecuteScalar());
                        }

                        // 2. Insert ke DailyPlan_Detail untuk setiap ToyNumber
                        string insertDetailQuery = @"
                        INSERT INTO [LeanForgeVision].[dbo].[DailyPlan_Detail]
                        ([DailyPlanHead_ID], [DailyPlanDetail_ToyNumber], [DailyPlanDetail_TotalPlanned], [DailyPlanDetail_StatusID])
                        VALUES (@HeadID, @ToyNumber, @TotalPlanned, 4);
                        SELECT SCOPE_IDENTITY();";

                        foreach (var toy in toyNumbers)
                        {
                            using (SqlCommand cmdDetail = new SqlCommand(insertDetailQuery, conn, transaction))
                            {
                                cmdDetail.Parameters.AddWithValue("@HeadID", headId);
                                cmdDetail.Parameters.AddWithValue("@ToyNumber", (object)toy.ToyNumber ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@TotalPlanned", (object)toy.Planned ?? DBNull.Value);

                                int detailId = Convert.ToInt32(cmdDetail.ExecuteScalar());
                                insertedDetailIds.Add(detailId);
                            }
                        }



                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Insert Error: " + ex.Message);
                throw;
            }

            return insertedDetailIds;
        }

        public void InsertToySortedAutomated(string toyNumber, DateTime sortedAt, int sortingMethod, int dailyPlanId)
        {
            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
            BEGIN TRY
                INSERT INTO [LeanForgeVision].[dbo].[Toy_Sorted] (Toy_Number, Sorted_At, Sorting_Method, Daily_Plan_ID) 
                VALUES (@ToyNumber, @SortedAt, @SortingMethod, @DailyPlanId);
            END TRY
            BEGIN CATCH
                IF ERROR_NUMBER() = 2627 -- Error karena UNIQUE CONSTRAINT
                BEGIN
                    PRINT '⚠️ Data Duplicate, Data cant insert to database';
                END
                ELSE
                BEGIN
                    THROW; -- Jika error lain, tetap lempar error
                END
            END CATCH";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ToyNumber", toyNumber);
                    cmd.Parameters.AddWithValue("@SortedAt", sortedAt);
                    cmd.Parameters.AddWithValue("@SortingMethod", sortingMethod);
                    cmd.Parameters.AddWithValue("@DailyPlanId", dailyPlanId);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public bool InsertToySortedManual(ToySorted model, int delayMilliseconds = 100)
        {
            // Pastikan Sorted_At tidak null sebelum menambah millisecond
            if (model.Sorted_At.HasValue)
            {
                var delayedSortedAt = model.Sorted_At.Value.AddMilliseconds(delayMilliseconds);

                string query = @"
        INSERT INTO LeanForgeVision.dbo.Toy_Sorted 
        (Toy_Number, Sorted_At, Sorting_Method, Daily_Plan_ID)
        VALUES (@Toy_Number, @Sorted_At, @Sorting_Method, @Daily_Plan_ID)";

                using (SqlConnection conn = new SqlConnection(dbLeanForge))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Toy_Number", model.Toy_Number);
                    cmd.Parameters.AddWithValue("@Sorted_At", delayedSortedAt);
                    cmd.Parameters.AddWithValue("@Sorting_Method", model.Sorting_Method);
                    cmd.Parameters.AddWithValue("@Daily_Plan_ID", model.Daily_Plan_ID);

                    conn.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();
                    return rowsAffected > 0;
                }
            }
            else
            {
                // Tangani jika Sorted_At adalah null
                throw new ArgumentException("Sorted_At cannot be null.");
            }
        }




        public List<ScheduleModel> GetSchedules(int page, int pageSize, out int totalRecords)
        {
            var schedules = new List<ScheduleModel>();
            var employees = GetEmployeesMasterDB();
            var statuses = GetLogStatuses();
            var combinedList = new List<ScheduleModel>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                // Ambil semua HEAD (bisa ditambahkan filter tertentu kalau mau efisien)
                string headQuery = @"
                SELECT 
                    DailyPlanHead_ID,
                    DailyPlanHead_CreatedAt,
                    DailyPlanHead_TotalPlanned,
                    DailyPlanHead_PlannerID,
                    DailyPlanHead_GateResponsibleID,
                    DailyPlanHead_SupervisorID,
                    DailyPlanHead_StatusID
                FROM DailyPlan_Head
                ORDER BY DailyPlanHead_CreatedAt DESC";

                using (SqlCommand cmd = new SqlCommand(headQuery, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int headId = reader.GetInt32(0);
                            DateTime createdAt = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);
                            int totalPlanned = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            string plannerId = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            string gateId = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            string supervisorId = reader.IsDBNull(5) ? "" : reader.GetString(5);
                            int statusId = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);

                            // Ambil detail
                            var details = GetDetailsByHeadId(headId, conn, transaction: null);

                            foreach (var detail in details)
                            {
                                combinedList.Add(new ScheduleModel
                                {
                                    Schedule_ID = headId,
                                    Schedule_Detail_ID = detail.DetailId,
                                    Created_At = createdAt,
                                    Total_Planned = totalPlanned,
                                    Planner_ID = plannerId,
                                    Planner_Name = employees.FirstOrDefault(e => e.Employee_ID == plannerId)?.Name ?? "No Name",
                                    Gate_Responsible_ID = gateId,
                                    Gate_Responsible_Name = employees.FirstOrDefault(e => e.Employee_ID == gateId)?.Name ?? "No Name",
                                    Supervisor_ID = supervisorId,
                                    Supervisor_Name = employees.FirstOrDefault(e => e.Employee_ID == supervisorId)?.Name ?? "No Name",
                                    Status_ID = statusId,
                                    Status_Desc = statuses.FirstOrDefault(s => s.Status_ID == statusId)?.Status_Desc ?? "No Description",
                                    Toy_Number = detail.ToyNumber,
                                    Toy_TotalPlanned = detail.TotalPlanned,
                                    Detail_Status_ID = detail.DetailStatusId,
                                    Detail_Status_Desc = statuses.FirstOrDefault(s => s.Status_ID == detail.DetailStatusId)?.Status_Desc ?? "No Description"
                                });
                            }

                        }
                    }
                }
            }

            totalRecords = combinedList.Count;

            // Apply pagination di akhir
            return combinedList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }


        private List<(int DetailId, string ToyNumber, int TotalPlanned, int DetailStatusId)> GetDetailsByHeadId(int headId, SqlConnection conn, SqlTransaction transaction)
        {
            var details = new List<(int DetailId, string ToyNumber, int TotalPlanned, int DetailStatusId)>();

            string query = @"
            SELECT DailyPlanDetail_ID, DailyPlanDetail_ToyNumber, DailyPlanDetail_TotalPlanned, DailyPlanDetail_StatusID
            FROM DailyPlan_Detail
            WHERE DailyPlanHead_ID = @HeadId";

            using (SqlCommand cmd = new SqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@HeadId", headId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int detailId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        string toyNumber = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        int totalPlanned = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                        int detailStatusId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                        details.Add((detailId, toyNumber, totalPlanned, detailStatusId));
                    }
                }
            }

            return details;
        }



        public bool InsertDailySchedule(int dailyPlanId, DateTime startDateTime, DateTime endDateTime, string gateId)
        {
            bool isInserted = false;

            try
            {
                using (SqlConnection conn = new SqlConnection(dbLeanForge))
                {
                    string query = @"
                INSERT INTO [LeanForgeVision].[dbo].[Daily_Plan_Schedule]
                ([Daily_Plan_ID], [Start_Date], [Finish_Date], [Gate_ID])
                VALUES (@Daily_Plan_ID, @Start_Date, @Finish_Date, @Gate_ID);";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Daily_Plan_ID", dailyPlanId);
                        cmd.Parameters.AddWithValue("@Start_Date", startDateTime);
                        cmd.Parameters.AddWithValue("@Finish_Date", endDateTime);
                        cmd.Parameters.AddWithValue("@Gate_ID", gateId);

                        conn.Open();
                        int rows = cmd.ExecuteNonQuery();
                        isInserted = rows > 0;
                        conn.Close();
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                Debug.Print("SQL Error: " + sqlEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                Debug.Print("General Error: " + ex.Message);
                throw;
            }

            return isInserted;
        }

        public List<PlanScheduleModelComparisonWithMQTT> GetPlanSchedules()
        {
            var planSchedules = new List<PlanScheduleModelComparisonWithMQTT>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = @"
                SELECT 
                    dps.Daily_Plan_ID,
                    dps.Start_Date,
                    dps.Finish_Date,
                    dps.Gate_ID,
                    dpm.DailyPlanDetail_ToyNumber
                FROM 
                    LeanForgeVision.dbo.Daily_Plan_Schedule dps
                INNER JOIN 
                    LeanForgeVision.dbo.DailyPlan_Detail dpm
                ON dps.Daily_Plan_ID = dpm.DailyPlanDetail_ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var schedule = new PlanScheduleModelComparisonWithMQTT
                        {
                            Daily_Plan_ID = reader.GetInt32(0),
                            Start_Date = reader.GetDateTime(1),
                            Finish_Date = reader.GetDateTime(2),
                            Gate_ID = reader.IsDBNull(3) ? "" : reader.GetInt32(3).ToString(),
                            Toy_Number = reader.IsDBNull(4) ? "" : reader.GetString(4)
                        };


                        planSchedules.Add(schedule);
                    }
                }
            }

            return planSchedules;
        }

        public Dictionary<int, int> GetToySortedCounts(string toyNumber)
        {
            var counts = new Dictionary<int, int>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
        SELECT Daily_Plan_ID, COUNT(*) AS Total
        FROM LeanForgeVision.dbo.Toy_Sorted
        WHERE Toy_Number = @ToyNumber
        GROUP BY Daily_Plan_ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ToyNumber", toyNumber);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int dailyPlanId = reader.GetInt32(0);
                            int total = reader.GetInt32(1);
                            counts[dailyPlanId] = total;
                        }
                    }
                }
            }

            return counts;
        }

        public List<(int DailyPlanId, int TotalPlanned, int TotalSorted)> GetPlannedAndSortedCounts(string toyNumber)
        {
            var result = new List<(int DailyPlanId, int TotalPlanned, int TotalSorted)>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
            SELECT 
                D.DailyPlanDetail_ID AS DailyPlanId,
                D.DailyPlanDetail_TotalPlanned AS TotalPlanned,
                (
                    SELECT COUNT(*) 
                    FROM LeanForgeVision.dbo.Toy_Sorted TS
                    WHERE TS.Daily_Plan_ID = D.DailyPlanDetail_ID
                ) AS TotalSorted
            FROM LeanForgeVision.dbo.DailyPlan_Detail D
            INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule S 
                ON D.DailyPlanDetail_ID = S.Daily_Plan_ID
            WHERE 
                D.DailyPlanDetail_ToyNumber = @ToyNumber
                AND GETDATE() BETWEEN S.Start_Date AND S.Finish_Date
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ToyNumber", toyNumber);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int dailyPlanId = reader.GetInt32(0);
                            int totalPlanned = reader.GetInt32(1);
                            int totalSorted = reader.GetInt32(2);

                            result.Add((dailyPlanId, totalPlanned, totalSorted));
                        }
                    }
                }
            }

            return result;
        }

        public List<DailyPlanDetailTotalSortedAndTotalPlanned> GetDailyPlanDetailTotalSortedAndTotalPlanned(int dailyPlanDetailId)
        {
            var result = new List<DailyPlanDetailTotalSortedAndTotalPlanned>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = @"
                SELECT 
                    D.DailyPlanDetail_ID AS DailyPlanId,
                    D.DailyPlanDetail_TotalPlanned AS TotalPlanned,
                    (
                        SELECT COUNT(*) 
                        FROM LeanForgeVision.dbo.Toy_Sorted TS
                        WHERE TS.Daily_Plan_ID = D.DailyPlanDetail_ID
                    ) AS TotalSorted
                FROM LeanForgeVision.dbo.DailyPlan_Detail D
                INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule S 
                    ON D.DailyPlanDetail_ID = S.Daily_Plan_ID
                WHERE 
                    D.DailyPlanDetail_ID = @DailyPlanDetailID
                    AND GETDATE() BETWEEN S.Start_Date AND S.Finish_Date";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DailyPlanDetailID", dailyPlanDetailId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new DailyPlanDetailTotalSortedAndTotalPlanned
                            {
                                DailyPlanId = reader.GetInt32(0),
                                TotalPlanned = reader.GetInt32(1),
                                TotalSorted = reader.GetInt32(2)
                            });
                        }
                    }
                }
            }

            return result;
        }


        public int UpdateDailyPlanStatusConditional()
        {
            int totalRowsAffected = 0;

            try
            {
                using (SqlConnection conn = new SqlConnection(dbLeanForge))
                {
                    conn.Open();

                    // 🔵 Step 1: Update DailyPlanDetail ke status 1 (IN PROGRESS)
                    string updateDetailStatus1 = @"
                    UPDATE DPD
                    SET DPD.DailyPlanDetail_StatusID = 1
                    FROM LeanForgeVision.dbo.DailyPlan_Detail DPD
                    JOIN LeanForgeVision.dbo.Daily_Plan_Schedule DPS
                        ON DPD.DailyPlanDetail_ID = DPS.Daily_Plan_ID
                    JOIN LeanForgeVision.dbo.Toy_Sorted TS
                        ON DPD.DailyPlanDetail_ID = TS.Daily_Plan_ID
                    WHERE TS.Sorted_At BETWEEN DPS.Start_Date AND DPS.Finish_Date
                      AND DPD.DailyPlanDetail_StatusID != 1
                      AND DPD.DailyPlanDetail_StatusID != 2;";

                    using (SqlCommand cmdDetail1 = new SqlCommand(updateDetailStatus1, conn))
                    {
                        totalRowsAffected += cmdDetail1.ExecuteNonQuery();
                    }

                    // 🟢 Step 2: Update DailyPlanDetail ke status 2 (COMPLETED)
                    string updateDetailStatus2 = @"
                    UPDATE DPD
                    SET DPD.DailyPlanDetail_StatusID = 2
                    FROM LeanForgeVision.dbo.DailyPlan_Detail DPD
                    JOIN LeanForgeVision.dbo.Daily_Plan_Schedule DPS
                        ON DPD.DailyPlanDetail_ID = DPS.Daily_Plan_ID
                    WHERE GETDATE() > DPS.Finish_Date
                      AND DPD.DailyPlanDetail_StatusID = 1;";

                    using (SqlCommand cmdDetail2 = new SqlCommand(updateDetailStatus2, conn))
                    {
                        totalRowsAffected += cmdDetail2.ExecuteNonQuery();
                    }

                    // 🟡 Step 3: Update DailyPlanHead ke status 2 jika semua DailyPlanDetail sudah 2
                    string updateHeadStatus2 = @"
                    UPDATE DPH
                    SET DPH.DailyPlanHead_StatusID = 2
                    FROM LeanForgeVision.dbo.DailyPlan_Head DPH
                    WHERE DPH.DailyPlanHead_StatusID != 2
                      AND NOT EXISTS (
                          SELECT 1
                          FROM LeanForgeVision.dbo.DailyPlan_Detail DPD
                          WHERE DPD.DailyPlanHead_ID = DPH.DailyPlanHead_ID
                            AND (DPD.DailyPlanDetail_StatusID IS NULL OR DPD.DailyPlanDetail_StatusID != 2)
                    );";

                    using (SqlCommand cmdHead2 = new SqlCommand(updateHeadStatus2, conn))
                    {
                        totalRowsAffected += cmdHead2.ExecuteNonQuery();
                    }

                    // 🟣 Step 4: Update Gate Status hanya jika perlu
                    // 🔍 Cek dulu apakah ada Gate yang status-nya perlu diupdate
                    string checkGateStatus = @"
                    SELECT COUNT(*)
                    FROM LeanForgeVision.dbo.Gate G
                    LEFT JOIN LeanForgeVision.dbo.Daily_Plan_Schedule DPS
                        ON G.Gate_ID = DPS.Gate_ID
                        AND GETDATE() BETWEEN DPS.Start_Date AND DPS.Finish_Date
                    WHERE 
                        (G.Gate_Status = 5 AND DPS.Gate_ID IS NULL) -- Sudah aktif, harusnya tidak aktif
                        OR
                        (G.Gate_Status = 6 AND DPS.Gate_ID IS NOT NULL); -- Sudah tidak aktif, harusnya aktif
                    ";

                    using (SqlCommand cmdCheckGate = new SqlCommand(checkGateStatus, conn))
                    {
                        int gateNeedUpdate = (int)cmdCheckGate.ExecuteScalar();

                        if (gateNeedUpdate > 0)
                        {
                            // 🛠️ Kalau ada Gate yang perlu diupdate, baru jalankan update
                            string updateGateStatus = @"
                            UPDATE G
                            SET G.Gate_Status = CASE
                                WHEN EXISTS (
                                    SELECT 1
                                    FROM LeanForgeVision.dbo.Daily_Plan_Schedule DPS
                                    WHERE G.Gate_ID = DPS.Gate_ID
                                      AND GETDATE() BETWEEN DPS.Start_Date AND DPS.Finish_Date
                                ) THEN 5
                                ELSE 6
                            END
                            FROM LeanForgeVision.dbo.Gate G;";

                            using (SqlCommand cmdGateUpdate = new SqlCommand(updateGateStatus, conn))
                            {
                               
                                totalRowsAffected += cmdGateUpdate.ExecuteNonQuery();
                            }

                            Debug.Print("[DEBUG] Gate status updated.");
                        }
                        else
                        {
                            Debug.Print("[DEBUG] No gate status change needed.");
                        }
                    }


                    conn.Close();
                }
            }
            catch (SqlException sqlEx)
            {
                Debug.Print("[AUTO][ERROR] SQL Error: " + sqlEx.Message);
                throw;
            }
            catch (Exception ex)
            {
                Debug.Print("[AUTO][ERROR] General Error: " + ex.Message);
                throw;
            }

            Debug.Print($"[DEBUG] Total records updated: {totalRowsAffected}");
            return totalRowsAffected;
        }

        public List<DailyPlanDetailCompletedAndStatusOnProgress> GetCompletedDailyPlanDetails()
        {
            var result = new List<DailyPlanDetailCompletedAndStatusOnProgress>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
        SELECT 
            D.DailyPlanDetail_ID AS DailyPlanId,
            D.DailyPlanDetail_TotalPlanned AS TotalPlanned,
            (
                SELECT COUNT(*) 
                FROM LeanForgeVision.dbo.Toy_Sorted TS
                WHERE TS.Daily_Plan_ID = D.DailyPlanDetail_ID
            ) AS TotalSorted,
            D.DailyPlanDetail_StatusID
        FROM LeanForgeVision.dbo.DailyPlan_Detail D
        INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule S 
            ON D.DailyPlanDetail_ID = S.Daily_Plan_ID
        WHERE 
            D.DailyPlanDetail_StatusID = 1 AND
            D.DailyPlanDetail_TotalPlanned = (
                SELECT COUNT(*) 
                FROM LeanForgeVision.dbo.Toy_Sorted TS
                WHERE TS.Daily_Plan_ID = D.DailyPlanDetail_ID
            )";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(new DailyPlanDetailCompletedAndStatusOnProgress
                            {
                                DailyPlanId = reader.GetInt32(0),
                                TotalPlanned = reader.GetInt32(1),
                                TotalSorted = reader.GetInt32(2),
                                StatusID = reader.GetInt32(3)
                            });
                        }
                    }
                }
            }

            return result;
        }

        public int UpdateDailyPlanStatusConditionalForCompletedSorted()
        {
            int updatedCount = 0;

            var completedPlans = GetCompletedDailyPlanDetails();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                foreach (var plan in completedPlans)
                {
                    string updateQuery = @"
                UPDATE LeanForgeVision.dbo.DailyPlan_Detail
                SET DailyPlanDetail_StatusID = 2
                WHERE DailyPlanDetail_ID = @DailyPlanDetailId";

                    using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@DailyPlanDetailId", plan.DailyPlanId);
                        updatedCount += cmd.ExecuteNonQuery(); // jumlah baris berhasil diupdate
                    }
                }
            }

            return updatedCount;
        }




        public List<DailyPlanSummaryModel> GetDailyPlanSummaries()
        {
            List<DailyPlanSummaryModel> list = new List<DailyPlanSummaryModel>();
            var employees = GetEmployeesMasterDB(); // Ambil data karyawan
            var statuses = GetLogStatuses();        // Ambil data status

            try
            {
                using (SqlConnection conn = new SqlConnection(dbLeanForge))
                {
                    string query = @"
                    SELECT 
                        H.DailyPlanHead_ID,
                        D.DailyPlanDetail_ID,
                        SDetail.Start_Date,
                        SDetail.Finish_Date,
                        D.DailyPlanDetail_ToyNumber,
                        SHead.Gate_ID,
                        D.DailyPlanDetail_TotalPlanned,
                        D.DailyPlanDetail_StatusID,
                        (
                            SELECT COUNT(*) 
                            FROM LeanForgeVision.dbo.Toy_Sorted TS
                            WHERE TS.Daily_Plan_ID = D.DailyPlanDetail_ID
                        ) AS Total_Sorted,
                        H.DailyPlanHead_GateResponsibleID,
                        H.DailyPlanHead_SupervisorID,
                        H.DailyPlanHead_StatusID
                    FROM LeanForgeVision.dbo.DailyPlan_Head H
                    LEFT JOIN LeanForgeVision.dbo.DailyPlan_Detail D
                        ON H.DailyPlanHead_ID = D.DailyPlanHead_ID
                    INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule SDetail
                        ON D.DailyPlanDetail_ID = SDetail.Daily_Plan_ID
                    INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule SHead
                        ON D.DailyPlanDetail_ID = SHead.Daily_Plan_ID
                    ORDER BY D.DailyPlanDetail_ID ASC";


                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        conn.Open();
                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            int dailyPlanHeadId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                            int dailyPlanDetailId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                            DateTime startDate = reader.IsDBNull(2) ? DateTime.MinValue : reader.GetDateTime(2);
                            DateTime finishDate = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
                            string toyNumber = reader.IsDBNull(4) ? "" : reader.GetString(4);
                            int gateId = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);
                            int totalPlanned = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);
                            int detailStatusId = reader.IsDBNull(7) ? 0 : reader.GetInt32(7); 
                            int totalSorted = reader.IsDBNull(8) ? 0 : reader.GetInt32(8);
                            string gateResponsibleId = reader.IsDBNull(9) ? "" : reader.GetString(9);
                            string supervisorId = reader.IsDBNull(10) ? "" : reader.GetString(10);
                            int headStatusId = reader.IsDBNull(11) ? 0 : reader.GetInt32(11);
                            string gateResponsibleName = employees.FirstOrDefault(e => e.Employee_ID == gateResponsibleId)?.Name ?? "No Name";
                            string supervisorName = employees.FirstOrDefault(e => e.Employee_ID == supervisorId)?.Name ?? "No Name";
                            string statusDesc = statuses.FirstOrDefault(s => s.Status_ID == headStatusId)?.Status_Desc ?? "Unknown Status";
                            string statusDetailDesc = statuses.FirstOrDefault(s => s.Status_ID == detailStatusId)?.Status_Desc ?? "Unknown Status";

                            list.Add(new DailyPlanSummaryModel
                            {
                                Daily_Plan_ID = dailyPlanHeadId,
                                Schedule_Detail_ID = dailyPlanDetailId,
                                Start_Date = startDate,
                                Finish_Date = finishDate,
                                Toy_Number = toyNumber,
                                Gate_ID = gateId,
                                Total_Planned = totalPlanned,
                                Total_Sorted = totalSorted,
                                Gate_Responsible_ID = gateResponsibleId,
                                Gate_Responsible_Name = gateResponsibleName,
                                Supervisor_ID = supervisorId,
                                Supervisor_Name = supervisorName,
                                Status_ID = headStatusId,
                                Status_Desc = statusDesc,
                                Detail_Status_ID = detailStatusId,
                                Detail_Status_Desc=statusDetailDesc
                            });
                        }


                        conn.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Error retrieving summary data: " + ex.Message);
                throw;
            }

            return list;
        }

        public List<DailyPlannedTotal> GetTodayPlanTotals()
        {
            var planTotals = new List<DailyPlannedTotal>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
                DECLARE @Today DATE = CAST(GETDATE() AS DATE);

                WITH ScheduleToday AS (
                    SELECT 
                        dps.Daily_Plan_ID
                    FROM 
                        LeanForgeVision.dbo.Daily_Plan_Schedule dps
                     WHERE 
                    @Today BETWEEN CAST(dps.Start_Date AS DATE) AND CAST(dps.Finish_Date AS DATE)
                )

                SELECT 
                    dpd.DailyPlanDetail_ID,
                    SUM(dpd.DailyPlanDetail_TotalPlanned) AS Total_Planned
                FROM 
                    LeanForgeVision.dbo.DailyPlan_Detail dpd
                JOIN 
                    ScheduleToday st
                    ON dpd.DailyPlanDetail_ID = st.Daily_Plan_ID
                GROUP BY 
                    dpd.DailyPlanDetail_ID
                ORDER BY 
                    dpd.DailyPlanDetail_ID;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var planTotal = new DailyPlannedTotal
                            {
                                DailyPlanDetail_ID = reader.GetInt32(0),
                                Total_Planned = reader.GetInt32(1)
                            };
                            planTotals.Add(planTotal);
                        }
                    }
                }
            }

            return planTotals;
        }
        public List<DailyPlannedTotalYesterdayToday> GetYesterdayAndTodayPlanTotals()
        {
            var planTotals = new List<DailyPlannedTotalYesterdayToday>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
                DECLARE @Today DATE = CAST(GETDATE() AS DATE);
                DECLARE @Yesterday DATE = DATEADD(DAY, -1, @Today);

                WITH DateCategories AS (
                    SELECT 'Today' AS PlanDateCategory
                    UNION
                    SELECT 'Yesterday'
                ),
                ExpandedPlanDates AS (
                    -- Expand setiap hari dari Start_Date ke Finish_Date
                    SELECT 
                        dps.Daily_Plan_ID,
                        DATEADD(DAY, v.number, CAST(dps.Start_Date AS DATE)) AS PlanDate
                    FROM 
                        LeanForgeVision.dbo.Daily_Plan_Schedule dps
                    JOIN master.dbo.spt_values v ON v.type = 'P' AND v.number <= DATEDIFF(DAY, dps.Start_Date, dps.Finish_Date)
                ),
                ScheduleYesterdayToday AS (
                    -- Tandai hanya tanggal yang termasuk Yesterday dan Today
                    SELECT 
                        epd.Daily_Plan_ID,
                        CASE 
                            WHEN epd.PlanDate = @Yesterday THEN 'Yesterday'
                            WHEN epd.PlanDate = @Today THEN 'Today'
                        END AS PlanDateCategory
                    FROM ExpandedPlanDates epd
                    WHERE epd.PlanDate IN (@Yesterday, @Today)
                ),
                  PlanSums AS (
                      SELECT 
                          st.PlanDateCategory,
                          SUM(dpd.DailyPlanDetail_TotalPlanned) AS Total_Planned
                      FROM 
                          LeanForgeVision.dbo.DailyPlan_Detail dpd
                      JOIN 
                          ScheduleYesterdayToday st
                          ON dpd.DailyPlanDetail_ID = st.Daily_Plan_ID
                      GROUP BY 
                          st.PlanDateCategory
                  )

                SELECT 
                    dc.PlanDateCategory,
                    ISNULL(ps.Total_Planned, 0) AS Total_Planned
                FROM DateCategories dc
                LEFT JOIN PlanSums ps ON dc.PlanDateCategory = ps.PlanDateCategory
                ORDER BY 
                    CASE dc.PlanDateCategory 
                        WHEN 'Today' THEN 1
                        WHEN 'Yesterday' THEN 2
                END;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var planTotal = new DailyPlannedTotalYesterdayToday
                            {
                                PlanDateCategory = reader.GetString(0), // Index 0 -> PlanDateCategory
                                Total_Planned = reader.GetInt32(1)       // Index 1 -> Total_Planned
                            };
                            planTotals.Add(planTotal);
                        }
                    }
                }
            }

            return planTotals;
        }

        public List<DailyPlanSummaryWeekly> GetWeeklyPlannedTotals()
        {
            var result = new List<DailyPlanSummaryWeekly>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge)) // ganti dbLeanForge dengan connection string Anda
            {
                conn.Open();
                string query = @"
                DECLARE @StartDate DATE = DATEADD(DAY, -6, CAST(GETDATE() AS DATE));
                DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);

                WITH DateRange AS (
                    SELECT @StartDate AS Plan_Date
                    UNION ALL
                    SELECT DATEADD(DAY, 1, Plan_Date)
                    FROM DateRange
                    WHERE Plan_Date < @EndDate
                ),
                PlannedTotals AS (
                    SELECT 
                        dr.Plan_Date,
                        SUM(dpd.DailyPlanDetail_TotalPlanned) AS Total_Planned
                    FROM DateRange dr
                    JOIN LeanForgeVision.dbo.Daily_Plan_Schedule dps
                        ON dr.Plan_Date BETWEEN CAST(dps.Start_Date AS DATE) AND CAST(dps.Finish_Date AS DATE)
                    JOIN LeanForgeVision.dbo.DailyPlan_Detail dpd
                        ON dpd.DailyPlanDetail_ID = dps.Daily_Plan_ID
                    GROUP BY dr.Plan_Date
                )
                SELECT 
                    dr.Plan_Date,
                    ISNULL(pt.Total_Planned, 0) AS Total_Planned
                FROM DateRange dr
                LEFT JOIN PlannedTotals pt ON dr.Plan_Date = pt.Plan_Date
                ORDER BY dr.Plan_Date
                OPTION (MAXRECURSION 100);
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var summary = new DailyPlanSummaryWeekly
                        {
                            PlanDate = reader.GetDateTime(0),
                            TotalPlanned = reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
                        };
                        result.Add(summary);
                    }
                }
            }

            return result;
        }

        public List<ToySortedTotalYesterdayToday> GetTotalToySortedCategories()
        {
            var result = new List<ToySortedTotalYesterdayToday>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
                DECLARE @Today DATE = CAST(GETDATE() AS DATE);
                DECLARE @Yesterday DATE = DATEADD(DAY, -1, @Today);

                -- Step 1: Ambil jumlah data yang ada
                WITH SortedData AS (
                    SELECT 
                        CASE 
                            WHEN CAST(ts.Sorted_At AS DATE) = @Yesterday THEN 'Yesterday'
                            WHEN CAST(ts.Sorted_At AS DATE) = @Today THEN 'Today'
                            ELSE 'Other'
                        END AS PlanDateCategory,
                        COUNT(ts.Toy_sorted_ID) AS TotalSorted
                    FROM 
                        LeanForgeVision.dbo.Toy_Sorted ts
                    WHERE 
                        CAST(ts.Sorted_At AS DATE) IN (@Yesterday, @Today)
                    GROUP BY 
                        CASE 
                            WHEN CAST(ts.Sorted_At AS DATE) = @Yesterday THEN 'Yesterday'
                            WHEN CAST(ts.Sorted_At AS DATE) = @Today THEN 'Today'
                            ELSE 'Other'
                        END
                )

                -- Step 2: Buat daftar tetap 'Today' dan 'Yesterday'
                , DateCategories AS (
                    SELECT 'Today' AS PlanDateCategory
                    UNION ALL
                    SELECT 'Yesterday'
                )

                -- Step 3: Join daftar tetap dengan data sorted
                SELECT 
                    dc.PlanDateCategory,
                    ISNULL(sd.TotalSorted, 0) AS TotalSorted
                FROM 
                    DateCategories dc
                LEFT JOIN 
                    SortedData sd ON dc.PlanDateCategory = sd.PlanDateCategory
                ORDER BY 
                    CASE dc.PlanDateCategory
                        WHEN 'Today' THEN 1
                        WHEN 'Yesterday' THEN 2
                        ELSE 3
                    END;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var category = new ToySortedTotalYesterdayToday
                            {
                                PlanDateCategory = reader.GetString(0),
                                TotalSorted = reader.GetInt32(1)
                            };
                            result.Add(category);
                        }
                    }
                }
            }

            return result;
        }

        public List<SortedSummaryWeekly> GetWeeklySortedTotals()
        {
            List<SortedSummaryWeekly> result = new List<SortedSummaryWeekly>();

            string query = @"
        DECLARE @StartDate DATE = DATEADD(DAY, -6, CAST(GETDATE() AS DATE));
        DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);

        WITH DateRange AS (
            SELECT @StartDate AS PlanDate
            UNION ALL
            SELECT DATEADD(DAY, 1, PlanDate)
            FROM DateRange
            WHERE PlanDate < @EndDate
        ),
        SortedCounts AS (
            SELECT 
                CAST(ts.Sorted_At AS DATE) AS PlanDate,
                COUNT(ts.Toy_sorted_ID) AS TotalSorted
            FROM 
                LeanForgeVision.dbo.Toy_Sorted ts
            WHERE 
                CAST(ts.Sorted_At AS DATE) BETWEEN @StartDate AND @EndDate
            GROUP BY 
                CAST(ts.Sorted_At AS DATE)
        )
        SELECT 
            dr.PlanDate,
            ISNULL(sc.TotalSorted, 0) AS TotalSorted
        FROM 
            DateRange dr
        LEFT JOIN 
            SortedCounts sc ON dr.PlanDate = sc.PlanDate
        ORDER BY 
            dr.PlanDate
        OPTION (MAXRECURSION 100);
            ";

            using (SqlConnection conn = new SqlConnection(dbLeanForge)) // ganti dengan connection string Anda
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new SortedSummaryWeekly
                        {
                            PlanDate = reader.GetDateTime(0),
                            TotalSorted = reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
                        });
                    }
                }
            }

            return result;
        }


        public ToySortedToday GetTotalToySortedToday()
        {
            var result = new ToySortedToday();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
            SELECT 
                COUNT(*) AS Total_Toy_Sorted_Today
            FROM 
                LeanForgeVision.dbo.Toy_Sorted
            WHERE 
                CAST(Sorted_At AS DATE) = CAST(GETDATE() AS DATE);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result.TotalToySortedToday = reader.GetInt32(0);
                        }
                    }
                }
            }

            return result;
        }

        public List<DailyPlanGateModel> GetDailyPlanGateData()
        {
            var result = new List<DailyPlanGateModel>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
                WITH ActiveGates AS (
                SELECT Gate_ID, Gate_Name
                FROM LeanForgeVision.dbo.Gate
                WHERE Gate_Status = 5
                ),
                LatestGatePlans AS (
                    SELECT 
                        dps.Gate_ID,
                        dps.Daily_Plan_ID,
                        ROW_NUMBER() OVER (PARTITION BY dps.Gate_ID ORDER BY dps.Start_Date DESC) AS rn
                    FROM LeanForgeVision.dbo.Daily_Plan_Schedule dps
                    WHERE CAST(dps.Start_Date AS DATE) <= CAST(GETDATE() AS DATE)
                ),
                DailyPlanDetailToHead AS (
                    SELECT 
                        dpd.DailyPlanDetail_ID,
                        dpd.DailyPlanHead_ID,
                        dph.DailyPlanHead_TotalPlanned
                    FROM LeanForgeVision.dbo.DailyPlan_Detail dpd
                    INNER JOIN LeanForgeVision.dbo.DailyPlan_Head dph 
                        ON dpd.DailyPlanHead_ID = dph.DailyPlanHead_ID
                ),
                SortedToys AS (
                    SELECT 
                        dpd.DailyPlanHead_ID,
                        COUNT(ts.Toy_Sorted_ID) AS TotalSorted
                    FROM LeanForgeVision.dbo.Toy_Sorted ts
                    INNER JOIN LeanForgeVision.dbo.DailyPlan_Detail dpd
                        ON ts.Daily_Plan_ID = dpd.DailyPlanDetail_ID
                    GROUP BY dpd.DailyPlanHead_ID
                )
                SELECT 
                    ag.Gate_ID,
                    ag.Gate_Name,
                    dpdth.DailyPlanHead_ID,
                    ISNULL(dpdth.DailyPlanHead_TotalPlanned, 0) AS TotalPlanned,
                    ISNULL(st.TotalSorted, 0) AS TotalSorted
                FROM ActiveGates ag
                LEFT JOIN LatestGatePlans gp ON ag.Gate_ID = gp.Gate_ID AND gp.rn = 1
                LEFT JOIN DailyPlanDetailToHead dpdth ON gp.Daily_Plan_ID = dpdth.DailyPlanDetail_ID
                LEFT JOIN SortedToys st ON dpdth.DailyPlanHead_ID = st.DailyPlanHead_ID;
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var data = new DailyPlanGateModel
                            {
                                Gate_ID = reader.GetInt32(reader.GetOrdinal("Gate_ID")),
                                Gate_Name = reader.GetString(reader.GetOrdinal("Gate_Name")),
                                DailyPlanHead_ID = reader.GetInt32(reader.GetOrdinal("DailyPlanHead_ID")),
                                TotalPlanned = reader.GetInt32(reader.GetOrdinal("TotalPlanned")),
                                TotalSorted = reader.GetInt32(reader.GetOrdinal("TotalSorted"))
                            };

                            result.Add(data);
                        }
                    }
                }
            }

            return result;
        }

        public List<GateCountRealtimeHourlyGate> GetTotalSortedPerGateRealtimeHourly()
        {
            var result = new List<GateCountRealtimeHourlyGate>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge)) // ganti dbLeanForge dengan connection string yang benar
            {
                conn.Open();

                string query = @"
WITH Hours AS (
    SELECT DATEADD(HOUR, -5, DATEADD(HOUR, DATEDIFF(HOUR, 0, GETDATE()), 0)) AS HourSlot
    UNION ALL
    SELECT DATEADD(HOUR, 1, HourSlot)
    FROM Hours
    WHERE HourSlot < DATEADD(HOUR, DATEDIFF(HOUR, 0, GETDATE()), 0)
),

Gates AS (
    SELECT DISTINCT Gate_ID FROM LeanForgeVision.dbo.Daily_Plan_Schedule
),

TimeGate AS (
    SELECT h.HourSlot, g.Gate_ID
    FROM Hours h
    CROSS JOIN Gates g
),

RoundedData AS (
    SELECT 
        ts.Toy_Number,
        DATEADD(HOUR, DATEDIFF(HOUR, 0, ts.Sorted_At), 0) AS Rounded_Hour,
        dps.Gate_ID
    FROM LeanForgeVision.dbo.Toy_Sorted ts
    INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule dps
        ON ts.Daily_Plan_ID = dps.Daily_Plan_ID
    WHERE ts.Sorted_At >= DATEADD(HOUR, -6, DATEADD(HOUR, DATEDIFF(HOUR, 0, GETDATE()), 0))
      AND ts.Sorted_At < DATEADD(HOUR, 1, DATEADD(HOUR, DATEDIFF(HOUR, 0, GETDATE()), 0))  -- JAM SEKARANG DISERTAKAN
)

SELECT 
    tg.HourSlot AS Rounded_Hour,
    tg.Gate_ID,
    COUNT(rd.Toy_Number) AS Total_Sorted
FROM TimeGate tg
LEFT JOIN RoundedData rd
    ON tg.HourSlot = rd.Rounded_Hour AND tg.Gate_ID = rd.Gate_ID
GROUP BY tg.HourSlot, tg.Gate_ID
ORDER BY tg.HourSlot, tg.Gate_ID
OPTION (MAXRECURSION 0);

                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var gateCount = new GateCountRealtimeHourlyGate
                            {
                                RoundedHour = reader.GetDateTime(0),
                                GateID = reader.GetInt32(1),
                                TotalSorted = reader.GetInt32(2)
                            };

                            result.Add(gateCount);
                        }
                    }
                }
            }

            return result;
        }

        public List<ActiveGateResponsibleAndSupervisor> GetActiveGateResponsibleAndSupervisor()
        {
            List<ActiveGateResponsibleAndSupervisor> result = new List<ActiveGateResponsibleAndSupervisor>();
            var employees = GetEmployeesMasterDB(); // Ambil data karyawan dari master table

            string query = @"
            SELECT DISTINCT 
                h.DailyPlanHead_ID,
                h.DailyPlanHead_GateResponsibleID,
                h.DailyPlanHead_SupervisorID,
                s.Gate_ID
            FROM 
                Daily_Plan_Schedule s
            JOIN 
                DailyPlan_Detail d ON s.Daily_Plan_ID = d.DailyPlanDetail_ID
            JOIN 
                DailyPlan_Head h ON d.DailyPlanHead_ID = h.DailyPlanHead_ID
            WHERE 
                GETDATE() BETWEEN s.Start_Date AND s.Finish_Date;
            ";

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    int headId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    string gateResponsibleId = reader.IsDBNull(1) ? "" : reader.GetValue(1).ToString();
                    string supervisorId = reader.IsDBNull(2) ? "" : reader.GetValue(2).ToString();

                    int gateId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                    string gateResponsibleName = employees.FirstOrDefault(e => e.Employee_ID == gateResponsibleId)?.Name ?? "No Name";
                    string supervisorName = employees.FirstOrDefault(e => e.Employee_ID == supervisorId)?.Name ?? "No Name";

                    result.Add(new ActiveGateResponsibleAndSupervisor
                    {
                        DailyPlanHead_ID = headId,
                        DailyPlanHead_GateResponsibleID = int.TryParse(gateResponsibleId, out var gid) ? gid : 0,
                        Gate_Responsible_Name = gateResponsibleName,
                        DailyPlanHead_SupervisorID = int.TryParse(supervisorId, out var sid) ? sid : 0,
                        Supervisor_Name = supervisorName,
                        Gate_ID = gateId
                    });
                }
            }

            return result;
        }

        public List<DailyPlanLocationPage> GetDailyPlanLoactionMonitoring()
        {
            List<DailyPlanLocationPage> result = new List<DailyPlanLocationPage>();

            var employees = GetEmployeesMasterDB(); // Ambil data karyawan
            var statuses = GetLogStatuses();        // Ambil data status

            string query = @"
            SELECT 
                H.DailyPlanHead_ID,
                D.DailyPlanDetail_ID,
                D.DailyPlanDetail_ToyNumber,
                SHead.Gate_ID,
                D.DailyPlanDetail_TotalPlanned,
                D.DailyPlanDetail_StatusID,
                (
                    SELECT COUNT(*) 
                    FROM LeanForgeVision.dbo.Toy_Sorted TS
                    WHERE TS.Daily_Plan_ID = D.DailyPlanDetail_ID
                ) AS Total_Sorted,
                H.DailyPlanHead_GateResponsibleID,
                H.DailyPlanHead_StatusID
            FROM LeanForgeVision.dbo.DailyPlan_Head H
            LEFT JOIN LeanForgeVision.dbo.DailyPlan_Detail D
                ON H.DailyPlanHead_ID = D.DailyPlanHead_ID
            INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule SDetail
                ON D.DailyPlanDetail_ID = SDetail.Daily_Plan_ID
            INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule SHead
                ON D.DailyPlanDetail_ID = SHead.Daily_Plan_ID
            ORDER BY D.DailyPlanDetail_ID ASC;
            ";

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var headId = reader.GetInt32(0);
                    var detailId = reader.GetInt32(1);
                    var toyNumber = reader.IsDBNull(2) ? "" : reader.GetString(2);
                    var gateId = reader.GetInt32(3);
                    var totalPlanned = reader.GetInt32(4);
                    var detailStatusId = reader.GetInt32(5);
                    var totalSorted = reader.GetInt32(6);
                    var gateResponsibleId = reader.IsDBNull(7) ? "" : reader.GetString(7);
                    var headStatusId = reader.GetInt32(8);

                    var gateResponsibleName = employees.FirstOrDefault(e => e.Employee_ID == gateResponsibleId)?.Name ?? "No Name";
                    var detailStatusDesc = statuses.FirstOrDefault(s => s.Status_ID == detailStatusId)?.Status_Desc ?? "Unknown";
                    var headStatusDesc = statuses.FirstOrDefault(s => s.Status_ID == headStatusId)?.Status_Desc ?? "Unknown";

                    result.Add(new DailyPlanLocationPage
                    {
                        DailyPlanHead_ID = headId,
                        DailyPlanDetail_ID = detailId,
                        DailyPlanDetail_ToyNumber = toyNumber,
                        Gate_ID = gateId,
                        DailyPlanDetail_TotalPlanned = totalPlanned,
                        DailyPlanDetail_StatusID = detailStatusId,
                        DailyPlanDetail_StatusDesc = detailStatusDesc,
                        Total_Sorted = totalSorted,
                        Gate_Responsible_ID = gateResponsibleId,
                        Gate_Responsible_Name = gateResponsibleName,
                        DailyPlanHead_StatusID = headStatusId,
                        DailyPlanHead_StatusDesc = headStatusDesc
                    });
                }
            }

            return result;
        }
        public List<DailyPlanShortDropdown> GetDailyPlanShortData()
        {
            List<DailyPlanShortDropdown> result = new List<DailyPlanShortDropdown>();

            string query = @"
        SELECT 
            D.DailyPlanDetail_ID,
            D.DailyPlanDetail_ToyNumber,
            SHead.Gate_ID,
            D.DailyPlanDetail_StatusID,
            H.DailyPlanHead_StatusID
        FROM LeanForgeVision.dbo.DailyPlan_Head H
        LEFT JOIN LeanForgeVision.dbo.DailyPlan_Detail D
            ON H.DailyPlanHead_ID = D.DailyPlanHead_ID
        INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule SDetail
            ON D.DailyPlanDetail_ID = SDetail.Daily_Plan_ID
        INNER JOIN LeanForgeVision.dbo.Daily_Plan_Schedule SHead
            ON D.DailyPlanDetail_ID = SHead.Daily_Plan_ID
        ORDER BY D.DailyPlanDetail_ID ASC;
            ";

            using (SqlConnection conn = new SqlConnection(dbLeanForge)) // pastikan variabel ini didefinisikan
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                conn.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(new DailyPlanShortDropdown
                    {
                        DailyPlanDetail_ID = reader.GetInt32(0),
                        DailyPlanDetail_ToyNumber = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        Gate_ID = reader.GetInt32(2),
                        DailyPlanDetail_StatusID = reader.GetInt32(3),
                        DailyPlanHead_StatusID = reader.GetInt32(4)
                    });
                }
            }

            return result;
        }
        public List<ToySorted> GetToySortedByDailyPlanId(int dailyPlanId)
        {
            List<ToySorted> toySortedList = new List<ToySorted>();
            var statuses = GetLogStatuses(); 

            string query = @"
                SELECT 
                    [Toy_Number], 
                    [Sorted_At], 
                    [Sorting_Method], 
                    [Daily_Plan_ID]
                FROM 
                    [LeanForgeVision].[dbo].[Toy_Sorted]
                WHERE 
                    [Daily_Plan_ID] = @DailyPlanId";

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@DailyPlanId", dailyPlanId);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int sortingMethod = Convert.ToInt32(reader["Sorting_Method"]);

                        ToySorted model = new ToySorted
                        {
                            Toy_Number = reader["Toy_Number"].ToString(),
                            Sorted_At = Convert.ToDateTime(reader["Sorted_At"]),
                            Sorting_Method = sortingMethod,
                            Daily_Plan_ID = Convert.ToInt32(reader["Daily_Plan_ID"]),
                            Sorthing_MethodName = statuses.FirstOrDefault(s => s.Status_ID == sortingMethod)?.Status_Desc ?? "Unknown"
                        };

                        toySortedList.Add(model);
                    }

                }
            }

            return toySortedList;
        }

        public List<GateLocation> GetGatesWithStatusName()
        {
            var gates = new List<GateLocation>();
            var statuses = GetLogStatuses(); // Ambil status dari Log_Status

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = "SELECT [Gate_ID], [Gate_Name], [Gate_Status] FROM [LeanForgeVision].[dbo].[Gate]";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int gateStatus = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);

                        gates.Add(new GateLocation
                        {
                            Gate_ID = reader.GetInt32(0),
                            Gate_Name = reader.IsDBNull(1) ? "No Name" : reader.GetString(1),
                            Gate_Status = gateStatus,
                            Status_Name = statuses.FirstOrDefault(s => s.Status_ID == gateStatus)?.Status_Desc ?? "Unknown"
                        });
                    }
                }
            }

            return gates;
        }

        public GateCount GetTotalGateStatus5()
        {
            var result = new GateCount();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
                SELECT 
                    COUNT(*) AS Total_Gate_Status_5
                FROM 
                    LeanForgeVision.dbo.Gate
                WHERE 
                    Gate_Status = 5;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result.TotalGateStatus5 = reader.GetInt32(0);
                        }
                    }
                }
            }

            return result;
        }
        public ActiveGateCount GetActiveGateCounts()
        {
            var result = new ActiveGateCount();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                // Query Today
                string queryToday = @"
            SELECT COUNT(DISTINCT Gate_ID)
            FROM LeanForgeVision.dbo.Daily_Plan_Schedule
            WHERE CAST(Start_Date AS DATE) <= CAST(GETDATE() AS DATE)
              AND CAST(Finish_Date AS DATE) >= CAST(GETDATE() AS DATE);";

                using (SqlCommand cmdToday = new SqlCommand(queryToday, conn))
                {
                    result.ActiveGatesToday = (int)cmdToday.ExecuteScalar();
                }

                // Query Yesterday
                string queryYesterday = @"
            SELECT COUNT(DISTINCT Gate_ID)
            FROM LeanForgeVision.dbo.Daily_Plan_Schedule
            WHERE CAST(Start_Date AS DATE) <= CAST(DATEADD(DAY, -1, GETDATE()) AS DATE)
              AND CAST(Finish_Date AS DATE) >= CAST(DATEADD(DAY, -1, GETDATE()) AS DATE);";

                using (SqlCommand cmdYesterday = new SqlCommand(queryYesterday, conn))
                {
                    result.ActiveGatesYesterday = (int)cmdYesterday.ExecuteScalar();
                }
            }

            // === Hitung Status & Percentage ===
            int pendingToday = result.ActiveGatesToday;
            int pendingYesterday = result.ActiveGatesYesterday;

            if (pendingYesterday > 0)
            {
                result.PendingDifferencePercentage = Math.Abs((double)(pendingToday - pendingYesterday) / pendingYesterday) * 100;
                result.PendingStatus = pendingToday > pendingYesterday ? "Increase" :
                                       pendingToday < pendingYesterday ? "Decrease" : "No Change";
            }
            else
            {
                if (pendingToday > 0)
                {
                    result.PendingDifferencePercentage = 100;
                    result.PendingStatus = "Increase";
                }
                else
                {
                    result.PendingDifferencePercentage = 0;
                    result.PendingStatus = "No Change";
                }
            }


            return result;
        }
        public List<WeeklyActiveGateModel> GetWeeklyActiveGateCounts()
        {
            var result = new List<WeeklyActiveGateModel>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
        DECLARE @StartDate DATE = DATEADD(DAY, -6, CAST(GETDATE() AS DATE));
        DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);

        WITH DateRange AS (
            SELECT @StartDate AS [Date]
            UNION ALL
            SELECT DATEADD(DAY, 1, [Date])
            FROM DateRange
            WHERE [Date] < @EndDate
        ),
        ActiveGates AS (
            SELECT
                d.[Date],
                COUNT(DISTINCT dps.Gate_ID) AS ActiveGateCount
            FROM DateRange d
            LEFT JOIN LeanForgeVision.dbo.Daily_Plan_Schedule dps
                ON CAST(dps.Start_Date AS DATE) <= d.[Date]
                AND CAST(dps.Finish_Date AS DATE) >= d.[Date]
            GROUP BY d.[Date]
        )
        SELECT [Date], ActiveGateCount FROM ActiveGates
        ORDER BY [Date]
        OPTION (MAXRECURSION 100);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new WeeklyActiveGateModel
                        {
                            Date = reader.GetDateTime(0),
                            ActiveGateCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
                        });
                    }
                }
            }

            return result;
        }



        public List<GateLocation> GetGatesWithStatusNameForInactiveGate(int dailyPlanHeadId)
        {
            var gates = new List<GateLocation>();
            var statuses = GetLogStatuses();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = @"
        SELECT DISTINCT g.[Gate_ID], g.[Gate_Name], g.[Gate_Status]
        FROM [LeanForgeVision].[dbo].[Gate] g
        WHERE g.[Gate_Status] = 6

        UNION

        SELECT DISTINCT g.[Gate_ID], g.[Gate_Name], g.[Gate_Status]
        FROM [LeanForgeVision].[dbo].[Gate] g
        JOIN [LeanForgeVision].[dbo].[Daily_Plan_Schedule] s ON s.[Gate_ID] = g.[Gate_ID]
        JOIN [LeanForgeVision].[dbo].[DailyPlan_Detail] d ON s.[Daily_Plan_ID] = d.[DailyPlanDetail_ID]
        WHERE g.[Gate_Status] = 5 AND d.[DailyPlanHead_ID] = @DailyPlanHeadID
        ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DailyPlanHeadID", dailyPlanHeadId);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int gateStatus = reader.IsDBNull(2) ? 0 : reader.GetInt32(2);
                            gates.Add(new GateLocation
                            {
                                Gate_ID = reader.GetInt32(0),
                                Gate_Name = reader.IsDBNull(1) ? "No Name" : reader.GetString(1),
                                Gate_Status = gateStatus,
                                Status_Name = statuses.FirstOrDefault(s => s.Status_ID == gateStatus)?.Status_Desc ?? "Unknown"
                            });
                        }
                    }
                }
            }

            return gates;
        }



        public List<LogStatusModel> GetLogStatuses()
        {
            var statuses = new List<LogStatusModel>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = "SELECT [Status_ID], [Status_Desc] FROM [LeanForgeVision].[dbo].[Log_Status]";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        statuses.Add(new LogStatusModel
                        {
                            Status_ID = reader.GetInt32(0),
                            Status_Desc = reader.IsDBNull(1) ? "No Description" : reader.GetString(1)
                        });
                    }
                }
            }

            return statuses;
        }

        public List<ProblemList> GetProblems()
        {
            var problems = new List<ProblemList>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge)) // pastikan dbLeanForge adalah connection string Anda
            {
                conn.Open();
                string query = "SELECT [Problem_ID], [Problem_Name] FROM [LeanForgeVision].[dbo].[ProblemList]";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        problems.Add(new ProblemList
                        {
                            Problem_ID = reader.GetInt32(0),
                            Problem_Name = reader.IsDBNull(1) ? "No Name" : reader.GetString(1)
                        });
                    }
                }
            }

            return problems;
        }
        public int GetTodayProblemCount()
        {
            int count = 0;

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = @"SELECT SUM([Report_Total]) AS Total_Problem_Today
                        FROM [LeanForgeVision].[dbo].[ProblemReport]
                        WHERE Report_Date >= CAST(GETDATE() AS DATE)
                          AND Report_Date < DATEADD(DAY, 1, CAST(GETDATE() AS DATE));
                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    object result = cmd.ExecuteScalar();
                    count = result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }

            return count;
        }


        public (int today, int yesterday) GetProblemCountsTodayAndYesterday()
        {
            int todayCount = 0;
            int yesterdayCount = 0;

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = @"
                SELECT 
                    SUM(CASE 
                            WHEN Report_Date >= CAST(GETDATE() AS DATE) 
                                 AND Report_Date < DATEADD(DAY, 1, CAST(GETDATE() AS DATE)) 
                            THEN Report_Total 
                            ELSE 0 
                        END) AS TodayTotal,
        
                SUM(CASE 
                            WHEN Report_Date >= CAST(DATEADD(DAY, -1, GETDATE()) AS DATE) 
                                 AND Report_Date < CAST(GETDATE() AS DATE) 
                            THEN Report_Total 
                            ELSE 0 
                        END) AS YesterdayTotal
                FROM [LeanForgeVision].[dbo].[ProblemReport]";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        todayCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                        yesterdayCount = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                    }
                }
            }

            return (todayCount, yesterdayCount);
        }

        public List<MonthlyTopProblem> GetTop3MonthlyProblems()
        {
            var result = new List<MonthlyTopProblem>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = @"
        WITH MonthlyData AS (
            SELECT 
                FORMAT(Report_Date, 'yyyy-MM') AS Month,
                Problem_ID,
                SUM(Report_Total) AS Total
            FROM [LeanForgeVision].[dbo].[ProblemReport]
            WHERE Report_Date >= DATEADD(MONTH, -8, DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1))
            GROUP BY FORMAT(Report_Date, 'yyyy-MM'), Problem_ID
        ),
        RankedProblems AS (
            SELECT 
                Month,
                Problem_ID,
                Total,
                RANK() OVER (PARTITION BY Month ORDER BY Total DESC) AS Rank
            FROM MonthlyData
        )
        SELECT 
            rp.Month,
            rp.Problem_ID,
            pl.Problem_Name,
            rp.Total
        FROM RankedProblems rp
        LEFT JOIN [LeanForgeVision].[dbo].[ProblemList] pl ON rp.Problem_ID = pl.Problem_ID
        WHERE rp.Rank <= 3
        ORDER BY rp.Month, rp.Total DESC;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new MonthlyTopProblem
                        {
                            Month = reader.GetString(0),
                            Problem_ID = reader.GetInt32(1),
                            Problem_Name = reader.IsDBNull(2) ? "No Name" : reader.GetString(2),
                            Total = reader.GetInt32(3)
                        });
                    }
                }
            }

            return result;
        }

        public List<ProblemReportCount> GetProblemReportCounts()
        {
            var results = new List<ProblemReportCount>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge)) // dbLeanForge = connection string
            {
                conn.Open();
                string query = @"
            SELECT 
                pr.Problem_ID,
                p.Problem_Name,
                COUNT(*) AS TotalReports
            FROM 
                [LeanForgeVision].[dbo].[ProblemReport] pr
            LEFT JOIN 
                [LeanForgeVision].[dbo].[ProblemList] p ON pr.Problem_ID = p.Problem_ID
            GROUP BY 
                pr.Problem_ID, p.Problem_Name
            ORDER BY 
                TotalReports DESC;";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(new ProblemReportCount
                        {
                            Problem_ID = reader.GetInt32(0),
                            Problem_Name = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1),
                            TotalReports = reader.GetInt32(2)
                        });
                    }
                }
            }

            return results;
        }

        public List<GateProblemReportModel> GetGateProblemReports()
        {
            List<GateProblemReportModel> result = new List<GateProblemReportModel>();

            string query = @"
                SELECT 
                    g.Gate_Name,
                    pr.Gate_ID,
                    COUNT(*) AS TotalReports
                FROM 
                    [LeanForgeVision].[dbo].[ProblemReport] pr
                LEFT JOIN 
                    [LeanForgeVision].[dbo].[Gate] g ON pr.Gate_ID = g.Gate_ID
                GROUP BY 
                    pr.Gate_ID, g.Gate_Name
                ORDER BY 
                    TotalReports DESC;";

            using (SqlConnection con = new SqlConnection(dbLeanForge))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();
                using (SqlDataReader rdr = cmd.ExecuteReader())
                {
                    while (rdr.Read())
                    {
                        result.Add(new GateProblemReportModel
                        {
                            Gate_ID = Convert.ToInt32(rdr["Gate_ID"]),
                            Gate_Name = rdr["Gate_Name"].ToString(),
                            TotalReports = Convert.ToInt32(rdr["TotalReports"])
                        });
                    }
                }
            }

            return result;
        }

        public List<ProblemReportTodayModel> GetTodayProblemReports()
        {
            var reports = new List<ProblemReportTodayModel>();
            string query = @"
            SELECT 
                pr.Report_ID,
                pr.DailyPlanHead_ID,
                p.Problem_Name,
                g.Gate_Name,
                pr.Report_Date,
                FORMAT(pr.Report_Date, 'HH:mm') AS Report_Time
            FROM 
                [LeanForgeVision].[dbo].[ProblemReport] pr
            LEFT JOIN 
                [LeanForgeVision].[dbo].[ProblemList] p ON pr.Problem_ID = p.Problem_ID
            LEFT JOIN 
                [LeanForgeVision].[dbo].[Gate] g ON pr.Gate_ID = g.Gate_ID
            WHERE 
                CAST(pr.Report_Date AS DATE) = CAST(GETDATE() AS DATE)
            ORDER BY 
                pr.Report_Date DESC;
        ";

            using (var connection = new SqlConnection(dbLeanForge))
            {
                connection.Open();
                using (var command = new SqlCommand(query, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var report = new ProblemReportTodayModel
                            {
                                Report_ID = reader.GetInt32(0),
                                DailyPlanHead_ID = reader.GetInt32(1),
                                Problem_Name = reader.GetString(2),
                                Gate_Name = reader.GetString(3),
                                Report_Date = reader.GetDateTime(4),
                                Report_Time = reader.GetString(5)
                            };
                            reports.Add(report);
                        }
                    }
                }
            }

            return reports;
        }
        public List<ProblemReportViewModel> GetProblemReports()
        {
            var result = new List<ProblemReportViewModel>();
            var employees = GetEmployeesMasterDB();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                string query = @"
        SELECT
            dr.DailyPlanDetail_ID,
            tn.CreatedDate,
            tn.Toy_Number,
            tn.Toy_Description,
            dr.DailyPlanDetail_ToyNumber,
            dr.DailyPlanDetail_TotalPlanned,
            pr.Report_Total,
            pr.Reporter_ID,
            pr.Report_Date,
            pr.Problem_ID,
            pl.Problem_Name
        FROM [LeanForgeVision].[dbo].[toy_number_master] tn
        JOIN [LeanForgeVision].[dbo].[DailyPlan_Detail] dr 
            ON tn.Toy_Number = dr.DailyPlanDetail_ToyNumber
        JOIN [LeanForgeVision].[dbo].[ProblemReport] pr 
            ON dr.DailyPlanDetail_ID = pr.DailyPlanDetail_ID
        LEFT JOIN [LeanForgeVision].[dbo].[ProblemList] pl 
            ON pr.Problem_ID = pl.Problem_ID";

                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ProblemReportViewModel
                        {
                            DailyPlanDetail_ID = Convert.ToInt32(reader["DailyPlanDetail_ID"]),
                            CreatedDate = Convert.ToDateTime(reader["CreatedDate"]),
                            Toy_Number = reader["Toy_Number"].ToString(),
                            Toy_Description = reader["Toy_Description"].ToString(),
                            DailyPlanDetail_ToyNumber = reader["DailyPlanDetail_ToyNumber"].ToString(),
                            DailyPlanDetail_TotalPlanned = Convert.ToInt32(reader["DailyPlanDetail_TotalPlanned"]),
                            Report_Total = Convert.ToInt32(reader["Report_Total"]),
                            Reporter_ID = reader["Reporter_ID"].ToString(),
                            Reporter_Name = employees.FirstOrDefault(e => e.Employee_ID == reader["Reporter_ID"].ToString())?.Name ?? "No Name",
                            Report_Date = Convert.ToDateTime(reader["Report_Date"]),
                            Problem_ID = Convert.ToInt32(reader["Problem_ID"]),
                            Problem_Name = reader["Problem_Name"]?.ToString() ?? "N/A"
                        });
                    }
                }
            }

            return result;
        }

        public List<ProblemReportWeeklyModel> GetWeeklyProblemReports()
        {
            var result = new List<ProblemReportWeeklyModel>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();

                string query = @"
        DECLARE @StartDate DATE = DATEADD(DAY, -6, CAST(GETDATE() AS DATE));
        DECLARE @EndDate DATE = CAST(GETDATE() AS DATE);

        WITH DateRange AS (
            SELECT @StartDate AS Report_Date
            UNION ALL
            SELECT DATEADD(DAY, 1, Report_Date)
            FROM DateRange
            WHERE Report_Date < @EndDate
        ),
        ReportSums AS (
            SELECT 
                CAST(Report_Date AS DATE) AS Report_Date,
                SUM(Report_Total) AS Total_Report
            FROM [LeanForgeVision].[dbo].[ProblemReport]
            WHERE CAST(Report_Date AS DATE) BETWEEN @StartDate AND @EndDate
            GROUP BY CAST(Report_Date AS DATE)
        )
        SELECT 
            d.Report_Date,
            ISNULL(r.Total_Report, 0) AS Total_Report
        FROM DateRange d
        LEFT JOIN ReportSums r ON d.Report_Date = r.Report_Date
        ORDER BY d.Report_Date
        OPTION (MAXRECURSION 100);";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ProblemReportWeeklyModel
                        {
                            ReportDate = reader.GetDateTime(0),
                            TotalReport = reader.IsDBNull(1) ? 0 : reader.GetInt32(1)
                        });
                    }
                }
            }

            return result;
        }

        public void InsertProblemReport(int dailyPlanDetailId, int dailyPlanHeadId, int reportTotal, int problemId, string reporterId, int gateId)
        {
            try
            {
                string query = @"
        INSERT INTO [LeanForgeVision].[dbo].[ProblemReport]
        ([DailyPlanDetail_ID], [DailyPlanHead_ID], [Report_Total], [Problem_ID], [Reporter_ID], [Report_Date], [Gate_ID])
        VALUES
        (@DailyPlanDetail_ID, @DailyPlanHead_ID, @Report_Total, @Problem_ID, @Reporter_ID, @Report_Date, @Gate_ID)";

                using (SqlConnection conn = new SqlConnection(dbLeanForge))
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@DailyPlanDetail_ID", dailyPlanDetailId);
                    cmd.Parameters.AddWithValue("@DailyPlanHead_ID", dailyPlanHeadId);
                    cmd.Parameters.AddWithValue("@Report_Total", reportTotal);
                    cmd.Parameters.AddWithValue("@Problem_ID", problemId);
                    cmd.Parameters.AddWithValue("@Reporter_ID", reporterId);
                    cmd.Parameters.AddWithValue("@Report_Date", DateTime.Now); // Or use GETDATE() in SQL
                    cmd.Parameters.AddWithValue("@Gate_ID", gateId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException sqlEx)
            {
                // Log SQL exception
                Debug.Print($"SQL Error: {sqlEx.Message}");
                throw; // Rethrow or handle it as needed
            }
            catch (Exception ex)
            {
                // Log general exceptions
                Debug.Print($"Error: {ex.Message}");
                throw; // Rethrow or handle it as needed
            }
        }

        public List<ToyNumberModel> GetToyNumbers()
        {
            var result = new List<ToyNumberModel>();

            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                string query = "SELECT [Toy_Number], [Toy_Description] FROM [LeanForgeVision].[dbo].[toy_number_master]";
                SqlCommand cmd = new SqlCommand(query, conn);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new ToyNumberModel
                        {
                            Toy_Number = reader["Toy_Number"].ToString(),
                            Toy_Description = reader["Toy_Description"].ToString()
                        });
                    }
                }
            }

            return result;
        }

        public void InsertAuditLog(AuditLogModel log)
        {
            using (SqlConnection conn = new SqlConnection(dbLeanForge))
            {
                conn.Open();
                string query = @"
            INSERT INTO AuditLog
                (AuditLog_TableName, AuditLog_RecordID, AuditLog_ActionTypeID, AuditLog_ChangedBy, AuditLog_ChangedAt)
            VALUES 
                (@TableName, @RecordID, @ActionTypeID, @ChangedBy, @ChangedAt)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TableName", log.AuditLog_TableName);
                    cmd.Parameters.AddWithValue("@RecordID", log.AuditLog_RecordID);
                    cmd.Parameters.AddWithValue("@ActionTypeID", log.AuditLog_ActionTypeID);
                    cmd.Parameters.AddWithValue("@ChangedBy", log.AuditLog_ChangedBy);
                    cmd.Parameters.AddWithValue("@ChangedAt", log.AuditLog_ChangedAt);

                    cmd.ExecuteNonQuery();
                }
            }
        }






    }
}