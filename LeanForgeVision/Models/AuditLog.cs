using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LeanForgeVision.Models
{
    public class AuditLogModel
    {
        public int AuditLog_ID { get; set; } // Otomatis oleh DB
        public string AuditLog_TableName { get; set; }
        public string AuditLog_RecordID { get; set; }
        public int AuditLog_ActionTypeID { get; set; }
        public string AuditLog_ChangedBy { get; set; }
        public DateTime AuditLog_ChangedAt { get; set; } = DateTime.Now;
    }

}