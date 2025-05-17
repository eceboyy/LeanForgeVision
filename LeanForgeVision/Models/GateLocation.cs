using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeanForgeVision.Models
{
    public class GateLocation
    {
        public int Gate_ID { get; set; }
        public string Gate_Name { get; set; }
        public int Gate_Status { get; set; }

        public string Status_Name { get; set; } // ini yang baru ditambahkan
    }
    public class GateCount
    {
        public int TotalGateStatus5 { get; set; }
    }
    public class ActiveGateCount
    {
        public int ActiveGatesToday { get; set; }
        public int ActiveGatesYesterday { get; set; }
        public double PendingDifferencePercentage { get; set; }
        public string PendingStatus { get; set; }
    }
    public class DailyPlanGateModel
    {
        public int Gate_ID { get; set; }
        public string Gate_Name { get; set; }
        public int DailyPlanHead_ID { get; set; }
        public int TotalPlanned { get; set; }
        public int TotalSorted { get; set; }
    }
    public class GateCountRealtimeHourlyGate
    {
        public DateTime RoundedHour { get; set; }
        public int GateID { get; set; }
        public int TotalSorted { get; set; }
    }

    public class WeeklyActiveGateModel
    {
        public DateTime Date { get; set; }
        public int ActiveGateCount { get; set; }
    }



}