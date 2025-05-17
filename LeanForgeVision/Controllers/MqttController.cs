using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Configuration;
using LeanForgeVision.Database;
using LeanForgeVision.Models;
using System.Linq;



namespace LeanForgeVision.Controllers
{
    public class MqttController : Controller
    {
        private static IMqttClient _mqttClient;
        private static bool _isConnected = false;
        private static ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>(); // Menyimpan pesan yang diterima
       
        private DbConnection _dbConnection = new DbConnection();

        public MqttController()
        {
            _dbConnection = new DbConnection();

            if (_mqttClient == null)
            {
                InitializeMqttClient();
                Task.Run(() => ConnectToMqttBroker());
            }
            Task.Run(() => RunStatusUpdaterPeriodically());
        }

        private async Task RunStatusUpdaterPeriodically()
        {
            while (true)
            {
                try
                {
                    // Panggil update status
                    RunStatusUpdater();
                    RunStatusUpdaterForCompletedSorted();

                    // Tunggu 10 detik sebelum menjalankan lagi
                    await Task.Delay(10000); // 10 detik = 10000 ms
                }
                catch (Exception ex)
                {
                    Debug.Print($"[ERROR] Error while updating status periodically: {ex.Message}");
                }
            }
        }

        private void RunStatusUpdater()
        {
            try
            {
                string currentTime = GetCurrentTime();
                Debug.Print($"[AUTO] Running status update at {currentTime}");

                int updatedCount = _dbConnection.UpdateDailyPlanStatusConditional();
                Debug.Print($"[AUTO] Status update complete. Total updated: {updatedCount}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[AUTO][ERROR] {ex.Message}");
            }
        }


        private void RunStatusUpdaterForCompletedSorted()
        {
            try
            {
                string currentTime = GetCurrentTime();
                Debug.Print($"[AUTO] Running status update at {currentTime} for update status completed sorted");

                int updatedCount = _dbConnection.UpdateDailyPlanStatusConditionalForCompletedSorted();
                Debug.Print($"[AUTO] Status update complete. Total updated: {updatedCount} for update status completed sorted");
            }
            catch (Exception ex)
            {
                Debug.Print($"[AUTO][ERROR] {ex.Message}");
            }
        }




        private void InitializeMqttClient()
        {
            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.UseConnectedHandler(async e =>
            {
                Debug.Print("✅ Connected to MQTT Broker!");
                await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("Toy_Number/#").Build());
                Debug.Print("📡 Subscribed to topic: Toy_Number/#");
            });

            _mqttClient.UseApplicationMessageReceivedHandler(async e =>
            {
                string topic = e.ApplicationMessage.Topic;
                string payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                Debug.Print($"📩 Received message from {topic}: {payload}");

                _messageQueue.Enqueue($"[{topic}] {payload}");

                if (!string.IsNullOrEmpty(payload))
                {
                    await HandleIncomingMessage(topic, payload);
                }
            });
        }

        private async Task HandleIncomingMessage(string topic, string payload)
        {
            var planSchedules = _dbConnection.GetPlanSchedules();

            // Filter jadwal aktif untuk toy number yang diterima
            var matchingSchedules = planSchedules
                .Where(p =>
                    p.Toy_Number == payload &&
                    DateTime.Now >= p.Start_Date &&
                    DateTime.Now <= p.Finish_Date)
                .ToList();

            if (!matchingSchedules.Any())
            {
                Debug.Print($"⚠️ Tidak ditemukan jadwal aktif untuk Toy Number: {payload} pada waktu sekarang.");
                return;
            }

            // Ambil total planned dan sorted
            var plannedAndSortedList = _dbConnection.GetPlannedAndSortedCounts(payload);
            // Filter hanya yang belum penuh (TotalSorted < TotalPlanned)
            var validSchedules = matchingSchedules
                .Join(plannedAndSortedList,
                      schedule => schedule.Daily_Plan_ID,
                      ps => ps.DailyPlanId,
                      (schedule, ps) => new
                      {
                          Schedule = schedule,
                          TotalPlanned = ps.TotalPlanned,
                          TotalSorted = ps.TotalSorted
                      })
                .Where(x => x.TotalSorted < x.TotalPlanned)
                .OrderBy(x => x.TotalSorted)
                .ToList();

            if (!validSchedules.Any())
            {
                Debug.Print($"✅ Toy Number '{payload}' sudah lengkap disortir untuk semua jadwal aktif.");
                return;
            }

            // Pilih jadwal yang sorted-nya paling sedikit
            var selected = validSchedules.First();
            int dailyPlanId = selected.Schedule.Daily_Plan_ID;
            DateTime sortedAt = DateTime.Now;
            int sortingMethod = 8;

            _dbConnection.InsertToySortedAutomated(payload, sortedAt, sortingMethod, dailyPlanId);
            Debug.Print($"✅ Data inserted: {payload}, {sortedAt}, method: {sortingMethod}, plan ID: {dailyPlanId}");

            await PublishGateLocation(payload, selected.Schedule.Gate_ID);
            var auditLog = new AuditLogModel
            {
                AuditLog_TableName = "Toy_Sorted Automated",
                AuditLog_RecordID = dailyPlanId.ToString(),
                AuditLog_ActionTypeID = 8,
                AuditLog_ChangedBy = "SYS999",
                AuditLog_ChangedAt = DateTime.Now
            };

            _dbConnection.InsertAuditLog(auditLog);
        }



        private async Task PublishGateLocation(string toyNumber, string gateLocation)
        {
            string locationTopic = $"Toy_Location/{toyNumber}";
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(locationTopic)
                .WithPayload(gateLocation)
                .WithExactlyOnceQoS()
                .WithRetainFlag()
                .Build();

            await _mqttClient.PublishAsync(message);
            Debug.Print($"📤 Published Gate_ID '{gateLocation}' to topic: {locationTopic}");
        }



        private async Task ConnectToMqttBroker()
        {
            var clientId = "mqttx_" + Guid.NewGuid().ToString("N"); // Client ID unik
            var options = new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer("broker.emqx.io", 1883)
                .WithCleanSession()
                .Build();

            while (!_isConnected)
            {
                try
                {
                    await _mqttClient.ConnectAsync(options);
                    _isConnected = true;
                    Debug.Print($"✅ MQTT Connected with Client ID: {clientId}");
                }
                catch (Exception ex)
                {
                    Debug.Print($"❌ MQTT Connection Error: {ex.Message}");
                    await Task.Delay(5000); // Coba lagi setelah 5 detik
                }
            }
        }


        // ✅ API untuk cek status MQTT
        public ActionResult Index()
        {
            return Json(new
            {
                status = "success",
                message = "✅ MQTT Client is running and subscribed to 'Toy_Number/#'",
                subscribedTopic = "Toy_Number/#"
            }, JsonRequestBehavior.AllowGet);
        }

        // ✅ API untuk melihat pesan terbaru yang diterima
        public ActionResult GetLatestMessage()
        {
            if (_messageQueue.TryDequeue(out string latestMessage))
            {
                return Json(new
                {
                    status = "success",
                    topic = "Toy_Number/#",
                    receivedMessage = latestMessage
                }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new
                {
                    status = "success",
                    topic = "Toy_Number/#",
                    receivedMessage = "No messages received yet"
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // ✅ API untuk melihat semua pesan yang diterima (jika tidak ingin dihapus langsung)
        public ActionResult GetAllMessages()
        {
            var allMessages = _messageQueue.ToArray(); // Ambil semua pesan tanpa menghapus dari queue

            Debug.Print($"📦 Total messages in queue: {allMessages.Length}");

            return Json(new
            {
                status = "success",
                topic = "Toy_Number/#",
                receivedMessages = allMessages
            }, JsonRequestBehavior.AllowGet);
        }

       

        public JsonResult GetPlanSchedules()
        {
            var planSchedules = _dbConnection.GetPlanSchedules();
            return Json(planSchedules, JsonRequestBehavior.AllowGet);
        }

        public string GetCurrentTime()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }

        public JsonResult UpdatePlanStatuses()
        {
            try
            {
                string currentTime = GetCurrentTime();
                Debug.Print($"[DEBUG] UpdatePlanStatuses called at: {currentTime}");

                int updatedCount = _dbConnection.UpdateDailyPlanStatusConditional();

                Debug.Print($"[DEBUG] Total records updated: {updatedCount}");

                return Json(new
                {
                    success = true,
                    message = "Status updated successfully",
                    updatedRecords = updatedCount,
                    updatedAt = currentTime
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Debug.Print($"[ERROR] Exception occurred at {GetCurrentTime()}: {ex.Message}");

                return Json(new
                {
                    success = false,
                    message = "Error while updating statuses",
                    error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }


    }
}
