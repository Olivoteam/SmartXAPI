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
using System.Linq;
using System.Security.Claims;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("waiverequest")]
    [ApiController]



    public class Ess_WaiveRequest : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int FormID;


        public Ess_WaiveRequest(IDataAccessLayer dl, IApiFunctions apiFun, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            api = apiFun;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 1232;
        }
         [HttpGet("list")]
        public ActionResult GetWaiveRequestList(string xReqType)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();

            int nUserID = myFunctions.GetUserID(User);
            int nCompanyID = myFunctions.GetCompanyID(User);
            QueryParams.Add("@nCompanyID", nCompanyID);
            QueryParams.Add("@nUserID", nUserID);
            string sqlCommandText = "";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    object nEmpID = dLayer.ExecuteScalar("Select N_EmpID From Sec_User where N_UserID=@nUserID and N_CompanyID=@nCompanyID", QueryParams, connection);
                    if (nEmpID != null)
                    {
                        QueryParams.Add("@nEmpID", myFunctions.getIntVAL(nEmpID.ToString()));
                        QueryParams.Add("@xStatus", xReqType);
                        if (xReqType.ToLower() == "all")
                            sqlCommandText = "Select * From vw_AnytimeRequestList where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID order by D_RequestDate Desc";
                        else
                        if (xReqType.ToLower() == "pending")
                            sqlCommandText = "select * from vw_AnytimeRequestList where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID and X_Status not in ('Reject','Approved')  order by D_RequestDate Desc ";
                        else
                            sqlCommandText = "Select * From vw_AnytimeRequestList where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID and X_Status=@xStatus order by D_RequestDate Desc";

                        dt = dLayer.ExecuteDataTable(sqlCommandText, QueryParams, connection);
                    }


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
                return BadRequest(api.Error(e));
            }
        }

         [HttpGet("TimesheetList")]
        public ActionResult GetTimesheetList(DateTime date,int nFnYearID,int nEmpID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();

            int nUserID = myFunctions.GetUserID(User);
            int nCompanyID = myFunctions.GetCompanyID(User);
            QueryParams.Add("@nCompanyID", nCompanyID);
            QueryParams.Add("@nUserID", nUserID);
            QueryParams.Add("@nFnYearID", nFnYearID);
            QueryParams.Add("@nEmpID", nEmpID);
            QueryParams.Add("@dDate", date);
            string sqlCommandText = "";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    //object nEmpID = dLayer.ExecuteScalar("Select N_EmpID From Sec_User where N_UserID=@nUserID and N_CompanyID=@nCompanyID", QueryParams, connection);
                    
                        //QueryParams.Add("@nEmpID", myFunctions.getIntVAL(nEmpID.ToString()));
                        //QueryParams.Add("@xStatus", xReqType);
                        sqlCommandText = "select * from Pay_TimeSheetImport where D_Date=@dDate and N_EmpID=@nEmpID and N_FnYearID=@nFnYearID";

                        dt = dLayer.ExecuteDataTable(sqlCommandText, QueryParams, connection);
                    


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
                return BadRequest(api.Error(e));
            }
        }

       
       

 [HttpGet("details")]
        public ActionResult GetRequestDetails(string xRequestCode)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();

           int companyid = myFunctions.GetCompanyID(User);

            QueryParams.Add("@nCompanyID", companyid);
            QueryParams.Add("@xRequestCode", xRequestCode);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string _sqlQuery = "SELECT Pay_AnytimeRequest.*, Pay_Employee.X_EmpCode, Pay_Employee.X_EmpName, Pay_Position.X_Position FROM Pay_Position RIGHT OUTER JOIN Pay_Employee ON Pay_Position.N_PositionID = Pay_Employee.N_PositionID AND Pay_Position.N_CompanyID = Pay_Employee.N_CompanyID RIGHT OUTER JOIN Pay_AnytimeRequest ON Pay_Employee.N_EmpID = Pay_AnytimeRequest.N_EmpID AND Pay_Employee.N_CompanyID = Pay_AnytimeRequest.N_CompanyID  where Pay_AnytimeRequest.X_RequestCode=@xRequestCode and  Pay_AnytimeRequest.N_CompanyID=@nCompanyID";
                
                        dt = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);


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
                return BadRequest(api.Error(e));
            }
        }

        //Save....
        [HttpPost("save")]
        public ActionResult SaveTORequest([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                MasterTable = ds.Tables["master"];
                SortedList Params = new SortedList();
                DataRow MasterRow = MasterTable.Rows[0];

                DataTable Approvals;
                Approvals = ds.Tables["approval"];
                DataRow ApprovalRow = Approvals.Rows[0];

                var x_RequestCode = MasterRow["x_RequestCode"].ToString();
                int nRequestID = myFunctions.getIntVAL(MasterRow["n_RequestID"].ToString());
                int nCompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyId"].ToString());
                int nFnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearId"].ToString());
                int nEmpID = myFunctions.getIntVAL(MasterRow["n_EmpID"].ToString());
                int nBranchID = myFunctions.getIntVAL(MasterRow["n_EmpID"].ToString());

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    SortedList EmpParams = new SortedList();
                    EmpParams.Add("@nCompanyID", nCompanyID);
                    EmpParams.Add("@nEmpID", nEmpID);
                    object objEmpName = dLayer.ExecuteScalar("Select X_EmpName From Pay_Employee where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID", EmpParams, connection, transaction);

                    if (!myFunctions.getBoolVAL(ApprovalRow["isEditable"].ToString()))
                    {
                        int N_PkeyID = nRequestID;
                        string X_Criteria = "N_RequestID=" + N_PkeyID + " and N_CompanyID=" + nCompanyID + " and N_FnYearID=" + nFnYearID;
                        myFunctions.UpdateApproverEntry(Approvals, "Pay_AnytimeRequest", X_Criteria, N_PkeyID, User, dLayer, connection, transaction);
                        myFunctions.LogApprovals(Approvals, nFnYearID, "Waive Request", N_PkeyID, x_RequestCode, 1, objEmpName.ToString(), 0, "", User, dLayer, connection, transaction);
                        transaction.Commit();
                        return Ok(api.Success("Waive Request Approval updated" + "-" + x_RequestCode));
                    }

                    if (x_RequestCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", this.FormID);
                        Params.Add("N_BranchID", nBranchID);

                        x_RequestCode = dLayer.GetAutoNumber("Pay_AnytimeRequest", "x_RequestCode", Params, connection, transaction);
                        if (x_RequestCode == "") { return Ok(api.Error("Unable to generate Waive Request Number")); }
                            MasterTable.Rows[0]["x_RequestCode"] = x_RequestCode;
                    }
                    else
                    {
                        dLayer.DeleteData("Pay_AnytimeRequest", "n_RequestID", nRequestID, "", connection, transaction);
                    }
                    MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable,"N_RequestType",typeof(int),this.FormID);
                    MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable,"N_UserID",typeof(int),myFunctions.GetUserID(User));
                    MasterTable.AcceptChanges();

                    MasterTable = myFunctions.SaveApprovals(MasterTable, Approvals, dLayer, connection, transaction);
                    nRequestID = dLayer.SaveData("Pay_AnytimeRequest", "n_RequestID", MasterTable, connection, transaction);
                    if (nRequestID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error("Unable to save"));
                    }
                    else
                    {
                         myFunctions.LogApprovals(Approvals, nFnYearID, "Waive Request", nRequestID, x_RequestCode, 1, objEmpName.ToString(), 0, "", User, dLayer, connection, transaction);
                         transaction.Commit();
                    }
                    Dictionary<string,string> res=new Dictionary<string, string>();
                    res.Add("x_RequestCode",x_RequestCode.ToString());
                    return Ok(api.Success(res,"Waive Request saved"));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(api.Error(ex));
            }
        }


          [HttpDelete()]
        public ActionResult DeleteData(int nRequestID,int nFnYearID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable TransData = new DataTable();
                    SortedList ParamList = new SortedList();
                    ParamList.Add("@nTransID", nRequestID);
                    ParamList.Add("@nFnYearID", nFnYearID);
                    ParamList.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    string Sql = "select isNull(N_UserID,0) as N_UserID,isNull(N_ProcStatus,0) as N_ProcStatus,isNull(N_ApprovalLevelId,0) as N_ApprovalLevelId,isNull(N_EmpID,0) as N_EmpID,X_RequestCode from Pay_AnytimeRequest where N_CompanyId=@nCompanyID and N_FnYearID=@nFnYearID and N_RequestID=@nTransID";
                    TransData = dLayer.ExecuteDataTable(Sql, ParamList, connection);
                    if (TransData.Rows.Count == 0)
                    {
                        return Ok(api.Error("Transaction not Found"));
                    }
                    DataRow TransRow = TransData.Rows[0];

                    DataTable Approvals = myFunctions.ListToTable(myFunctions.GetApprovals(-1, this.FormID, nRequestID, myFunctions.getIntVAL(TransRow["N_UserID"].ToString()), myFunctions.getIntVAL(TransRow["N_ProcStatus"].ToString()), myFunctions.getIntVAL(TransRow["N_ApprovalLevelId"].ToString()), 0, 0, 1, nFnYearID, myFunctions.getIntVAL(TransRow["N_EmpID"].ToString()), 2001, User, dLayer, connection));
                    Approvals = myFunctions.AddNewColumnToDataTable(Approvals, "comments", typeof(string), "Auto Generated Comment");
                    SqlTransaction transaction = connection.BeginTransaction(); ;

                    string X_Criteria = "N_RequestID=" + nRequestID + " and N_CompanyID=" + myFunctions.GetCompanyID(User) + " and N_FnYearID=" + nFnYearID;
                    if (myFunctions.UpdateApprovals(Approvals, nFnYearID, "Waive Request", nRequestID, TransRow["X_RequestCode"].ToString(), myFunctions.getIntVAL(TransRow["N_ProcStatus"].ToString()), "Pay_AnytimeRequest", X_Criteria, "", User, dLayer, connection, transaction))
                    {
                        //Delete Attachement
                        transaction.Commit();
                        return Ok(api.Success("Waive Request Deleted Successfully"));
                    }
                    else
                    {
                        transaction.Rollback();
                        return Ok(api.Error("Unable to delete Waive Request"));
                    }


                }
            }
            catch (Exception ex)
            {
                return BadRequest(api.Error(ex));
            }
        }



    }
}