using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeanForgeVision.Models
{
   

    public class ScheduleModel
    {
        public int Schedule_ID { get; set; }
        public int Schedule_Detail_ID { get; set; }
        public DateTime Created_At { get; set; }
        public int Total_Planned { get; set; }
        public string Planner_ID { get; set; }
        public string Planner_Name { get; set; }
        public string Gate_Responsible_ID { get; set; }
        public string Gate_Responsible_Name { get; set; }
        public string Supervisor_ID { get; set; }
        public string Supervisor_Name { get; set; }
        public int Status_ID { get; set; }
        public string Status_Desc { get; set; }

        public string Toy_Number { get; set; }
        public int Toy_TotalPlanned { get; set; }
        public int Detail_Status_ID { get; set; }
        public string Detail_Status_Desc { get; set; }

    }



    public class ScheduleRequestModel
    {
        public string Planner_ID { get; set; }
        public int Total_Planned { get; set; }
        public string Gate_Responsible_ID { get; set; }
        public string Supervisor_ID { get; set; }
        public List<ToyNumberPlannedModel> Toy_Numbers { get; set; }
    }


    public class ScheduleDailyPlan
    {
        public int ScheduleId { get; set; } // maps to Daily_Plan_ID
        public DateTime StartDateTime { get; set; } // maps to Start_Date
        public DateTime EndDateTime { get; set; } // maps to Finish_Date
        public string GateLocation { get; set; } // maps to Gate_ID
    }

    public class PlanScheduleModelComparisonWithMQTT
    {
        public int Daily_Plan_ID { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime Finish_Date { get; set; }
        public string Gate_ID { get; set; }
        public string Toy_Number { get; set; }
    }

    public class PlanDetailModel
    {
        public int Daily_Plan_ID { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime Finish_Date { get; set; }
        public string Gate_ID { get; set; }
        public string Toy_Number { get; set; }
        public int Total_Planned { get; set; }
        public string Gate_Responsible_ID { get; set; }
        public string Gate_Responsible_Name { get; set; } // Tambahan
        public string Supervisor_ID { get; set; }
        public string Supervisor_Name { get; set; } // Tambahan
        public int Status_ID { get; set; }
    }

    public class DailyPlanSummaryModel
    {
        public int Daily_Plan_ID { get; set; }

        public int Schedule_Detail_ID { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime Finish_Date { get; set; }
        public string Toy_Number { get; set; }
        public int Gate_ID { get; set; }
        public int Total_Planned { get; set; }
        public int Total_Sorted { get; set; }

        public string Gate_Responsible_ID { get; set; }  // <- ubah ke string
        public string Gate_Responsible_Name { get; set; }
        public string Supervisor_ID { get; set; }        // <- ubah ke string
        public string Supervisor_Name { get; set; }

        public int Status_ID { get; set; }
        public string Status_Desc { get; set; } // Tambahan
        public int Detail_Status_ID { get; set; }
        public string Detail_Status_Desc { get; set; }
    }

    public class ToyNumberPlannedModel
    {
        public string ToyNumber { get; set; }
        public int Planned { get; set; }
    }

    public class DailyPlannedTotal
    {
        public int DailyPlanDetail_ID { get; set; }
        public int Total_Planned { get; set; }
    }

    public class DailyPlannedTotalYesterdayToday
    {
        public string PlanDateCategory { get; set; } // Tambahkan ini untuk 'Today' atau 'Yesterday'
        public int Total_Planned { get; set; }
    }

    public class ActiveGateResponsibleAndSupervisor
    {
        public int DailyPlanHead_ID { get; set; }
        public int DailyPlanHead_GateResponsibleID { get; set; }
        public string Gate_Responsible_Name { get; set; }
        public int DailyPlanHead_SupervisorID { get; set; }
        public string Supervisor_Name { get; set; }
        public int Gate_ID { get; set; }
    }
    public class DailyPlanLocationPage
    {
        public int DailyPlanHead_ID { get; set; }
        public int DailyPlanDetail_ID { get; set; }
        public string DailyPlanDetail_ToyNumber { get; set; }
        public int Gate_ID { get; set; }
        public int DailyPlanDetail_TotalPlanned { get; set; }
        public int DailyPlanDetail_StatusID { get; set; }
        public string DailyPlanDetail_StatusDesc { get; set; }
        public int Total_Sorted { get; set; }
        public string Gate_Responsible_ID { get; set; }
        public string Gate_Responsible_Name { get; set; }
        public int DailyPlanHead_StatusID { get; set; }
        public string DailyPlanHead_StatusDesc { get; set; }
    }

    public class DailyPlanShortDropdown
    {
        public int DailyPlanDetail_ID { get; set; }
        public string DailyPlanDetail_ToyNumber { get; set; }
        public int Gate_ID { get; set; }
        public int DailyPlanDetail_StatusID { get; set; }
        public int DailyPlanHead_StatusID { get; set; }
    }

    public class DailyPlanSummaryWeekly
    {
        public DateTime PlanDate { get; set; }
        public int TotalPlanned { get; set; }
    }
    public class SortedSummaryWeekly
    {
        public DateTime PlanDate { get; set; }
        public int TotalSorted { get; set; }
    }
    public class WeeklyPlanSortedPendingModel
    {
        public DateTime PlanDate { get; set; }
        public int TotalPlanned { get; set; }
        public int TotalSorted { get; set; }
        public int TotalPending { get; set; }
    }

    public class DailyPlanDetailTotalSortedAndTotalPlanned
    {
        public int DailyPlanId { get; set; }
        public int TotalPlanned { get; set; }
        public int TotalSorted { get; set; }
    }

    public class DailyPlanDetailCompletedAndStatusOnProgress
    {
        public int DailyPlanId { get; set; }
        public int TotalPlanned { get; set; }
        public int TotalSorted { get; set; }
        public int StatusID { get; set; }
    }










}