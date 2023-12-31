using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using SmartxAPI.GeneralFunctions;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Net.Mail;
using System.Net.Http;
using System.Threading;
namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("timesheet")]
    [ApiController]



    public class Ess_TimeSheet : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int N_FormID = 1305;
        private static TimeZoneInfo India_Standard_Time = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");


        public Ess_TimeSheet(IDataAccessLayer dl, IApiFunctions apiFun, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            api = apiFun;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }

        // [HttpGet("details")]
        // public ActionResult GetAttendanceDetails(string xPayRunID, int nEmployeeID)
        // {
        //     DataTable Master = new DataTable();
        //     DataTable Detail = new DataTable();
        //     DataSet ds = new DataSet();
        //     SortedList Params = new SortedList();
        //     SortedList QueryParams = new SortedList();

        //     int companyid = myFunctions.GetCompanyID(User);

        //     QueryParams.Add("@nCompanyID", companyid);
        //     QueryParams.Add("@xPayRunID", xPayRunID);
        //     QueryParams.Add("@nEmployeeID", nEmployeeID);
        //     string Condition = "";
        //     string _sqlQuery = "";
        //     try
        //     {
        //         using (SqlConnection connection = new SqlConnection(connectionString))
        //         {
        //             connection.Open();
        //             Condition = "Pay_TimeSheetMaster.N_EmpID=@nEmployeeID AND Pay_TimeSheetMaster.x_PayrunText=@xPayRunID";

        //             _sqlQuery = "SELECT  case when Pay_TimeSheet.n_TotalWorkHour > 0 then 'P' else 'A' end as X_Status,Pay_TimeSheetMaster.N_TimeSheetID, Pay_TimeSheetMaster.N_EmpID, Pay_TimeSheetMaster.X_PayrunText, Pay_TimeSheetMaster.D_DateFrom, Pay_TimeSheetMaster.D_DateTo, Pay_TimeSheetMaster.N_TotalDutyHours, Pay_TimeSheetMaster.N_TotalWorkedDays, Pay_TimeSheet.D_In,Pay_TimeSheet.D_Out, Pay_TimeSheet.D_Shift2_In, Pay_TimeSheet.D_Shift2_Out, Pay_TimeSheet.N_Status, Pay_TimeSheet.N_DutyHours,Pay_TimeSheet.N_diff,CONVERT(VARCHAR ,Pay_TimeSheet.D_Date, 106) as D_Date,round(Pay_TimeSheet.N_TotalWorkHour,2) as N_TotalWorkHour,* FROM Pay_TimeSheetMaster INNER JOIN Pay_TimeSheet ON Pay_TimeSheetMaster.N_TimeSheetID = Pay_TimeSheet.N_TimeSheetID AND Pay_TimeSheetMaster.N_CompanyID = Pay_TimeSheet.N_CompanyID Where " + Condition + "";

        //             Master = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);

        //             Master = api.Format(Master, "master");
        //             if (Master.Rows.Count == 0)
        //             {
        //                 return Ok(api.Notice("No Results Found"));
        //             }
        //             else
        //             {
        //                 ds.Tables.Add(Master);
        //                 return Ok(api.Success(ds));
        //             }
        //         }

        //     }
        //     catch (Exception e)
        //     {
        //         return Ok(api.Error(User,e));
        //     }
        // }

        [HttpGet("details")]
        public ActionResult GetAttendanceDetails(int nEmployeeID, int nFnYear, string payText, DateTime payDate, DateTime dDateFrom, DateTime dDateTo)
        {
            DataTable Details = new DataTable();


            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();

            int companyid = myFunctions.GetCompanyID(User);





            // string Condition = "";
            // string _sqlQuery = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    // Condition = "Pay_TimeSheetMaster.N_EmpID=@nEmployeeID AND Pay_TimeSheetMaster.x_PayrunText=@xPayRunID";

                    // _sqlQuery = "SELECT  case when Pay_TimeSheet.n_TotalWorkHour > 0 then 'P' else 'A' end as X_Status,Pay_TimeSheetMaster.N_TimeSheetID, Pay_TimeSheetMaster.N_EmpID, Pay_TimeSheetMaster.X_PayrunText, Pay_TimeSheetMaster.D_DateFrom, Pay_TimeSheetMaster.D_DateTo, Pay_TimeSheetMaster.N_TotalDutyHours, Pay_TimeSheetMaster.N_TotalWorkedDays, Pay_TimeSheet.D_In,Pay_TimeSheet.D_Out, Pay_TimeSheet.D_Shift2_In, Pay_TimeSheet.D_Shift2_Out, Pay_TimeSheet.N_Status, Pay_TimeSheet.N_DutyHours,Pay_TimeSheet.N_diff,CONVERT(VARCHAR ,Pay_TimeSheet.D_Date, 106) as D_Date,round(Pay_TimeSheet.N_TotalWorkHour,2) as N_TotalWorkHour,* FROM Pay_TimeSheetMaster INNER JOIN Pay_TimeSheet ON Pay_TimeSheetMaster.N_TimeSheetID = Pay_TimeSheet.N_TimeSheetID AND Pay_TimeSheetMaster.N_CompanyID = Pay_TimeSheet.N_CompanyID Where " + Condition + "";


                    // DateTime fromDate=new DateTime();
                    // DateTime toDate=new DateTime();

                    DateTime fromDate = dDateFrom;
                    DateTime toDate = dDateTo;

                    DateTime dtStartDate = new DateTime(payDate.Year, payDate.Month, 1);

                    int days = 0;
                    if (payText != null && payText != "")
                    {
                        object PeriodType = dLayer.ExecuteScalar("Select X_Value from Gen_Settings Where X_Description ='Period Settings' and N_CompanyID= " + companyid + " and X_Group='Payroll'", connection);
                        object Periodvalue = dLayer.ExecuteScalar("Select N_Value from Gen_Settings Where X_Description ='Period Settings' and N_CompanyID= " + companyid + " and X_Group='Payroll'", connection);
                        if (Periodvalue == null) return Ok(api.Notice("No Results Found"));

                        if (PeriodType != null && PeriodType.ToString() == "M")
                        {
                            days = DateTime.DaysInMonth(payDate.Year, payDate.Month) - myFunctions.getIntVAL(Periodvalue.ToString());
                            toDate = dtStartDate.AddDays(myFunctions.getIntVAL(Periodvalue.ToString()) - 2);
                            int lastdays = myFunctions.getIntVAL(Periodvalue.ToString());
                            fromDate = dtStartDate.AddMonths(-1).AddDays(lastdays - 1);
                        }
                        else
                        {
                            days = DateTime.DaysInMonth(payDate.Year, payDate.Month) - myFunctions.getIntVAL(Periodvalue.ToString());
                            toDate = dtStartDate.AddDays(myFunctions.getIntVAL(days.ToString()) - 1);
                            int lastdays = myFunctions.getIntVAL(Periodvalue.ToString());
                            dDateFrom = dtStartDate.AddDays(-lastdays);
                        }
                    }

                    //DateTime.ParseExact(fromDate.ToString(), "yyyy-MM-dd HH:mm:ss:fff", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                    //DateTime.ParseExact(toDate.ToString(), "yyyy-MM-dd HH:mm:ss:fff", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");;
                    //DateTime.ParseExact(fromDate(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    QueryParams.Add("N_CompanyID", companyid);
                    QueryParams.Add("N_FnYear", nFnYear);
                    QueryParams.Add("D_DateFrom", fromDate);
                    QueryParams.Add("D_DateTo", toDate);
                    QueryParams.Add("N_EmpID", nEmployeeID);

                    SortedList OutPut = new SortedList();
                    SortedList Master = new SortedList();
                    Master.Add("fromDate", fromDate);
                    Master.Add("toDate", toDate);
                    Master.Add("days", days);
                    //DateTime.ParseExact(fromDate.ToString(), "yyyy-MM-dd HH:mm:ss:fff", System.Globalization.CultureInfo.InvariantCulture);

                    // DateTime.ParseExact(fromDate.ToString(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    // DateTime.ParseExact(toDate.ToString(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    Details = dLayer.ExecuteDataTablePro("SP_Pay_TimeSheet", QueryParams, connection);



                    if (Details.Rows.Count > 0)
                    {


                        var rows = Details.AsEnumerable().Where(r => r.Field<int>("B_HolidayFlag") == 1 || r.Field<int>("B_IsVacation") == 1);

                        DataTable HolyDays = Details.Clone();

                        foreach (var row in rows)
                        {
                            HolyDays.ImportRow(row);
                        }


                        foreach (DataRow row in HolyDays.Rows)
                        {

                            for (int i = Details.Rows.Count - 1; i >= 0; i--)
                            {
                                if (row["d_date"].ToString() == Details.Rows[i]["d_date"].ToString() && (Details.Rows[i]["B_HolidayFlag"].ToString() == "0" && row["B_HolidayFlag"].ToString() == "1"))
                                {
                                    Details.Rows[i].Delete();
                                    Details.AcceptChanges();
                                }
                                if (row["d_date"].ToString() == Details.Rows[i]["d_date"].ToString() && (Details.Rows[i]["B_IsVacation"].ToString() == "0" && row["B_IsVacation"].ToString() == "1"))
                                {
                                    Details.Rows[i].Delete();
                                    Details.AcceptChanges();
                                }
                                Details.AcceptChanges();
                            }
                        }
                    }
                    Details.AcceptChanges();
                    Double N_WorkHours = 0, N_WorkdHrs = 0, N_Deduction = 0, N_compensated = 0, NetDeduction = 0, Addition = 0, ExtraHour = 0;
                    if (Details.Rows.Count == 0)
                    {
                        return Ok(api.Notice("No Results Found"));
                    }
                    else
                    {
                        foreach (DataRow row in Details.Rows)
                        {

                            try
                            {
                                string d_shift2_Out = row["d_shift2_Out"].ToString(); //row.GetColumnError("d_shift2_Out") ;
                                string d_out = row["d_out"].ToString();
                                if (d_shift2_Out != null && d_shift2_Out.ToString() != "00:00:00" || d_out != null && d_out.ToString() != "00:00:00")
                                {
                                    // Summary
                                    N_WorkHours += HoursToMinutes(Convert.ToDouble(row["N_Workhours"].ToString()));
                                    N_Deduction += HoursToMinutes(Convert.ToDouble(row["Deduction"].ToString()));
                                    N_WorkdHrs += HoursToMinutes(Convert.ToDouble(row["N_Tothours"].ToString()));
                                    N_compensated += HoursToMinutes(Convert.ToDouble(row["CompMinutes"].ToString()));
                                }
                                else
                                {

                                    row["N_CompAdd"] = 0;
                                    row["N_CompLess"] = 0;
                                    row["N_Diff"] = 0;
                                    row["N_DutyHours"] = 0;
                                    row["N_TotHours"] = 0;
                                    row["deduction"] = 0;
                                    row["compMinutes"] = 0;
                                }
                            }
                            catch (Exception e)
                            {
                                row["N_CompAdd"] = 0;
                                row["N_CompLess"] = 0;
                                row["N_Diff"] = 0;
                                row["N_DutyHours"] = 0;
                                row["N_TotHours"] = 0;
                                row["deduction"] = 0;
                                row["compMinutes"] = 0;
                            }

                        }
                        Master.Add("TotalWorkingHours", MinutesToHours(N_WorkHours).ToString("0.00"));
                        Master.Add("TotalWorkedHours", MinutesToHours(N_WorkdHrs).ToString("0.00"));
                        Master.Add("Compensated", MinutesToHours(N_compensated).ToString("0.00"));
                        Master.Add("DirectDeduction", MinutesToHours(N_Deduction).ToString("0.00"));
                        Details = api.Format(Details, "master");
                        OutPut.Add("master", Master);
                        OutPut.Add("details", Details);
                        return Ok(api.Success(OutPut));
                    }
                }

            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }

        public double MinutesToHours(double Minutes)
        {
            double Hours = 0;
            //Minutes = Round(Minutes, 2);
            Hours = (((int)Minutes / 60) + (Minutes % 60) / 100);
            return Hours;
        }
        public double HoursToMinutes(double Hours)
        {
            double Minutes = 0;
            Minutes = (((int)Hours * 60) + (Hours % 1) % .60 * 100);
            return Minutes;
        }

        [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                MasterTable = ds.Tables["master"];
                int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyId"].ToString());
                int nFnYearId = myFunctions.getIntVAL(MasterTable.Rows[0]["n_FnYearId"].ToString());
                int nEmpID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_EmpID"].ToString());
                string userIDIndex = MasterTable.Rows[0]["userIDIndex"].ToString();
                string Punchtype = MasterTable.Rows[0]["punchType"].ToString();

                if (MasterTable.Columns.Contains("Punchtype"))
                    MasterTable.Columns.Remove("Punchtype");
                if (MasterTable.Columns.Contains("n_CompanyId"))
                    MasterTable.Columns.Remove("n_CompanyId");
                if (MasterTable.Columns.Contains("n_FnYearId"))
                    MasterTable.Columns.Remove("n_FnYearId");
                if (MasterTable.Columns.Contains("n_EmpID"))
                    MasterTable.Columns.Remove("n_EmpID");


                DataRow masterRow = MasterTable.Rows[0];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    int nTimesheetID = 0;
                    string defultTime = "00:00:00";
                    string currentTime = DateTime.Now.ToString("HH:mm:ss");
                    //DateTime date = DateTime.Now;

                    object TimezoneID = dLayer.ExecuteScalar("select isnull(n_timezoneid,82) from acc_company where N_CompanyID= " + nCompanyID, connection, transaction);
                    object Timezone = dLayer.ExecuteScalar("select X_ZoneName from Gen_TimeZone where n_timezoneid=" + TimezoneID, connection, transaction);

                    DateTime dateTime_Indian = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, India_Standard_Time);
                    DateTime date = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, Timezone.ToString());

                    masterRow["transactionTime"] = date.ToString();
                    masterRow["serverRecordTime"] = date.ToString();

                    MasterTable.AcceptChanges();

                    nTimesheetID = dLayer.SaveData("Pay_TimeSheetLog", "indexKey", MasterTable, connection, transaction);


                    if (nTimesheetID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User, "Unable to save"));
                    }
                    else
                    {

                        SortedList postingParams = new SortedList();
                        postingParams.Add("D_Date", date);
                        postingParams.Add("N_UserID", userIDIndex);
                        postingParams.Add("N_CompanyID", nCompanyID);
                        dLayer.ExecuteScalarPro("SP_Pay_TimesheetLog", postingParams, connection, transaction);

                        //Time Updates
                        if (Punchtype == "IN")
                            dLayer.ExecuteNonQuery("update Pay_TimeSheetImport set D_Out='00:00:00.0000000' where N_CompanyID=" + nCompanyID + " and N_EmpID=" + nEmpID + " and D_Date='" + date.ToString("yyyy-M-dd") + "'", Params, connection, transaction);
                        if (Punchtype == "OUT")
                            dLayer.ExecuteNonQuery("update Pay_TimeSheetImport set D_Out='" + date.ToString() + "' where N_CompanyID=" + nCompanyID + " and N_EmpID=" + nEmpID + " and D_Date='" + date.ToString("yyyy-M-dd") + "'", Params, connection, transaction);

                        //Whatsapp
                        object N_WhatsappMSG = dLayer.ExecuteScalar("select N_Value from Gen_Settings where N_CompanyID=" + myFunctions.GetCompanyID(User) + " and X_Group='1334' and X_Description='Whatsapp Message'", Params, connection, transaction);
                        if (N_WhatsappMSG != null)
                        {
                            if (N_WhatsappMSG.ToString() == "1")
                            {
                                string Company = myFunctions.GetCompanyName(User);
                                object WhatsappAPI = dLayer.ExecuteScalar("select X_Value from Gen_Settings where N_CompanyID=" + myFunctions.GetCompanyID(User) + " and X_Group='1334' and X_Description='Whatsapp Message'", Params, connection, transaction);
                                object Employee = dLayer.ExecuteScalar("select x_empname from vw_PayEmployee where n_companyid=" + myFunctions.GetCompanyID(User) + " and  n_empid=" + nEmpID, Params, connection, transaction);
                                object Receip = dLayer.ExecuteScalar("select x_phone1 from acc_company where n_companyid=" + myFunctions.GetCompanyID(User), Params, connection, transaction);
                                string body = "";
                                if (Punchtype == "IN")
                                    body = Employee + "%0AIN @ " + date.ToString("hh:mm tt");
                                if (Punchtype == "OUT")
                                    body = Employee + "%0AOUT @ " + date.ToString("hh:mm tt");


                                string URLAPI = "https://api.textmebot.com/send.php?recipient=" + Receip + "&apikey=" + WhatsappAPI + "&text=" + body;
                                var handler = new HttpClientHandler
                                {
                                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }
                                };
                                var client = new HttpClient(handler);
                                var clientFile = new HttpClient(handler);
                                var MSG = client.GetAsync(URLAPI);
                                MSG.Wait();
                                Thread.Sleep(6000);
                                URLAPI = "https://api.textmebot.com/send.php?recipient=+918547686435&apikey=" + WhatsappAPI + "&text=" + body;
                                var MSG1 = client.GetAsync(URLAPI);
                                MSG1.Wait();

                            }
                        }


                        SortedList QueryParams = new SortedList();

                        QueryParams.Add("@nCompanyID", nCompanyID);
                        QueryParams.Add("@nFnYear", nFnYearId);
                        QueryParams.Add("@nDate", date);
                        QueryParams.Add("@nEmpID", nEmpID);
                        string sqlCommandDailyLogin = "SELECT isNull(MAX(D_In),'00:00:00') as D_In,isNull(MAX(D_Out),'00:00:00') as D_Out,Convert(Time, GetDate()) as D_Cur,cast(dateadd(millisecond, datediff(millisecond,MAX(D_In),case when Max(D_Out)='00:00:00.0000000' then  Convert(Time, '" + date + "') else Max(D_Out) end), '19000101')  AS TIME) AS workedHours from Pay_TimeSheetImport  where D_Date=@nDate and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYear and N_EmpID=@nEmpID";

                        // DataTable Details = dLayer.ExecuteDataTable("select * from Pay_TimeSheetImport where D_Date=@nDate and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYear and N_EmpID=@nEmpID", QueryParams, connection, transaction);
                        DataTable Details = dLayer.ExecuteDataTable(sqlCommandDailyLogin, QueryParams, connection, transaction);
                        if (Details.Rows.Count == 0)
                        {
                            transaction.Rollback();
                            return Ok(api.Error(User, "Unable to save"));
                        }
                        else
                        {
                            transaction.Commit();
                            Details = api.Format(Details, "master");
                            return Ok(api.Success(Details, "Your Attendance Marked"));
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User, ex));
            }
        }


        [HttpGet("dayAttendanceDetails")]
        public ActionResult GetDayAttendance(int nEmpID, int nFnYear)
        {
            //DataTable Details = new DataTable();
            //DateTime date = DateTime.Today;

            int companyid = myFunctions.GetCompanyID(User);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    //New Details
                    object TimezoneID = dLayer.ExecuteScalar("select isnull(n_timezoneid,82) from acc_company where N_CompanyID= " + companyid, connection, transaction);
                    object Timezone = dLayer.ExecuteScalar("select X_ZoneName from Gen_TimeZone where n_timezoneid=" + TimezoneID, connection, transaction);

                    DateTime dateTime_Indian = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, India_Standard_Time);
                    DateTime date = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, Timezone.ToString());

                    SortedList QueryParams = new SortedList();
                    QueryParams.Add("@nCompanyID", companyid);
                    QueryParams.Add("@nFnYear", nFnYear);
                    QueryParams.Add("@nDate", date);
                    QueryParams.Add("@nEmpID", nEmpID);
                    string sqlCommandDailyLogin = "SELECT isNull(MAX(D_In),'00:00:00') as D_In,isNull(MAX(D_Out),'00:00:00') as D_Out,Convert(Time, GetDate()) as D_Cur,cast(dateadd(millisecond, datediff(millisecond,MAX(D_In),case when Max(D_Out)='00:00:00.0000000' then  Convert(Time, GetDate()) else Max(D_Out) end), '19000101')  AS TIME) AS workedHours,isNull(MAX(d_shift2_in),'00:00:00') as d_shift2_in,isNull(MAX(D_Shift2_Out),'00:00:00') as D_Shift2_Out  from Pay_TimeSheetImport  where D_Date=@nDate and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYear and N_EmpID=@nEmpID";

                    DataTable Details = dLayer.ExecuteDataTable(sqlCommandDailyLogin, QueryParams, connection, transaction);

                    if (Details.Rows.Count == 0)
                    {
                        return Ok(api.Notice("No Results Found"));
                    }
                    else
                    {
                        Details = api.Format(Details, "master");
                        return Ok(api.Success(Details));
                    }
                }

            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }



        [HttpPost("saveWorkLocation")]
        public ActionResult SaveWorkLocation([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                MasterTable = ds.Tables["master"];
                DataRow MasterRow = MasterTable.Rows[0];

                int n_LocationID = myFunctions.getIntVAL(MasterRow["n_LocationID"].ToString());
                int nCompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyID"].ToString());
                string x_LocationCode = MasterRow["x_LocationCode"].ToString();
                bool b_Enableall = myFunctions.getBoolVAL(MasterRow["b_Enableall"].ToString());

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList EmpParams = new SortedList();
                    EmpParams.Add("@nCompanyID", nCompanyID);
                    SortedList Params = new SortedList();


                    if (x_LocationCode == "@Auto")
                    {

                        Params.Add("@nCompanyID", nCompanyID);
                        x_LocationCode = dLayer.ExecuteScalar("Select max(isnull(x_LocationCode,0))+1 as x_LocationCode from Pay_workLocation where N_CompanyID=@nCompanyID", Params, connection, transaction).ToString();
                        if (x_LocationCode == null || x_LocationCode == "") { x_LocationCode = "1"; }
                        MasterTable.Rows[0]["x_LocationCode"] = x_LocationCode;
                    }


                    if (n_LocationID > 0)
                    {
                        dLayer.DeleteData("Pay_workLocation", "n_LocationID", n_LocationID, "", connection, transaction);
                    }

                    MasterTable.Columns.Remove("N_FnYearID");
                    MasterTable.Columns.Remove("N_EmpID");

                    n_LocationID = dLayer.SaveData("Pay_workLocation", "n_LocationID", MasterTable, connection, transaction);
                    if (b_Enableall)
                        dLayer.ExecuteNonQuery("update Pay_employee set x_worklocationid=" + n_LocationID + " where N_CompanyID=" + nCompanyID, Params, connection, transaction);

                    if (n_LocationID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User, "Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("Work Location Saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User, ex));
            }
        }
        [HttpGet("locationList")]
        public ActionResult GetSalaryPayBatch()
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            Params.Add("@nCompanyID", nCompanyID);



            string sqlCommandText = "Select * from Pay_WorkLocation Where  N_CompanyID=@nCompanyID ";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                dt = api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }

        [HttpGet("workLocationList")]
        public ActionResult GetEmpReqList(string xLocationCode, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();
            string sqlCommandCount = "";
            int nUserID = myFunctions.GetUserID(User);
            int nCompanyID = myFunctions.GetCompanyID(User);
            QueryParams.Add("@nCompanyID", nCompanyID);
            QueryParams.Add("@nUserID", nUserID);
            string sqlCommandText = "";
            int Count = (nPage - 1) * nSizeperpage;
            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (x_LocationCode like'%" + xSearchkey + "%'or x_LocationName like'%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by x_LocationCode desc";
            else
                xSortBy = " order by " + xSortBy;

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from Pay_workLocation where   N_CompanyID=@nCompanyID " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from Pay_workLocation where  N_CompanyID=@nCompanyID " + Searchkey + " and n_LocationID not in (select top(" + Count + ") n_LocationID from Pay_workLocation where N_CompanyID=@nCompanyID " + xSortBy + " ) " + xSortBy;

            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    dt = dLayer.ExecuteDataTable(sqlCommandText, QueryParams, connection);
                    sqlCommandCount = "select count(1) as N_Count from Pay_workLocation where N_CompanyID=@nCompanyID " + Searchkey + "";
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, QueryParams, connection);

                    if (dt.Rows.Count > 0)
                    {
                        dt = myFunctions.AddNewColumnToDataTable(dt, "longitude", typeof(string), "");
                        dt = myFunctions.AddNewColumnToDataTable(dt, "latitude", typeof(string), "");
                        dt = myFunctions.AddNewColumnToDataTable(dt, "radius", typeof(string), "");
                        foreach (DataRow dRow in dt.Rows)
                        {
                            if (dRow["x_GeoLocation"].ToString() != "" && dRow["x_GeoLocation"].ToString() != null)
                            {
                                JObject o = JObject.Parse(dRow["x_GeoLocation"].ToString());

                                dRow["longitude"] = (string)o["lng"];
                                dRow["latitude"] = (string)o["lat"];
                                dRow["radius"] = (string)o["radius"];
                            }
                        }
                    }
                    OutPut.Add("Details", api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);



                }
                dt = api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(api.Success(OutPut));
                }

            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }

        [HttpGet("timesheetLog")]
        public ActionResult GetTimesheetLog(string xEmpCode, DateTime dEventDate)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            Params.Add("@nEmpCode", xEmpCode);
            Params.Add("@dEventDate", dEventDate);
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nCompanyID", nCompanyID);
            // string sqlCommandText="select * from Pay_TimesheetReport where N_CompanyID=@nCompanyID and N_EmpID=@nEmpID and D_EventDate=@dEventDate order by D_In,D_Out";
            string sqlCommandText = "select UserID as X_EmpCode ,cast(TransactionTime as date) as date,CAST(TransactionTime AS TIME) as time from Pay_TimesheetLog where cast(TransactionTime as date)=@dEventDate and UserID=@nEmpCode and N_CompanyID=@nCompanyID order by TransactionTime asc";
            SortedList OutPut = new SortedList();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    string sqlCommandCount = "select count(1) as N_Count from Pay_TimesheetLog where cast(TransactionTime as date)=@dEventDate and UserID=@nEmpCode";
                    DataTable Summary = dLayer.ExecuteDataTable(sqlCommandCount, Params, connection);
                    string TotalCount = "0";

                    if (Summary.Rows.Count > 0)
                    {
                        DataRow drow = Summary.Rows[0];
                        TotalCount = drow["N_Count"].ToString();

                    }
                    OutPut.Add("Details", api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                }
                dt = api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(api.Success(OutPut));
                }
            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }



    }

}