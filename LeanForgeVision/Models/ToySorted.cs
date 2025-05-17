using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeanForgeVision.Models
{
    public class ToySorted
    {
        public string Toy_Number { get; set; }  // Bisa null
        public DateTime? Sorted_At { get; set; } // Bisa null
        public int? Sorting_Method { get; set; }
        public string Sorthing_MethodName { get; set; }  // Bisa null
        public int? Daily_Plan_ID { get; set; }  // Bisa null
    }
    public class ToySortedToday
    {
        public int TotalToySortedToday { get; set; }
    }

    public class ToySortedTotalYesterdayToday
    {
        public string PlanDateCategory { get; set; }
        public int TotalSorted { get; set; }
    }

    public class ToyNumberModel
    {
        public string Toy_Number { get; set; }
        public string Toy_Description { get; set; }
    }


}