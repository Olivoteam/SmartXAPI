using System;
namespace SmartxAPI.Dtos.Login
{
    public class UserDto
    {
        public int N_UserID { get; set; }
        public string X_UserName { get; set; }

        public string X_UserCategory { get; set; }
        public int N_UserCategoryID { get; set; }

        public DateTime D_LoginDate { get; set; }
        public string X_Language { get; set; }
        public int N_LanguageID { get; set; }

        public bool B_AllBranchesData { get; set; }

        public string X_UserFullName { get; set; }
        public int N_EmpID { get; set; }
        public string X_EmpCode { get; set; }
        public string X_EmpName { get; set; }
        public string X_EmpNameLocale { get; set; }
        public int N_PositionID { get; set; }
        public string X_Position { get; set; }
        public int N_DepartmentID { get; set; }
        public string X_Department { get; set; }
        public int N_SalesmanID { get; set; }
        public string X_SalesmanCode { get; set; }
        public string X_SalesmanName { get; set; }
        public string X_UserCategoryIDList { get; set; }
        public bool B_AllowEdit { get; set; }

    }
}
