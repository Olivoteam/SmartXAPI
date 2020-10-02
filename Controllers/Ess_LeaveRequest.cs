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

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("leaverequest")]
    [ApiController]



    public class Ess_LeaveRequest : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int FormID;


        public Ess_LeaveRequest(IDataAccessLayer dl, IApiFunctions apiFun, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            api = apiFun;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 210;
        }



        [HttpGet("leaveList")]
        public ActionResult GetEmployeeLeaveRequest(string xReqType)
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
                            sqlCommandText = "Select * From vw_PayVacationList where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID order by VacationRequestDate Desc";
                        else
                        if (xReqType.ToLower() == "pending")
                            sqlCommandText = "select * from vw_PayVacationList where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID and X_Status not in ('Reject','Approved')  order by VacationRequestDate Desc ";
                        else
                            sqlCommandText = "Select * From vw_PayVacationList where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID and X_Status=@xStatus order by VacationRequestDate Desc";

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


        [HttpGet("details")]
        public ActionResult GetEmployeeVacationDetails(string xVacationGroupCode, int nBranchID, bool bShowAllBranchData)
        {
            DataTable Master = new DataTable();
            DataTable Detail = new DataTable();
            DataSet ds = new DataSet();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();

            int companyid = myFunctions.GetCompanyID(User);

            QueryParams.Add("@nCompanyID", companyid);
            QueryParams.Add("@xVacationGroupCode", xVacationGroupCode);
            QueryParams.Add("@nBranchID", nBranchID);
            string Condition = "";
            string _sqlQuery = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    if (bShowAllBranchData == true)
                        Condition = "n_Companyid=@nCompanyID and X_VacationGroupCode =@xVacationGroupCode and N_TransType=1";
                    else
                        Condition = "n_Companyid=@nCompanyID and X_VacationGroupCode =@xVacationGroupCode and N_BranchID=@nBranchID and N_TransType=1";


                    _sqlQuery = "Select * from vw_PayVacationMaster Where " + Condition + "";

                    Master = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);

                    Master = api.Format(Master, "master");
                    if (Master.Rows.Count == 0)
                    {
                        return Ok(api.Notice("No Results Found"));
                    }
                    else
                    {
                        QueryParams.Add("@nVacationGroupID", Master.Rows[0]["N_VacationGroupID"].ToString());

                        ds.Tables.Add(Master);
                        Condition = "";
                        if (bShowAllBranchData == true)
                            Condition = "n_Companyid=@nCompanyID and N_VacationGroupID =@nVacationGroupID and N_TransType=1 and X_Type='B'";
                        else
                            Condition = "n_Companyid=@nCompanyID and N_VacationGroupID =@nVacationGroupID and N_BranchID=@nBranchID  and N_TransType=1 and X_Type='B'";

                        _sqlQuery = "Select * from vw_PayVacationDetails_Disp Where " + Condition + "";
                        Detail = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);

                        Detail = api.Format(Detail, "details");
                        if (Detail.Rows.Count == 0)
                        {
                            return Ok(api.Notice("No Results Found"));
                        }

                        DataTable benifits = FillCodeList(companyid,myFunctions.getIntVAL(Master.Rows[0]["N_EmpID"].ToString()), myFunctions.getIntVAL(Master.Rows[0]["N_VacationGroupID"].ToString()), connection);

                        ds.Tables.Add(api.Format(benifits,"benifits"));
                        ds.Tables.Add(Detail);

                        return Ok(api.Success(ds));
                    }


                }


            }
            catch (Exception e)
            {
                return BadRequest(api.Error(e));
            }
        }


        //List
        [HttpGet("vacationList")]
        public ActionResult GetVacationList(string nEmpID)
        {
            DataTable dt = new DataTable();
            SortedList QueryParams = new SortedList();
            int companyid = myFunctions.GetCompanyID(User);

            QueryParams.Add("@nCompanyID", companyid);
            QueryParams.Add("@nEmpID", nEmpID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable("Select * from vw_pay_Vacation_List where X_Type='B' and N_EmpId=@nEmpID and N_CompanyID=@nCompanyID", QueryParams, connection);

                }
                dt = api.Format(dt);
                if (dt.Rows.Count == 0)
                    return Ok(api.Notice("No Results Found"));
                else
                    return Ok(api.Success(dt));

            }
            catch (Exception e)
            {
                return BadRequest(api.Error(e));
            }
        }

        [HttpGet("getAvailable")]
        public ActionResult GetAvailableDays(int nVacTypeID, DateTime dDateFrom, double nAccrued, int nEmpID, int nVacationGroupID)
        {
            DateTime toDate;
            int days = 0;
            double totalDays = 0;
            int nCompanyID = myFunctions.GetCompanyID(User);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    double AvlDays = Convert.ToDouble(CalculateGridAnnualDays(nVacTypeID, nEmpID, nCompanyID, nVacationGroupID, connection));

                    SortedList paramList = new SortedList();
                    paramList.Add("@nCompanyID", nCompanyID);
                    paramList.Add("@nEmpID", nEmpID);
                    paramList.Add("@nVacTypeID", nVacTypeID);
                    paramList.Add("@nVacationGroupID", nVacationGroupID);

                    toDate = Convert.ToDateTime(dLayer.ExecuteScalar("Select isnull(Max(D_VacDateTo),getdate()) from Pay_VacationDetails Where N_CompanyID =@nCompanyID and  N_EmpID  =@nEmpID and N_VacTypeID =@nVacTypeID and N_VacationStatus = 0 and N_VacDays>0 ", paramList, connection).ToString());
                    if (toDate < dDateFrom)
                        days = Convert.ToInt32(dLayer.ExecuteScalar("select  DATEDIFF(day,'" + Convert.ToDateTime(toDate) + "','" + dDateFrom + "')", connection).ToString());
                    else
                        days = 0;
                    if (nVacTypeID == 6)
                    {
                        totalDays = Math.Round(AvlDays + ((days / 30.458) * nAccrued), 0);
                    }
                    else
                        totalDays = Math.Round(AvlDays + ((days / 30.458)), 0);
                }
                Dictionary<string, string> res = new Dictionary<string, string>();
                res.Add("availableDays", totalDays.ToString());

                return Ok(api.Success(res));
            }
            catch (Exception e)
            {
                return BadRequest(api.Error(e));
            }
        }

        private String CalculateGridAnnualDays(int VacTypeID, int empID, int compID, int nVacationGroupID, SqlConnection connection)
        {
            double vacation;
            const double tolerance = 8e-14;
            SortedList paramList = new SortedList();
            paramList.Add("@nCompanyID", compID);
            paramList.Add("@nEmpID", empID);
            paramList.Add("@nVacTypeID", VacTypeID);
            paramList.Add("@nVacationGroupID", nVacationGroupID);


            DateTime toDate = Convert.ToDateTime(dLayer.ExecuteScalar("Select isnull(Max(D_VacDateTo),getdate()) from Pay_VacationDetails Where N_CompanyID =@nCompanyID and  N_EmpID  =@nEmpID and N_VacTypeID =@nVacTypeID and N_VacDays>0 ", paramList, connection).ToString());

            if (nVacationGroupID == 0)
                vacation = Convert.ToDouble(dLayer.ExecuteScalar("Select isnull(SUM(N_VacDays),0) as N_VacDays from Pay_VacationDetails Where N_CompanyID =@nCompanyID and  N_EmpID  =@nEmpID and N_VacTypeID =@nVacTypeID", paramList, connection).ToString());
            else
                vacation = Convert.ToDouble(dLayer.ExecuteScalar("Select isnull(SUM(N_VacDays),0) as N_VacDays from Pay_VacationDetails Where N_CompanyID =@nCompanyID and  N_EmpID  =@nEmpID and N_VacTypeID =@nVacTypeID and isnull(N_VacationGroupID,0) < @nVacationGroupID", paramList, connection).ToString());

            String AvlDays = RoundApproximate(vacation, 0, tolerance, MidpointRounding.AwayFromZero).ToString();


            return AvlDays;
        }

        private static double RoundApproximate(double dbl, int digits, double margin, MidpointRounding mode)
        {
            double fraction = dbl * Math.Pow(10, digits);
            double value = Math.Truncate(fraction);
            fraction = fraction - value;
            if (fraction == 0)
                return dbl;

            double tolerance = margin * dbl;
            // Any remaining fractional value greater than .5 is not a midpoint value. 
            if (fraction > .5)
                return (value + 0.5) / Math.Pow(10, digits);
            else if (fraction < -(0.5))
                return (value + 0.5) / Math.Pow(10, digits);
            else if (fraction == .5)
                return Math.Round(dbl, 1);
            else
                return value / Math.Pow(10, digits);
        }

        //List
        [HttpGet("vacationBenefits")]
        public ActionResult GetVacationBenefits(string nEmpID, int nVacationGroupID)
        {
            DataTable dt = new DataTable();
            SortedList QueryParams = new SortedList();
            int companyid = myFunctions.GetCompanyID(User);

            QueryParams.Add("@nCompanyID", companyid);
            QueryParams.Add("@nEmpID", nEmpID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();


                    object res = dLayer.ExecuteScalar("Select ISNULL(sum(N_VacDays),0) as N_VacDays from Pay_VacationDetails where (B_Allowance=1) and  N_CompanyID=@nCompanyID and N_EmpId=@nEmpID", QueryParams, connection);

                    if (res == null)
                        res = 0;
                    string sql = "";
                    if (nVacationGroupID > 0)
                        sql = "SELECT    N_VacTypeID,X_VacCode,X_VacType as Code,N_Accrued as Value,N_Accrued+isnull(" + res.ToString() + ",0) as Avlbl,CONVERT(bit,0) As Mark,B_IsReturn,X_Type,0 as DetailsID  from vw_pay_EmpVacation_Alowance  where (X_Type='A' or X_Type='T') AND  N_CompanyID=@nCompanyID and N_EmpId=@nEmpID and N_VacDays<=0";
                    else
                        sql = "SELECT    N_VacTypeID,X_VacCode,X_VacType as Code,N_Accrued as Value,N_Accrued+isnull(" + res.ToString() + ",0) as Avlbl,CONVERT(bit,0) As Mark,B_IsReturn,X_Type,0 as DetailsID  from vw_pay_EmpVacation_Alowance  where (X_Type='A' or X_Type='T') AND  N_CompanyID=@nCompanyID and N_EmpId=@nEmpID and N_VacDays>0";

                    dt = dLayer.ExecuteDataTable(sql, QueryParams, connection);

                }
                dt = api.Format(dt);
                if (dt.Rows.Count == 0)
                    return Ok(api.Notice("No Results Found"));
                else
                    return Ok(api.Success(dt));

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
                DataTable MasterTable, DetailTable;
                MasterTable = ds.Tables["master"];
                DetailTable = ds.Tables["details"];
                DataTable Approvals;
                Approvals = ds.Tables["approval"];
                DataRow ApprovalRow = Approvals.Rows[0];

                DataTable Benifits;
                Benifits = ds.Tables["benifits"];

                SortedList Params = new SortedList();
                DataRow MasterRow = MasterTable.Rows[0];
                var x_VacationGroupCode = MasterRow["x_VacationGroupCode"].ToString();
                int n_VacationGroupID = myFunctions.getIntVAL(MasterRow["n_VacationGroupID"].ToString());
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
                        int N_PkeyID = n_VacationGroupID;
                        string X_Criteria = "N_VacationGroupID=" + N_PkeyID + " and N_CompanyID=" + nCompanyID + " and N_FnYearID=" + nFnYearID;
                        myFunctions.UpdateApproverEntry(Approvals, "Pay_VacationMaster", X_Criteria, N_PkeyID, User, dLayer, connection, transaction);
                        myFunctions.LogApprovals(Approvals, nFnYearID, "LEAVE REQUEST", N_PkeyID, x_VacationGroupCode, 1, objEmpName.ToString(), 0, "", User, dLayer, connection, transaction);
                        transaction.Commit();
                        return Ok(api.Success("Leave Request Approved " + "-" + x_VacationGroupCode));
                    }

                    if (x_VacationGroupCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", "210");
                        Params.Add("N_BranchID", nBranchID);
                        x_VacationGroupCode = dLayer.GetAutoNumber("Pay_VacationMaster", "x_VacationGroupCode", Params, connection, transaction);
                        if (x_VacationGroupCode == "") { return Ok(api.Error("Unable to generate leave Request Code")); }
                        MasterTable.Rows[0]["x_VacationGroupCode"] = x_VacationGroupCode;
                    }
                    else
                    {
                        dLayer.DeleteData("Pay_VacationMaster", "n_VacationGroupID", n_VacationGroupID, "", connection, transaction);
                    }
                    MasterTable.AcceptChanges();

                    MasterTable = myFunctions.SaveApprovals(MasterTable, Approvals, dLayer, connection, transaction);
                    n_VacationGroupID = dLayer.SaveData("Pay_VacationMaster", "n_VacationGroupID", MasterTable, connection, transaction);
                    if (n_VacationGroupID > 0)
                    {
                        myFunctions.LogApprovals(Approvals, nFnYearID, "LEAVE REQUEST", n_VacationGroupID, x_VacationGroupCode, 1, objEmpName.ToString(), 0, "", User, dLayer, connection, transaction);

                        for (int j = 0; j < DetailTable.Rows.Count; j++)
                        {
                            DetailTable.Rows[j]["n_VacationGroupID"] = n_VacationGroupID;
                        }
                        int DetailID = dLayer.SaveData("Pay_VacationDetails", "n_VacationID", DetailTable, connection, transaction);
                        if (DetailID > 0)
                        {



                            foreach (DataRow var in Benifits.Rows)
                            {
                                bool ticketSelected = false;
                                if (!myFunctions.getBoolVAL(var["Mark"].ToString())) continue;
                                ticketSelected = true;



                                SortedList benifitParam = new SortedList();
                                benifitParam.Add("@nVacTypeID", myFunctions.getIntVAL(var["N_VacTypeID"].ToString()));
                                benifitParam.Add("@nCompanyID", nCompanyID);
                                benifitParam.Add("@nFnYearID", nFnYearID);
                                benifitParam.Add("@nEmpID", nEmpID);

                                object result = dLayer.ExecuteScalar("select ISNULL(Sum(N_VacDays),0) from Pay_VacationDetails where  N_VacTypeID=@nVacTypeID and  N_CompanyID =@nCompanyID and N_EmpID=@nEmpID and N_FnYearID=@nFnYearID", benifitParam, connection, transaction);
                                if (myFunctions.getIntVAL(result.ToString()) <= 0)
                                {
                                    if (myFunctions.getIntVAL(var["Value"].ToString()) < 0) continue;

                                }
                                
                                DataTable BenifitsTable = new DataTable();
                                BenifitsTable=DetailTable.Clone();
                                BenifitsTable.Rows.Add(DetailTable.Rows[0].ItemArray);
                                DataRow BenifitsRow = BenifitsTable.Rows[0];

                                BenifitsRow["N_VacDays"] = -1;
                                BenifitsRow["N_TransType"] = 1;
                                BenifitsRow["N_VacationStatus"] = 1;
                                BenifitsRow["B_IsAdjustEntry"] = 0;
                                BenifitsRow["B_Ticket"] = ticketSelected;
                                BenifitsRow["X_VacationCode"] = x_VacationGroupCode;
                                BenifitsRow["B_Allowance"] = 1;
                                BenifitsRow["N_FormID"] = 210;
                                BenifitsRow["N_VacTypeID"] = var["n_VacTypeID"].ToString();

                                
                                BenifitsTable.AcceptChanges();
                                int BeniftID = dLayer.SaveData("Pay_VacationDetails", "n_VacationID", BenifitsTable, connection, transaction);
                                if (BeniftID <= 0)
                                {
                                    transaction.Rollback();
                                    return Ok(api.Error("Unable to save"));
                                }

                            }
                        }
                        else
                        {
                            transaction.Rollback();
                            return Ok(api.Error("Unable to save"));
                        }
                    }
                    else
                    {
                        transaction.Rollback();
                        return Ok(api.Error("Unable to save"));
                    }

                    transaction.Commit();
                    Dictionary<string, string> res = new Dictionary<string, string>();
                    res.Add("x_RequestCode", x_VacationGroupCode.ToString());
                    return Ok(api.Success(res, "Leave Request saved"));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(api.Error(ex));
            }
        }


        private DataTable FillCodeList(int nCompanyID,  int nEmpID, int nVacGroupID, SqlConnection connection)
        {
            DataTable Benifits = new DataTable();

            SortedList benifitParam = new SortedList();
            benifitParam.Add("@nCompanyID", nCompanyID);
            benifitParam.Add("@nEmpID", nEmpID);
            benifitParam.Add("@nVacGroupID", nVacGroupID);

            object res = dLayer.ExecuteScalar("Select ISNULL(sum(N_VacDays),0) as N_VacDays from Pay_VacationDetails where (B_Allowance=1) and  N_CompanyID=@nCompanyID and N_EmpId=@nEmpID", benifitParam, connection);

            if (res == null)
                res = 0;
            string sql = "";
            if (nVacGroupID > 0)
                sql = "SELECT    N_VacTypeID,X_VacCode,X_VacType as Code,N_Accrued as Value,N_Accrued+isnull(" + res.ToString() + ",0) as Avlbl,CONVERT(bit,0) As Mark,B_IsReturn,X_Type,0 as DetailsID  from vw_pay_EmpVacation_Alowance  where (X_Type='A' or X_Type='T') AND  N_CompanyID=@nCompanyID and N_EmpId=@nEmpID and N_VacDays<=0";
            else
                sql = "SELECT    N_VacTypeID,X_VacCode,X_VacType as Code,N_Accrued as Value,N_Accrued+isnull(" + res.ToString() + ",0) as Avlbl,CONVERT(bit,0) As Mark,B_IsReturn,X_Type,0 as DetailsID  from vw_pay_EmpVacation_Alowance  where (X_Type='A' or X_Type='T') AND  N_CompanyID=@nCompanyID and N_EmpId=@nEmpID and N_VacDays>0";
            Benifits = dLayer.ExecuteDataTable(sql, benifitParam, connection);

            DataTable PayVacDetails = dLayer.ExecuteDataTable("Select * from vw_PayVacationDetails Where N_CompanyID=@nCompanyID and N_EmpID =@nEmpID and N_VacationGroupID=@nVacGroupID and (X_Type='A' or X_Type='T')",benifitParam,connection);
            foreach (DataRow var in PayVacDetails.Rows)
            {
                for (int i = 0; i < Benifits.Rows.Count; i++)
                {

                    if (Benifits.Rows[i]["N_VacTypeID"].ToString() == var["N_VacTypeID"].ToString())
                    {
                        Benifits.Rows[i]["DetailsID"] = var["N_VacationID"].ToString();
                        Benifits.Rows[i]["Mark"] = true;
                    }

                }
            }
            Benifits.AcceptChanges();

            return Benifits;
        }

        [HttpDelete()]
        public ActionResult DeleteData(int n_VacationGroupID, int nFnYearID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable TransData = new DataTable();
                    SortedList ParamList = new SortedList();
                    ParamList.Add("@nTransID", n_VacationGroupID);
                    ParamList.Add("@nFnYearID", nFnYearID);
                    ParamList.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    string Sql = "select isNull(N_UserID,0) as N_UserID,isNull(N_ProcStatus,0) as N_ProcStatus,isNull(N_ApprovalLevelId,0) as N_ApprovalLevelId,isNull(N_EmpID,0) as N_EmpID,X_VacationGroupCode,N_vacTypeID from Pay_VacationMaster where N_CompanyId=@nCompanyID and N_FnYearID=@nFnYearID and N_VacationGroupID=@nTransID";
                    TransData = dLayer.ExecuteDataTable(Sql, ParamList, connection);
                    if (TransData.Rows.Count == 0)
                    {
                        return Ok(api.Error("Transaction not Found"));
                    }
                    DataRow TransRow = TransData.Rows[0];

                    DataTable Approvals = myFunctions.ListToTable(myFunctions.GetApprovals(-1, this.FormID, n_VacationGroupID, myFunctions.getIntVAL(TransRow["N_UserID"].ToString()), myFunctions.getIntVAL(TransRow["N_ProcStatus"].ToString()), myFunctions.getIntVAL(TransRow["N_ApprovalLevelId"].ToString()), 0, 0, 1, nFnYearID, myFunctions.getIntVAL(TransRow["N_EmpID"].ToString()), myFunctions.getIntVAL(TransRow["N_vacTypeID"].ToString()), User, dLayer, connection));
                    Approvals = myFunctions.AddNewColumnToDataTable(Approvals, "comments", typeof(string), "");
                    SqlTransaction transaction = connection.BeginTransaction(); ;

                    string X_Criteria = "N_VacationGroupID=" + n_VacationGroupID + " and N_CompanyID=" + myFunctions.GetCompanyID(User) + " and N_FnYearID=" + nFnYearID;
                                        string ButtonTag = Approvals.Rows[0]["deleteTag"].ToString();
                    int ProcStatus=myFunctions.getIntVAL(ButtonTag.ToString());
                    //myFunctions.getIntVAL(TransRow["N_ProcStatus"].ToString())
                    string status = myFunctions.UpdateApprovals(Approvals, nFnYearID, "LEAVE REQUEST", n_VacationGroupID, TransRow["X_VacationGroupCode"].ToString(), ProcStatus, "Pay_VacationMaster", X_Criteria, "", User, dLayer, connection, transaction);
                    if (status != "Error")
                    {
                        transaction.Commit();
                        return Ok(api.Success("Leave Request " + status + " Successfully"));
                    }
                    else
                    {
                        transaction.Rollback();
                        return Ok(api.Error("Unable to delete Leave Request"));
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