using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace LeanForgeVision.Models
{
    public class EmployeeModel
    {
        public string Employee_ID { get; set; } // Ubah dari int ke string
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime? Birth_Date { get; set; } // Nullable untuk menangani NULL di database

        [NotMapped] // For Login Credentials
        public string Password { get; set; }
    }
    public class UserModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime Created_Date { get; set; }
        public int Role_ID { get; set; }

        public string RoleName { get; set; }
        public string User_ID { get; set; }
    }

    public class UserRoleModel
    {
        public int Role_ID { get; set; }
        public string Role_Name { get; set; }
        public string Role_Desc { get; set; }
    }



}