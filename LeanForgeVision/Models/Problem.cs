using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

namespace LeanForgeVision.Models
{
    public class ProblemList
    {
        public int Problem_ID { get; set; }
        public string Problem_Name { get; set; }
    }

    public class ProblemReportDto
    {
        public int DailyPlanDetailId { get; set; }
        public int DailyPlanHeadId { get; set; }
        public int ReportTotal { get; set; }
        public int ProblemId { get; set; }
        public string ReporterId { get; set; }
        public int GateId { get; set; }
    }
    public class ProblemCountToday
    {
        public int Count { get; set; }
    }

    public class MonthlyTopProblem
    {
        public string Month { get; set; }
        public int Problem_ID { get; set; }
        public string Problem_Name { get; set; } // ← tambahkan ini
        public int Total { get; set; }
    }

    public class ProblemReportCount
    {
        public int Problem_ID { get; set; }
        public string Problem_Name { get; set; }
        public int TotalReports { get; set; }
    }

    public class GateProblemReportModel
    {
        public int Gate_ID { get; set; }
        public string Gate_Name { get; set; }
        public int TotalReports { get; set; }
    }

    public class ProblemReportTodayModel
    {
        public int Report_ID { get; set; }
        public int DailyPlanHead_ID { get; set; }
        public string Problem_Name { get; set; }
        public string Gate_Name { get; set; }
        public DateTime Report_Date { get; set; }
        public string Report_Time { get; set; }
    }

    public class ProblemReportViewModel
    {
        public int DailyPlanDetail_ID { get; set; }
        public DateTime CreatedDate { get; set; }
        public string Toy_Number { get; set; }
        public string Toy_Description { get; set; }
        public string DailyPlanDetail_ToyNumber { get; set; }
        public int DailyPlanDetail_TotalPlanned { get; set; }
        public int Report_Total { get; set; }
        public string Reporter_ID { get; set; }
        public string Reporter_Name { get; set; }
        public DateTime Report_Date { get; set; }
        public int Problem_ID { get; set; }
        public string Problem_Name { get; set; }

    }
    public class ProblemReportWeeklyModel
    {
        public DateTime ReportDate { get; set; }
        public int TotalReport { get; set; }
    }




}