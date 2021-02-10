using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using SmartxAPI.GeneralFunctions;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Collections.Generic;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("employee")]
    [ApiController]
    public class Pay_EmployeeMaster : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int FormID;


        public Pay_EmployeeMaster(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 188;
        }

        [HttpGet("list")]
        public ActionResult GetEmployeeList(int? nCompanyID, int nFnYearID, bool bAllBranchData, int nBranchID, int nEmpID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnYearID);
            Params.Add("@bAllBranchData", bAllBranchData);
            Params.Add("@nBranchID", nBranchID);
            Params.Add("@nEmpID", nEmpID);
            string sqlCommandText = "";
            string projectFilter = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    int filterByProject = myFunctions.getIntVAL(dLayer.ExecuteScalar("select isNull(N_Value,0) as val from gen_settings where x_Group='HR' and x_Description='FilterDelegateEmployeeByProject' and n_CompanyID=" + nCompanyID, connection).ToString());
                    if (nEmpID > 0 && filterByProject > 0)
                        projectFilter = " and N_ProjectID =(select max(isNull(N_ProjectID,0)) from vw_PayEmployee_Disp where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID ) and n_EmpID<>@nEmpID ";
                    if (bAllBranchData == true)
                        sqlCommandText = "Select N_CompanyID,N_EmpID,N_BranchID,N_FnYearID,[Employee Code] as X_EmpCode,Name as X_EmpName,X_Position,X_Department,X_BranchName from vw_PayEmployee_Disp Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + projectFilter + "  group by N_CompanyID,N_EmpID,N_BranchID,N_FnYearID,[Employee Code],Name,X_Position,X_Department,X_BranchName";
                    else
                        sqlCommandText = "Select N_CompanyID,N_EmpID,N_BranchID,N_FnYearID,[Employee Code] as X_EmpCode ,Name as X_EmpName,X_Position,X_Department,X_BranchName from vw_PayEmployee_Disp Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and (N_BranchID=0 or N_BranchID=@nBranchID)  " + projectFilter + "   group by N_CompanyID,N_EmpID,N_BranchID,N_FnYearID,[Employee Code],Name,X_Position,X_Department,X_BranchName";


                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }


        [HttpGet("details")]
        public ActionResult GetEmployeeDetails(string xEmpCode, int nFnYearID, bool bAllBranchData, int nBranchID)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable Pay_Employee, Pay_EmpAddlInfo, pay_EmployeeDependence, pay_EmployeeAlerts, acc_OtherInformation, pay_EmpAccruls, pay_EmployeePayHistory, pay_PaySetup, pay_EmployeeSub,pay_Getsalary;

            SortedList Result = new SortedList();
            SortedList Params = new SortedList();
            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnYearID);
            Params.Add("@bAllBranchData", bAllBranchData);
            Params.Add("@nBranchID", nBranchID);
            Params.Add("@xEmpCode", xEmpCode);

            string branchSql = bAllBranchData == false ? " and (vw_PayEmployee.N_BranchID=0 or vw_PayEmployee.N_BranchID=@nBranchID" : "";
            string EmployeeSql = "Select X_LedgerName_Ar As X_LedgerName,[Loan Ledger Name_Ar] As [Loan Ledger Name], *,Pay_Employee.X_EmpName AS X_ReportTo,CASE WHEN dbo.Pay_VacationDetails.D_VacDateFrom<=CONVERT(date, GETDATE()) AND dbo.Pay_VacationDetails.D_VacDateTo>=CONVERT(date, GETDATE()) AND dbo.Pay_VacationDetails.N_VacDays<0 and dbo.Pay_VacationDetails.B_IsSaveDraft=0 Then '1' Else vw_PayEmployee.N_Status end AS [Status]  from vw_PayEmployee Left Outer Join Pay_Supervisor On vw_PayEmployee.N_ReportToID= Pay_Supervisor.N_SupervisorID Left Outer Join Pay_Employee On Pay_Supervisor.N_EmpID=Pay_Employee.N_EmpID Left Outer Join  dbo.Pay_VacationDetails ON vw_PayEmployee.N_EmpID = dbo.Pay_VacationDetails.N_EmpID AND dbo.Pay_VacationDetails.D_VacDateFrom <= CONVERT(date, GETDATE()) AND dbo.Pay_VacationDetails.D_VacDateTo >=CONVERT(date, GETDATE()) AND dbo.Pay_VacationDetails.N_VacDays<0  Where vw_PayEmployee.N_CompanyID=@nCompanyID and vw_PayEmployee.N_FnYearID=@nFnYearID and vw_PayEmployee.X_EmpCode=@xEmpCode " + branchSql;
            string contactSql = "Select * from vw_ContactDetails where N_CompanyID =@nCompanyID and N_EmpID=@nEmpID";
            string salarySql = "Select *,(Select COUNT(*) from Pay_PaymentDetails Where N_CompanyID = vw_EmpPayInformation.N_CompanyID AND N_EmpID = vw_EmpPayInformation.N_EmpID AND N_PayID = vw_EmpPayInformation.N_PayID AND N_Value = vw_EmpPayInformation.N_value ) AS N_NoEdit from vw_EmpPayInformation Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_EmpID=@nEmpID";
            string accrualSql = "Select *,(Select COUNT(*) from Pay_VacationDetails Where N_CompanyID = vw_Pay_EmployeeAccrul.N_CompanyID AND N_EmpID = vw_Pay_EmployeeAccrul.N_EmpID AND N_VacTypeID = vw_Pay_EmployeeAccrul.N_VacTypeID ) AS N_NoEdit from vw_Pay_EmployeeAccrul Where N_CompanyID=@nCompanyID  and N_EmpID=@nEmpID";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Pay_Employee = dLayer.ExecuteDataTable(EmployeeSql, Params, connection);

                    Pay_Employee = _api.Format(Pay_Employee);
                    if (Pay_Employee.Rows.Count == 0)
                    {
                        return Ok(_api.Notice("No Results Found"));
                    }
                    else
                    {
                        Params.Add("@nEmpID", Pay_Employee.Rows[0]["N_EmpID"].ToString());
                        pay_EmployeeSub = dLayer.ExecuteDataTable(contactSql, Params, connection);
                        pay_Getsalary = dLayer.ExecuteDataTable(salarySql, Params, connection);
                        pay_EmpAccruls = dLayer.ExecuteDataTable(accrualSql, Params, connection);
                        
                        Result.Add("pay_Employee", Pay_Employee);
                        Result.Add("pay_EmployeeSub", pay_EmployeeSub);
                        Result.Add("pay_Getsalary", pay_Getsalary);
                        Result.Add("pay_EmpAccruls", pay_EmpAccruls);
                        return Ok(_api.Success(Result));
                    }
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }

        [HttpGet("default")]
        public ActionResult GetEmployeeDefault(int nFnYearID, int nBranchID)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable pay_Codes, pay_benifits, pay_EmpAccruls, pay_OtherInfo, pay_PaySetup;

            SortedList Result = new SortedList();
            SortedList Params = new SortedList();
            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnYearID);
            Params.Add("@nBranchID", nBranchID);


            string accrualSql = " select N_vacTypeID,Name,N_Accrued,X_Type,X_Period from [vw_PayAccruedCode_List] Where N_CompanyID=@nCompanyID order by X_Type desc";
            string paySetupSql = "Select * from vw_PayMaster Where  N_CompanyID=@nCompanyID  and (N_PayTypeID <>11 and N_PayTypeID <>12 and N_PayTypeID <>14) and N_FnYearID=@nFnYearID  and N_PaymentID=5 and (N_Paymethod=0 or N_Paymethod=3) and B_InActive=0";
            string payBenifitsSql = "Select * from vw_PayMaster Where  N_CompanyID=@nCompanyID and  N_FnYearID=@nFnYearID and (N_PaymentID=6 or N_PaymentID=7 )and N_PaytypeID<>14  and (N_Paymethod=0 or N_Paymethod=3)";
            string PayCodeSql ="Select * From [vw_Pay_Sal4perPaycodes] Where N_CompanyID=@nCompanyID and N_FnyearID =@nFnYearID";
            string payOthInfoSql = "Select N_OtherCode,X_subject from Acc_OtherInformationMaster Where  N_CompanyID=@nCompanyID and  N_FormID=188";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    pay_PaySetup = dLayer.ExecuteDataTable(paySetupSql, Params, connection);
                    pay_EmpAccruls = dLayer.ExecuteDataTable(accrualSql, Params, connection);
                    pay_benifits  = dLayer.ExecuteDataTable(payBenifitsSql, Params, connection);
                    pay_Codes = dLayer.ExecuteDataTable(PayCodeSql, Params, connection);
                    pay_OtherInfo = dLayer.ExecuteDataTable(payOthInfoSql, Params, connection);
                    pay_PaySetup = _api.Format(pay_PaySetup);
                    pay_EmpAccruls = _api.Format(pay_EmpAccruls);
                    pay_benifits = _api.Format(pay_benifits);
                    pay_Codes = _api.Format(pay_Codes);
                    pay_OtherInfo = _api.Format(pay_OtherInfo);
                    Result.Add("pay_PaySetup", pay_PaySetup);
                        Result.Add("pay_EmpAccruls", pay_EmpAccruls);
                        Result.Add("pay_benifits", pay_benifits);
                        Result.Add("pay_Codes", pay_Codes);
                        Result.Add("pay_OtherInfo", pay_OtherInfo);

                        return Ok(_api.Success(Result));
                   
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }


        [HttpGet("dashboardList")]
        public ActionResult GetEmployeeDashboardList(int nFnYearID, bool bAllBranchData, int nBranchID, int EmpStatus, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnYearID);

            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandCount = "";
            string sqlCommandText = "";
            string Searchkey = "";
            string Criteria = " where N_CompanyID =@nCompanyID and N_FnYearID =@nFnYearID ";

            if (bAllBranchData == false)
            {
                Criteria = Criteria + " and N_BranchID=@nBranchID ";
                Params.Add("@nBranchID", nBranchID);
            }

            if (EmpStatus == 0)
                Criteria = Criteria + " and N_Status<>3 and N_Status<>2 ";
            else if (EmpStatus == 1)
                Criteria = Criteria + " and N_Status =3 or N_Status =2 ";



            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and X_EmployeeCode like '%" + xSearchkey + "%' or X_EmployeeName like '%" + xSearchkey + "%' ";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by X_EmployeeCode desc";
            else
                xSortBy = " order by " + xSortBy;

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") N_CompanyID,N_FnYearID,N_Branchid,B_Inactive,N_EmpID,N_Status,N_EmpTypeID,X_EmployeeCode,X_EmployeeName,X_Position,X_Department,X_BranchName,D_HireDate,X_TypeName,X_Nationality,X_IqamaNo,X_Sex,X_PhoneNo,N_TicketCount from vw_PayEmployee_Dashboard " + Criteria + Searchkey + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") N_CompanyID,N_FnYearID,N_Branchid,B_Inactive,N_EmpID,N_Status,N_EmpTypeID,X_EmployeeCode,X_EmployeeName,X_Position,X_Department,X_BranchName,D_HireDate,X_TypeName,X_Nationality,X_IqamaNo,X_Sex,X_PhoneNo,N_TicketCount from vw_PayEmployee_Dashboard " + Criteria + Searchkey + " and N_EmpID not in (select top(" + Count + ") N_EmpID from vw_PayEmployee_Dashboard " + Criteria + Searchkey + xSortBy + " ) " + xSortBy;

            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(*) as N_Count  from vw_PayEmployee_Dashboard where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + Searchkey;
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("Details", _api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                    if (dt.Rows.Count == 0)
                    {
                        return Ok(_api.Warning("No Results Found"));
                    }
                    else
                    {
                        return Ok(_api.Success(OutPut));
                    }
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }


        //Save....
        [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {

                DataTable dtMasterTable, dtPay_EmpAddlInfo, dtpay_EmployeeDependence, dtpay_EmployeeAlerts, dtacc_OtherInformation, dtpay_EmpAccruls, dtpay_EmployeePayHistory, dtpay_PaySetup, dtpay_EmployeeSub, dtPay_Employee_Log, dtInv_Salesman, dtVeh_Drivers,dtSch_Teacher,dtPay_EmployeeEducation,dtPay_EmploymentHistory;
                // if(ds.Tables.Contains("pay_Employee"))
                dtMasterTable = ds.Tables["pay_Employee"];
                // if(ds.Tables.Contains("pay_EmpAddlInfo"))
                dtPay_EmpAddlInfo = ds.Tables["pay_EmpAddlInfo"];
                // if(ds.Tables.Contains("pay_EmployeeDependence"))
                dtpay_EmployeeDependence = ds.Tables["pay_EmployeeDependence"];
                // if(ds.Tables.Contains("pay_EmployeeAlerts"))
                dtpay_EmployeeAlerts = ds.Tables["pay_EmployeeAlerts"];
                // if(ds.Tables.Contains("acc_OtherInformation"))
                dtacc_OtherInformation = ds.Tables["acc_OtherInformation"];
                // if(ds.Tables.Contains("pay_EmpAccruls"))
                dtpay_EmpAccruls = ds.Tables["pay_EmpAccruls"];
                // if(ds.Tables.Contains("pay_EmployeePayHistory"))
                dtpay_EmployeePayHistory = ds.Tables["pay_EmployeePayHistory"];
                // if(ds.Tables.Contains("pay_PaySetup"))
                dtpay_PaySetup = ds.Tables["pay_PaySetup"];
                // if(ds.Tables.Contains("pay_EmployeeSub"))
                dtpay_EmployeeSub = ds.Tables["pay_EmployeeSub"];
                dtPay_Employee_Log = ds.Tables["Pay_Employee_Log"];
                dtInv_Salesman = ds.Tables["Inv_Salesman"];
                dtVeh_Drivers = ds.Tables["Veh_Drivers"];
                dtSch_Teacher = ds.Tables["Sch_Teacher"];
                dtPay_EmployeeEducation = ds.Tables["Pay_EmployeeEducation"];
                dtPay_EmploymentHistory = ds.Tables["Pay_EmploymentHistory"];


                int nCompanyID = myFunctions.getIntVAL(dtMasterTable.Rows[0]["n_CompanyID"].ToString());
                int nEmpID = myFunctions.getIntVAL(dtMasterTable.Rows[0]["n_EmpID"].ToString());
                int nSavedEmpID = myFunctions.getIntVAL(dtMasterTable.Rows[0]["n_EmpID"].ToString());
                int nFnYearID = myFunctions.getIntVAL(dtMasterTable.Rows[0]["n_FnYearID"].ToString());
                int nDepartmentID = myFunctions.getIntVAL(dtMasterTable.Rows[0]["n_DepartmentID"].ToString());
                string xEmpCode = dtMasterTable.Rows[0]["x_EmpCode"].ToString();
                string xEmpName = dtMasterTable.Rows[0]["x_EmpName"].ToString();
                int nUserID = myFunctions.GetUserID(User);
                string X_BtnAction = "";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    SortedList QueryParams = new SortedList();

                    QueryParams.Add("@nCompanyID", nCompanyID);
                    QueryParams.Add("@nFnYearID", nFnYearID);
                    QueryParams.Add("@nFormID", this.FormID);
                    QueryParams.Add("@nPositionID", myFunctions.getIntVAL(dtMasterTable.Rows[0]["n_PositionID"].ToString()));

                    // Auto Gen
                    if (xEmpCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", this.FormID);
                        xEmpCode = dLayer.GetAutoNumber("pay_Employee", "x_EmpCode", Params, connection, transaction);
                        if (xEmpCode == "") { transaction.Rollback();return Ok(_api.Error("Unable to generate Employee Code")); }
                        dtMasterTable.Rows[0]["x_EmpCode"] = xEmpCode;
                        X_BtnAction = "INSERT";
                    }
                    else
                    {
                        //dLayer.DeleteData("pay_Employee", "n_EmpID", nEmpID, "", connection, transaction);
                        X_BtnAction = "UPDATE";

                    }

                    string DupCriteria = "N_CompanyID=" + nCompanyID + " and N_FnYearID =" + nFnYearID + " and X_EmpCode='" + xEmpCode.Trim() + "'";
                    string X_Crieteria = "N_CompanyID=" + myCompanyID._CompanyID + " and N_FnYearID =" + myCompanyID._FnYearID;
                    nEmpID = dLayer.SaveData("pay_Employee", "n_EmpID", DupCriteria, X_Crieteria, dtMasterTable, connection, transaction);
                    if (nEmpID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error("Unable to save"));
                    }
                    else
                    {
                        nSavedEmpID = nEmpID;
                        QueryParams.Add("@nSavedEmpID", nEmpID);
                        //inserting to [Log_ScreenActivity
                        string ipAddress = "";
                        if (Request.Headers.ContainsKey("X-Forwarded-For"))
                            ipAddress = Request.Headers["X-Forwarded-For"];
                        else
                            ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                        SortedList LogParams = new SortedList();
                        LogParams.Add("N_CompanyID", nCompanyID);
                        LogParams.Add("N_FnYearID", nFnYearID);
                        LogParams.Add("N_TransID", nEmpID);
                        LogParams.Add("N_FormID", this.FormID);
                        LogParams.Add("N_UserId", nUserID);
                        LogParams.Add("X_Action", X_BtnAction);
                        LogParams.Add("X_SystemName", "ERP Cloud");
                        LogParams.Add("X_IP", ipAddress);
                        LogParams.Add("X_TransCode", xEmpCode);
                        LogParams.Add("X_Remark", " ");
                        dLayer.ExecuteNonQueryPro("SP_Log_SysActivity", LogParams, connection, transaction);

                        int pay_EmpAddlInfoRes = 0;
                        if (dtPay_EmpAddlInfo.Rows.Count > 0)
                            pay_EmpAddlInfoRes = dLayer.SaveData("Pay_EmpAddlInfo", "N_InfoID", dtPay_EmpAddlInfo, connection, transaction);

                        int Pay_EmployeeSubRes = 0;
                        if (dtpay_EmployeeSub.Rows.Count > 0)
                            Pay_EmployeeSubRes = dLayer.SaveData("Pay_EmployeeSub", "N_ContactDetailsID", dtpay_EmployeeSub, connection, transaction);

                        int Pay_Employee_LogRes = 0;
                        if (dtPay_Employee_Log.Rows.Count > 0)
                            Pay_Employee_LogRes = dLayer.SaveData("Pay_Employee_Log", "N_EmployeeLogID", dtPay_Employee_Log, connection, transaction);

                        dLayer.ExecuteNonQuery("Update Pay_SuperVisor Set N_EmpID = 0 Where N_CompanyID =@nCompanyID And N_EmpID =@nSavedEmpID", QueryParams, connection);

                        bool B_Inactive = false;
                        B_Inactive = myFunctions.getBoolVAL(dtMasterTable.Rows[0]["b_Inactive"].ToString());
                        if (B_Inactive)
                            dLayer.ExecuteNonQuery("Update Pay_SuperVisor Set N_EmpID = 0 Where N_CompanyID =@nCompanyID And N_EmpID =@nPositionID", QueryParams, connection);
                        else
                            dLayer.ExecuteNonQuery("Update Pay_SuperVisor Set N_EmpID = @nSavedEmpID Where N_CompanyID =@nCompanyID And N_PositionID =@nPositionID", QueryParams, connection);

                        //SAving EMPLOYEE SALARY/BENEFITS
                        int pay_PaySetupRes = 0;
                        if (dtpay_PaySetup.Rows.Count > 0)
                            pay_PaySetupRes = dLayer.SaveData("Pay_PaySetup", "Pay_PaySetupID", dtpay_PaySetup, connection, transaction);
                        if (pay_PaySetupRes > 0)
                        {
                            int Pay_EmployeePayHistoryRes = 0;
                            if (dtpay_EmployeePayHistory.Rows.Count > 0)
                                Pay_EmployeePayHistoryRes = dLayer.SaveData("Pay_EmployeePayHistory", "N_PayHistoryID", dtpay_EmployeePayHistory, connection, transaction);
                        }
                        int pay_EmpAccrulsRes = 0;
                        if (dtpay_EmpAccruls.Rows.Count > 0)
                            pay_EmpAccrulsRes = dLayer.SaveData("Pay_EmpAccruls", "N_EmpAccID", dtpay_EmpAccruls, connection, transaction);

                        int Acc_OtherInformationRes = 0;
                        if (dtacc_OtherInformation.Rows.Count > 0)
                            Acc_OtherInformationRes = dLayer.SaveData("Acc_OtherInformation", "N_OtherDtlsID", dtacc_OtherInformation, connection, transaction);

                        //ATTACHMENT SAVING
                        //REMINDER SAVING

                        int Pay_EmployeeAlertsRes = 0;
                        if (dtpay_EmployeeAlerts.Rows.Count > 0)
                            Pay_EmployeeAlertsRes = dLayer.SaveData("Pay_EmployeeAlerts", "N_AlertID", dtpay_EmployeeAlerts, connection, transaction);

                        int Pay_EmployeeDependenceRes = 0;
                        if (dtpay_EmployeeDependence.Rows.Count > 0)
                            Pay_EmployeeDependenceRes = dLayer.SaveData("Pay_EmployeeDependence", "N_DependenceID", dtpay_EmployeeDependence, connection, transaction);
                        if (Pay_EmployeeDependenceRes > 0)
                        {
                            //SaveFamilyAttachements
                            //DependenceReminderSave
                        }

                        string xDepartment = "";
                        object objDept = dLayer.ExecuteScalar("Select X_Department from Pay_Department Where N_DepartmentID =" + nDepartmentID + " and N_CompanyID= " + nCompanyID + "and N_FnYearID =" + nFnYearID, connection, transaction);
                        if (objDept != null)
                            xDepartment = objDept.ToString();

                        SortedList ParamsAccount = new SortedList();
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_EmpID", nSavedEmpID);
                        Params.Add("X_EmpCode", xEmpCode);
                        Params.Add("X_Department", xDepartment);
                        Params.Add("X_EmpName", xEmpName);
                        Params.Add("N_UserID", nUserID);
                        Params.Add("X_Form", "Pay_EmployeeMaster");

                        if (myFunctions.getIntVAL(dtMasterTable.Rows[0]["N_LedgerID"].ToString()) == 0)
                            dLayer.ExecuteScalarPro("SP_Pay_CreateEmployeeAccount", ParamsAccount, connection, transaction).ToString();
                        if (myFunctions.getIntVAL(dtMasterTable.Rows[0]["N_LoanLedgerID"].ToString()) == 0)
                            dLayer.ExecuteScalarPro("SP_Pay_CreateEmployeeLoanAccount", ParamsAccount, connection, transaction).ToString();

                        bool B_EnableSalesExec = myFunctions.CheckPermission(nCompanyID, 290, myFunctions.GetUserCategory(User).ToString(), "N_UserCategoryID", dLayer, connection);
                        if (B_EnableSalesExec)
                        {
                            int Inv_SalesmanRes = 0;
                            if (dtInv_Salesman.Rows.Count > 0)
                                Inv_SalesmanRes = dLayer.SaveData("Inv_Salesman", "N_SalesmanID", dtInv_Salesman, connection, transaction);
                        }

                        bool B_CheckDriver = false;
                        if (B_CheckDriver)
                        {
                            int Veh_DriversRes = 0;
                            if (dtVeh_Drivers.Rows.Count > 0)
                                Veh_DriversRes = dLayer.SaveData("Inv_Salesman", "N_SalesmanID", dtVeh_Drivers, connection, transaction);
                        }
                        bool B_Teacher = myFunctions.CheckPermission(nCompanyID, 155, myFunctions.GetUserCategory(User).ToString(), "N_UserCategoryID", dLayer, connection);
                        if (B_Teacher)
                        {
                            int Sch_TeacherRes = 0;
                            if (dtSch_Teacher.Rows.Count > 0)
                                Sch_TeacherRes = dLayer.SaveData("Sch_Teacher", "N_TeacherID", dtSch_Teacher, connection, transaction);
                        }
                        int Pay_EmployeeEducationRes = 0;
                        if (dtPay_EmployeeEducation.Rows.Count > 0)
                            Pay_EmployeeEducationRes = dLayer.SaveData("Pay_EmployeeEducation", "N_EduID", dtPay_EmployeeEducation, connection, transaction);

                        int Pay_EmploymentHistoryRes = 0;
                        if (dtPay_EmploymentHistory.Rows.Count > 0)
                            Pay_EmploymentHistoryRes = dLayer.SaveData("Pay_EmploymentHistory", "N_JobID", dtPay_EmploymentHistory, connection, transaction);


                        transaction.Commit();
                        return Ok(_api.Success("Employee Information Saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(ex));
            }
        }


        [HttpGet("managerList")]
        public ActionResult GetManagerList(int nFnYearID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnYearID);
            string sqlCommandText = "Select N_CompanyID,N_SupervisorID,N_EmpID,Code,N_BranchID,N_FnYearID,[Employee Code],[Employee Name],Description from vw_Supervisor_ReportTo Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID order by [Employee Code]";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }


        [HttpGet("employeeType")]
        public ActionResult GetEmployeeType()
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nCompanyID", nCompanyID);
            string sqlCommandText = "Select N_EmploymentID,N_TypeId,B_EnableGosi,N_CompanyID,N_Months,X_Description from Pay_EmploymentType Where N_CompanyID=@nCompanyID  order by X_Description";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }

        [HttpGet("salaryGrade")]
        public ActionResult GetSalaryGrade()
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nCompanyID", nCompanyID);
            string sqlCommandText = "Select X_GradeCode,X_Gradename,N_CompanyID,N_GradeID,B_Active,B_Edit from Pay_SalaryGrade Where N_CompanyID=@nCompanyID   and B_Active=1 order by X_Gradename";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }
        [AllowAnonymous]
        [HttpGet("dummy")]
        public ActionResult GetVoucherDummy(string id)
        {
            try
            {
                return Ok(myFunctions.DecryptString("wbQgkm+DI/k="));
                using (SqlConnection Con = new SqlConnection(connectionString))
                {
                    Con.Open();
                    string sqlCommandText = "select * from Pay_EmploymentHistory";
                    SortedList mParamList = new SortedList() { { "@p1", id } };
                    DataTable masterTable = dLayer.ExecuteDataTable(sqlCommandText, mParamList, Con);
                    masterTable = _api.Format(masterTable, "Pay_EmployeeEducation");

                    // string sqlCommandText2 = "select * from Pay_EmpAddlInfo where N_EmpID=@p1";
                    // SortedList dParamList = new SortedList() { { "@p1", id } };
                    // DataTable detailTable = dLayer.ExecuteDataTable(sqlCommandText2, dParamList, Con);
                    // detailTable = _api.Format(detailTable, "Pay_EmpAddlInfo");

                    // string sqlCommandText3 = "select * from Inv_SaleAmountDetails where N_SalesId=@p1";
                    // DataTable dtAmountDetails = dLayer.ExecuteDataTable(sqlCommandText3, dParamList, Con);
                    // dtAmountDetails = _api.Format(dtAmountDetails, "saleamountdetails");

                    //if (detailTable.Rows.Count == 0) { return Ok(new { }); }
                    DataSet dataSet = new DataSet();
                    dataSet.Tables.Add(masterTable);
                    // dataSet.Tables.Add(detailTable);
                    //dataSet.Tables.Add(dtAmountDetails);

                    return Ok(dataSet);

                }
            }
            catch (Exception e)
            {
                return StatusCode(403, _api.Error(e));
            }
        }


    }
}