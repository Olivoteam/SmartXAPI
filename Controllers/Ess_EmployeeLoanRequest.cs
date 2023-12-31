using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using SmartxAPI.GeneralFunctions;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("employeeloanrequest")]
    [ApiController]



    public class Ess_EmployeeLoanRequest : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly string xTransType;
        private readonly int FormID;

        public Ess_EmployeeLoanRequest(IDataAccessLayer dl, IApiFunctions _api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            api = _api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 212;
            xTransType = "LOAN ISSUE";
        }



        [HttpGet("list")]
        public ActionResult GetEmpReqList(string xReqType, int nPage, int nSizeperpage, string xSearchkey, string xSortBy, int empID)
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
            if (empID != 0 && empID != null)
            {
                sqlCommandText = "select * from vw_Pay_LoanIssueList where  N_EmpID=" + empID + " and N_CompanyID=" + nCompanyID + " ";
            }

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (N_LoanID like'%" + xSearchkey + "%'or X_Remarks like'%" + xSearchkey + "%'or D_LoanIssueDate like'%" + xSearchkey + "%'or N_LoanAmount like'%" + xSearchkey + "%'or N_Installments like'%" + xSearchkey + "%'or D_LoanPeriodFrom like'%" + xSearchkey + "%'or D_LoanPeriodTo like'%" + xSearchkey + "%'or X_CurrentStatus like'%" + xSearchkey + "%'or X_Description like'%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_LoanID desc";
            else if (xSortBy.Contains("d_LoanIssueDate"))
                xSortBy = " order by cast(D_LoanIssueDate as DateTime) " + xSortBy.Split(" ")[1];
            else
                xSortBy = " order by " + xSortBy;
            if (empID == 0 || empID == null)
            {
                if (Count == 0)
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Pay_LoanIssueList where  N_EmpID=@nEmpID and N_CompanyID=@nCompanyID " + Searchkey + " " + xSortBy;
                else
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Pay_LoanIssueList where  N_EmpID=@nEmpID and N_CompanyID=@nCompanyID " + Searchkey + " and N_LoanTransID not in (select top(" + Count + ") N_LoanTransID from vw_Pay_LoanIssueList where  N_EmpID=@nEmpID and N_CompanyID=@nCompanyID " + xSortBy + " ) " + xSortBy;
            }
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    object nEmpID;
                    if (empID == 0 || empID == null)
                    {
                        nEmpID = dLayer.ExecuteScalar("Select N_EmpID From Sec_User where N_UserID=@nUserID and N_CompanyID=@nCompanyID", QueryParams, connection);
                    }
                    else
                    {
                        nEmpID = empID;
                    }
                    if (nEmpID != null)
                    {
                        QueryParams.Add("@nEmpID", myFunctions.getIntVAL(nEmpID.ToString()));
                        dt = dLayer.ExecuteDataTable(sqlCommandText, QueryParams, connection);
                        sqlCommandCount = "select count(1) as N_Count from vw_Pay_LoanIssueList where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID  " + Searchkey + "";
                        object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, QueryParams, connection);
                        OutPut.Add("Details", api.Format(dt));
                        OutPut.Add("TotalCount", TotalCount);
                    }
                    else
                    {
                        return Ok(api.Notice("No Results Found"));
                    }


                }
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
                return Ok(api.Error(User,e));
            }
        }


        [HttpGet("typeList")]
        public ActionResult GetLoanTypeList(int? nCompanyID, int nFnYearID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnYearID);
            string sqlCommandText = "";
            sqlCommandText = "select * from Pay_PayMaster where N_PayTypeID=8 and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID";

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
                return Ok(api.Error(User,e));
            }
        }

        [HttpGet("details")]
        public ActionResult GetEmployeeLoanDetails(int nLoanID, int nFnYearID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();

            int companyid = myFunctions.GetCompanyID(User);


            QueryParams.Add("@nCompanyID", companyid);
            QueryParams.Add("@nLoanID", nLoanID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    object count = dLayer.ExecuteScalar("select count(1) From Pay_LoanIssue " +
                                 " INNER JOIN dbo.Pay_EmployeePaymentDetails ON dbo.Pay_LoanIssue.N_CompanyID = dbo.Pay_EmployeePaymentDetails.N_CompanyID AND " +
                                 " dbo.Pay_LoanIssue.N_LoanTransID = dbo.Pay_EmployeePaymentDetails.N_SalesID and dbo.Pay_EmployeePaymentDetails.N_Entryfrom=212 and Pay_LoanIssue.N_LoanAmount =dbo.Pay_EmployeePaymentDetails.N_Amount " +
                                 " where Pay_LoanIssue.N_LoanID =" + nLoanID + " and Pay_LoanIssue.N_CompanyID=" + companyid + " and Pay_LoanIssue.N_FnYearID =" + nFnYearID + "", QueryParams, connection);


                    object RefundAmount = dLayer.ExecuteScalar("select SUM(N_RefundAmount) from Pay_LoanIssueDetails inner join Pay_LoanIssue on Pay_LoanIssueDetails.N_LoanTransID=Pay_LoanIssue.N_LoanTransID and Pay_LoanIssueDetails.N_CompanyID=Pay_LoanIssue.N_CompanyID where Pay_LoanIssue.N_LoanID =" + nLoanID + " and Pay_LoanIssue.N_CompanyID=" + companyid + " and Pay_LoanIssue.N_FnYearID =" + nFnYearID + "", QueryParams, connection);

                    string _sqlQuery = " SELECT  Pay_LoanIssue.N_CompanyID, Pay_LoanIssue.N_EmpID, Pay_LoanIssue.N_LoanTransID, Pay_LoanIssue.D_LoanIssueDate, Pay_LoanIssue.D_EntryDate, Pay_LoanIssue.X_Remarks,Pay_LoanIssue.D_LoanPeriodFrom, Pay_LoanIssue.D_LoanPeriodTo, Pay_LoanIssue.N_LoanAmount, Pay_LoanIssue.N_LoanID, Pay_LoanIssue.N_PayID, Pay_LoanIssue.N_Installments,Pay_LoanIssue.N_DefLedgerID, Pay_LoanIssue.X_Paymentmethod, Pay_LoanIssue.X_ChequeNo, Pay_LoanIssue.D_ChequeDate, Pay_LoanIssue.N_UserID, Pay_LoanIssue.X_BankName, Pay_LoanIssue.N_FnYearID, Pay_LoanIssue.N_LoanStatus, Pay_LoanIssue.B_OpeningBal, Pay_LoanIssue.N_BranchID, Pay_LoanIssue.N_WebLoanId, Pay_LoanIssue.N_ApprovalLevelId,Pay_LoanIssue.N_ProcStatus, Pay_LoanIssue.N_NextApprovalID, Pay_LoanIssue.B_IsSaveDraft, Pay_LoanIssue.X_Comments, Pay_LoanIssue.X_Guarantor1, Pay_LoanIssue.X_Guarantor2,Pay_LoanIssue.X_RefFrom, Pay_LoanIssue.N_RefID, Pay_LoanIssue.N_StatusID, Pay_Employee.X_EmpCode, Pay_Employee.X_EmpName, Pay_Position.X_Position,Pay_Employee.X_EmpNameLocale, Pay_PayMaster.X_Description AS x_LoanType, Sec_User.X_UserName,CASE WHEN Pay_LoanIssue.N_StatusID = 1 THEN CONCAT('Approved by ', Sec_User.X_UserName) end as X_ActionStatus FROM  Sec_User RIGHT OUTER JOIN "+
                     "Pay_LoanIssue ON Sec_User.N_UserID = Pay_LoanIssue.N_UserID AND Sec_User.N_CompanyID = Pay_LoanIssue.N_CompanyID LEFT OUTER JOIN Pay_PayMaster ON Pay_LoanIssue.N_FnYearID = Pay_PayMaster.N_FnYearID AND Pay_LoanIssue.N_CompanyID = Pay_PayMaster.N_CompanyID AND Pay_LoanIssue.N_PayID = Pay_PayMaster.N_PayID LEFT OUTER JOIN Pay_Position RIGHT OUTER JOIN "+
                      "Pay_Employee ON Pay_Position.N_PositionID = Pay_Employee.N_PositionID AND Pay_Position.N_CompanyID = Pay_Employee.N_CompanyID ON Pay_LoanIssue.N_EmpID = Pay_Employee.N_EmpID AND Pay_LoanIssue.N_CompanyID = Pay_Employee.N_CompanyID AND Pay_LoanIssue.N_FnYearID = Pay_Employee.N_FnYearID where Pay_LoanIssue.N_LoanID=@nLoanID and Pay_LoanIssue.N_CompanyID=@nCompanyID";

                    dt = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);

                    if (RefundAmount != null)
                    {
                        dt = myFunctions.AddNewColumnToDataTable(dt, "N_Amount", typeof(double), myFunctions.getVAL(RefundAmount.ToString()));
                    }
                    else
                    {
                        dt = myFunctions.AddNewColumnToDataTable(dt, "N_Amount", typeof(double), 0);

                    }
                    if (count != null)
                    {
                        dt = myFunctions.AddNewColumnToDataTable(dt, "N_Count", typeof(double), myFunctions.getVAL(count.ToString()));
                    }
                    else
                    {
                        dt = myFunctions.AddNewColumnToDataTable(dt, "N_Count", typeof(double), 0);

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
                return Ok(api.Error(User,e));
            }
        }

        [HttpPost("save")]
        public ActionResult SaveLoanRequest([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                DataTable Approvals;
                MasterTable = ds.Tables["master"];
                Approvals = ds.Tables["approval"];
                SortedList Params = new SortedList();
                SortedList QueryParams = new SortedList();
                // Auto Gen
                DataRow MasterRow = MasterTable.Rows[0];

                int nUserID = myFunctions.GetUserID(User);


                string xLoanID = MasterRow["n_LoanID"].ToString();
                int nLoanTransID = myFunctions.getIntVAL(MasterRow["n_LoanTransID"].ToString());
                int nCompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyId"].ToString());
                int nFnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearId"].ToString());
                int nEmpID = myFunctions.getIntVAL(MasterRow["n_EmpID"].ToString());
                var dDateFrom = MasterRow["d_LoanPeriodFrom"].ToString();
                var dLoanPeriodTo = MasterRow["d_LoanPeriodTo"].ToString();
                var dLoanIssueDate = MasterRow["d_LoanIssueDate"].ToString();
                //double nAmount = MasterRow["n_Amount"].ToString();
                double n_LoanAmount = myFunctions.getVAL(MasterRow["n_LoanAmount"].ToString());
                 string xButtonAction="";
                QueryParams.Add("@nCompanyID", nCompanyID);
                QueryParams.Add("@nFnYearID", nFnYearID);
                QueryParams.Add("@nEmpID", nEmpID);
                int N_NextApproverID = 0;
                int nFormID = myFunctions.getIntVAL(MasterRow["n_FormID"].ToString());
                //QueryParams.Add("@nLoanTransID", nLoanTransID);
                if (MasterTable.Columns.Contains("n_Amount"))
                    MasterTable.Columns.Remove("n_Amount");

                  if (MasterTable.Columns.Contains("n_FormID"))
                    MasterTable.Columns.Remove("n_FormID");   



                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList EmpParams = new SortedList();
                    EmpParams.Add("@nCompanyID", nCompanyID);
                    EmpParams.Add("@nEmpID", nEmpID);
                    EmpParams.Add("@nFnYearID", nFnYearID);

                    object objEmpName = dLayer.ExecuteScalar("Select X_EmpName From Pay_Employee where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID", EmpParams, connection, transaction);
                    
                    if(Approvals.Rows.Count > 0){
                        DataRow ApprovalRow = Approvals.Rows[0];
                    if (!myFunctions.getBoolVAL(ApprovalRow["isEditable"].ToString()))
                    {
                        int N_PkeyID = nLoanTransID;
                        string X_Criteria = "N_LoanTransID=" + N_PkeyID + " and N_CompanyID=" + nCompanyID + " and N_FnYearID=" + nFnYearID;
                        myFunctions.UpdateApproverEntry(Approvals, "Pay_LoanIssue", X_Criteria, N_PkeyID, User, dLayer, connection, transaction);
                        N_NextApproverID = myFunctions.LogApprovals(Approvals, nFnYearID, this.xTransType, N_PkeyID, xLoanID, 1, objEmpName.ToString(), 0, "",0, User, dLayer, connection, transaction);
                        transaction.Commit();
                        //myFunctions.SendApprovalMail(N_NextApproverID, FormID, nLoanTransID, this.xTransType, xLoanID, dLayer, connection, transaction, User);
                        return Ok(api.Success("Loan Request Approved " + "-" + xLoanID));
                    }
                    }


                  
                        if (!EligibleForLoan(dLoanIssueDate, QueryParams, connection, transaction))
                        {
                            transaction.Rollback();
                            return Ok(api.Warning("Not Eligible For Loan!"));
                        }
                        if (!checkSalaryProcess(dDateFrom, nCompanyID, dLoanPeriodTo, nFnYearID, nEmpID, QueryParams, connection, transaction))
                        {
                            transaction.Rollback();
                            return Ok(api.Warning("Salary Already Processed!"));
                        }
                        if (LoanCountLimitExceed(QueryParams, connection, transaction))
                        {
                            transaction.Rollback();
                            return Ok(api.Warning("Loan Limit Exceeded!"));
                        }
                        if (!checkSalaryProcess(dDateFrom, nCompanyID, dLoanPeriodTo, nFnYearID, nEmpID, QueryParams, connection, transaction))
                        {
                            transaction.Rollback();
                            return Ok(api.Warning("Salary Already Processed!"));
                        }
                        object loanLimitAmount = dLayer.ExecuteScalar("SELECT N_LoanAmountLimit From Pay_Employee Where N_CompanyID=" + nCompanyID + " and N_EmpId = " + nEmpID +" and N_FnyearID="+nFnYearID, Params, connection, transaction);//----Credit Balance

                            if(loanLimitAmount != null)
                            if (!checkMaxAmount(n_LoanAmount, nCompanyID, nFnYearID, nEmpID, QueryParams, connection, transaction))
                            {
                                transaction.Rollback();
                                return Ok(api.Warning("Maximum Loan Amount is" + " : " + loanLimitAmount.ToString()));
                            }
                  if (xLoanID == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", this.FormID);
                        xLoanID = dLayer.GetAutoNumber("Pay_LoanIssue", "n_LoanID", Params, connection, transaction);
                        xButtonAction="Insert"; 
                        if (xLoanID == "") { transaction.Rollback(); return Ok(api.Error(User,"Unable to generate Loan ID")); }
                        MasterTable.Rows[0]["n_LoanID"] = xLoanID;
                    }
                    else
                    {
                            xButtonAction="Update"; 
                           xLoanID = MasterTable.Rows[0]["n_LoanID"].ToString();
                        dLayer.DeleteData("Pay_LoanIssueDetails", "n_LoanTransID", nLoanTransID, "", connection, transaction);
                        dLayer.DeleteData("Pay_LoanIssue", "n_LoanTransID", nLoanTransID, "", connection, transaction);
                    }

                    decimal nInstAmount = myFunctions.getDecimalVAL(MasterTable.Rows[0]["n_InstallmentAmount"].ToString());
                    int nInstNos = myFunctions.getIntVAL(MasterTable.Rows[0]["n_Installments"].ToString());
                    MasterTable.Columns.Remove("n_InstallmentAmount");
                    MasterTable.AcceptChanges();
                    
                    if(Approvals.Rows.Count > 0){
                    MasterTable = myFunctions.SaveApprovals(MasterTable, Approvals, dLayer, connection, transaction);
                    }
                    nLoanTransID = dLayer.SaveData("Pay_LoanIssue", "n_LoanTransID", MasterTable, connection, transaction);
                    if (nLoanTransID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User,"Unable to save Loan Request"));
                    }
                    else
                    {

                        if(Approvals.Rows.Count > 0){
                        N_NextApproverID = myFunctions.LogApprovals(Approvals, nFnYearID, this.xTransType, nLoanTransID, xLoanID, 1, objEmpName.ToString(), 0, "",0, User, dLayer, connection, transaction);
                        }
                        DataTable dt = new DataTable();
                        dt.Clear();
                        dt.Columns.Add("N_LoanTransDetailsID");
                        dt.Columns.Add("N_CompanyID");
                        dt.Columns.Add("N_LoanTransID");
                        dt.Columns.Add("D_DateFrom");
                        dt.Columns.Add("D_DateTo");
                        dt.Columns.Add("N_InstAmount");

                        DateTime Start = new DateTime(Convert.ToDateTime(dDateFrom.ToString()).Year, Convert.ToDateTime(dDateFrom.ToString()).Month, 1);


                        for (int i = 1; i <= nInstNos; i++)
                        {
                            DateTime End = new DateTime(Start.AddMonths(1).Year, Start.AddMonths(1).Month, 1).AddDays(-1);
                            DataRow row = dt.NewRow();
                            row["N_LoanTransDetailsID"] = 0;
                            row["N_CompanyID"] = nCompanyID;
                            row["N_LoanTransID"] = nLoanTransID;
                            row["D_DateFrom"] = myFunctions.getDateVAL(Start);
                            row["D_DateTo"] = myFunctions.getDateVAL(End);
                            row["N_InstAmount"] =Math.Floor(nInstAmount); //myFunctions.getIntVAL(Math.Round(Convert.ToDouble(nInstAmount)).ToString());
                            dt.Rows.Add(row);
                            Start = Start.AddMonths(1);
                        }
                 

                        int N_LoanTransDeatilsID = dLayer.SaveData("Pay_LoanIssueDetails", "N_LoanTransDetailsID", dt, connection, transaction);
                        if(nFormID==1382)
                        {
                        dLayer.ExecuteNonQuery("update Pay_LoanIssue set N_StatusID=1 where N_CompanyID=" + nCompanyID + " and N_LoanTransID=" + nLoanTransID, Params, connection, transaction);
                        }
                        if (N_LoanTransDeatilsID <= 0)
                        {
                            transaction.Rollback();
                            return Ok(api.Error(User,"Unable to save Loan Request"));
                        }
                        double loandecimalAmt= myFunctions.getIntVAL(Math.Floor(nInstAmount).ToString())*nInstNos;
                        double n_LoaninstAmount= myFunctions.getIntVAL(Math.Floor(nInstAmount).ToString()) + n_LoanAmount-loandecimalAmt;

                        dLayer.ExecuteNonQuery("update Pay_LoanIssueDetails set n_InstAmount="+n_LoaninstAmount+" where N_CompanyID=" + nCompanyID + " and N_LoanTransDetailsID = (select MAX (N_LoanTransDetailsID) from Pay_LoanIssueDetails where N_CompanyID="+nCompanyID+" and N_LoanTransId ="+nLoanTransID+" )", Params, connection, transaction);
                        //Activity Log
                string ipAddress = "";
                if (  Request.Headers.ContainsKey("X-Forwarded-For"))
                    ipAddress = Request.Headers["X-Forwarded-For"];
                else
                    ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                       myFunctions.LogScreenActivitys(nFnYearID,nLoanTransID,xLoanID,212,xButtonAction,ipAddress,"",User,dLayer,connection,transaction);
                       

                        transaction.Commit();
                        //myFunctions.SendApprovalMail(N_NextApproverID, FormID, nLoanTransID, this.xTransType, xLoanID, dLayer, connection, transaction, User);
                    }
                    return Ok(api.Success("Loan request saved" + ":" + xLoanID));
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }
        }


        [HttpDelete()]
        public ActionResult DeleteData(int nLoanTransID, int nFnYearID,int nFormID, string comments)
        {
                        if (comments == null)
            {
                comments = "";
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable TransData = new DataTable();
                    SortedList ParamList = new SortedList();
                    ParamList.Add("@nTransID", nLoanTransID);
                    ParamList.Add("@nFnYearID", nFnYearID);
                    ParamList.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    string Sql = "select isNull(N_UserID,0) as N_UserID,isNull(N_ProcStatus,0) as N_ProcStatus,isNull(N_ApprovalLevelId,0) as N_ApprovalLevelId,isNull(N_EmpID,0) as N_EmpID,N_loanID from Pay_LoanIssue where N_CompanyId=@nCompanyID and N_FnYearID=@nFnYearID and N_LoanTransID=@nTransID";
                    string xButtonAction="Delete";
                    string n_LoanID="";
                    TransData = dLayer.ExecuteDataTable(Sql, ParamList, connection);
                    if (TransData.Rows.Count == 0)
                    {
                        return Ok(api.Error(User,"Transaction not Found"));
                    }
                    DataRow TransRow = TransData.Rows[0];
                    int EmpID = myFunctions.getIntVAL(TransRow["N_EmpID"].ToString());
                    SortedList EmpParams = new SortedList();
                    EmpParams.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    EmpParams.Add("@nEmpID", EmpID);
                    EmpParams.Add("@nFnYearID", nFnYearID);
                    object objEmpName = dLayer.ExecuteScalar("Select X_EmpName From Pay_Employee where N_EmpID=@nEmpID and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID", EmpParams, connection);


                    DataTable Approvals = myFunctions.ListToTable(myFunctions.GetApprovals(-1, nFormID, nLoanTransID, myFunctions.getIntVAL(TransRow["N_UserID"].ToString()), myFunctions.getIntVAL(TransRow["N_ProcStatus"].ToString()), myFunctions.getIntVAL(TransRow["N_ApprovalLevelId"].ToString()), 0, 0, 1, nFnYearID, myFunctions.getIntVAL(TransRow["N_EmpID"].ToString()), 2001, User, dLayer, connection));
                    Approvals = myFunctions.AddNewColumnToDataTable(Approvals, "comments", typeof(string), comments);
                    SqlTransaction transaction = connection.BeginTransaction(); ;

                    string X_Criteria = "N_LoanTransID=" + nLoanTransID + " and N_CompanyID=" + myFunctions.GetCompanyID(User) + " and N_FnYearID=" + nFnYearID;

                    string ButtonTag = Approvals.Rows[0]["deleteTag"].ToString();
                    int ProcStatus = myFunctions.getIntVAL(ButtonTag.ToString());
                    //myFunctions.getIntVAL(TransRow["N_ProcStatus"].ToString())
                                                   //  Activity Log
                string ipAddress = "";
                if (  Request.Headers.ContainsKey("X-Forwarded-For"))
                    ipAddress = Request.Headers["X-Forwarded-For"];
                else
                    ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                       myFunctions.LogScreenActivitys(myFunctions.getIntVAL( nFnYearID.ToString()),nLoanTransID,TransRow["n_LoanID"].ToString(),212,xButtonAction,ipAddress,"",User,dLayer,connection,transaction);
             


                  
                    string status = myFunctions.UpdateApprovals(Approvals, nFnYearID, this.xTransType, nLoanTransID, TransRow["N_loanID"].ToString(), ProcStatus, "Pay_LoanIssue", X_Criteria, objEmpName.ToString(), User, dLayer, connection, transaction);
                    if (status != "Error")
                    {
                        transaction.Commit();
                        return Ok(api.Success("Loan Request " + status + " Successfully"));
                    }
                    else
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User,"Unable to delete Loan request"));
                    }


                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }


        }


        private bool LoanCountLimitExceed(SortedList Params, SqlConnection connection, SqlTransaction transaction)
        {
            int N_EmpLoanCount = 0, N_LoanLimitCount = 0;
            object obj = dLayer.ExecuteScalar("SELECT N_LoanCountLimit From Pay_Employee Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_EmpId =@nEmpID", Params, connection, transaction);
            if (obj == null)return false;

            N_LoanLimitCount = myFunctions.getIntVAL(obj.ToString());
            object EmpLoanCount = dLayer.ExecuteScalar("SELECT isnull(COUNT(N_LoanTransID),0) From Pay_LoanIssue Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_EmpId =@nEmpID", Params, connection, transaction);
            if (EmpLoanCount != null)
                N_EmpLoanCount = myFunctions.getIntVAL(EmpLoanCount.ToString());

            else if ((N_EmpLoanCount + 1) > N_LoanLimitCount )
            {
                return true;
            }
            return false;
        }

        private bool checkSalaryProcess(string fromDate, int nCompanyID, string dLoanPeriodTo, int nFnYearID, int nEmpID, SortedList Params, SqlConnection connection, SqlTransaction transaction)
        {
            DateTime dtpEffectiveDateFrom = Convert.ToDateTime(fromDate.ToString());
            DateTime dtpEffectiveDateTo = Convert.ToDateTime(dLoanPeriodTo.ToString());
            DateTime D_SalaryProcessDate = dtpEffectiveDateFrom;



            int N_MaxParunID = myFunctions.getIntVAL(Convert.ToString(dLayer.ExecuteScalar("select ISNULL(MAX(Pay_PaymentMaster.N_PayRunID),0) AS N_MaxParunID from Pay_PaymentMaster where Pay_PaymentMaster.N_CompanyID=" + nCompanyID, Params, connection, transaction)));

            while (D_SalaryProcessDate <= dtpEffectiveDateTo)
            {
                String N_ParunID = D_SalaryProcessDate.Year.ToString("00##") + D_SalaryProcessDate.Month.ToString("0#");

                if ((N_MaxParunID != 0 && N_MaxParunID < myFunctions.getIntVAL(N_ParunID)) || (N_MaxParunID == 0))
                    break;

                int count = myFunctions.getIntVAL(Convert.ToString(dLayer.ExecuteScalar("select 1 from Pay_PaymentDetails inner join Pay_PaymentMaster on Pay_PaymentDetails.N_TransID= Pay_PaymentMaster.N_TransID  where Pay_PaymentDetails.N_CompanyID=" + nCompanyID + " and Pay_PaymentMaster.N_FnYearID=" + nFnYearID + "and Pay_PaymentDetails.N_EmpID =" + nEmpID.ToString() + " and (Pay_PaymentMaster.N_PayRunID = " + N_ParunID + ")", Params, connection, transaction)));

                if (count > 0)
                {
                    return false;
                }
                D_SalaryProcessDate = D_SalaryProcessDate.AddMonths(1);
            }

            return true;


        }


        private bool EligibleForLoan(string fromDate, SortedList Params, SqlConnection connection, SqlTransaction transaction)
        {
            object obj = null;
            double N_EmpLoanEligible = 0;
            DateTime D_HireDate = DateTime.Now;

            obj = dLayer.ExecuteScalar("SELECT isnull(N_LoanEligible,0) From Pay_Employee Where N_CompanyID=@nCompanyID and N_EmpId =@nEmpID and N_FnyearID=@nFnYearID", Params, connection, transaction);
            if (obj != null)
                N_EmpLoanEligible = myFunctions.getVAL(obj.ToString());
            object EmpHireDate = dLayer.ExecuteScalar("SELECT D_HireDate From Pay_Employee Where N_CompanyID=@nCompanyID and N_EmpId =@nEmpID and N_FnyearID=@nFnYearID", Params, connection, transaction);
            if (EmpHireDate != null)
                D_HireDate = Convert.ToDateTime(EmpHireDate.ToString());

            TimeSpan TS = Convert.ToDateTime(fromDate.ToString()) - D_HireDate;


            double Years = TS.TotalDays / 365.25;

            if (N_EmpLoanEligible > Years)
            {
                return false;
            }
            return true;
        }
        private bool checkMaxAmount(double n_LoanAmount, int nCompanyID, int nFnYearID, int nEmpID, SortedList Params, SqlConnection connection, SqlTransaction transaction)
        {
            object N_LoanLimitAmount1 = dLayer.ExecuteScalar("SELECT N_LoanAmountLimit From Pay_Employee Where N_CompanyID=" + nCompanyID + " and N_EmpId = " + nEmpID +" and N_FnyearID="+nFnYearID, Params, connection, transaction);//----Credit Balance
            
            if(N_LoanLimitAmount1==null || myFunctions.getVAL(N_LoanLimitAmount1.ToString())==0) return true;
            string xLoanLimitAmountS = N_LoanLimitAmount1.ToString();
            double N_LoanLimitAmount = myFunctions.getVAL(N_LoanLimitAmount1.ToString());

            if (n_LoanAmount > N_LoanLimitAmount)
            {
                return false;

            }



            return true;
        }

        [HttpGet("loanListAll")]
        public ActionResult GetEmployeeAllLoanRequest(int nFnYearID, int nPage, int nSizeperpage, string xSearchkey, string xSortBy,bool bAllBranchData,int nBranchID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();


            int nCompanyID = myFunctions.GetCompanyID(User);
            QueryParams.Add("@nCompanyID", nCompanyID);
            QueryParams.Add("@nFnYearID", nFnYearID);
            QueryParams.Add("@nBranchID", nBranchID);

            string sqlCommandText = "";
            string sqlCommandCount = "";
            string criteria="";
            int Count = (nPage - 1) * nSizeperpage;
            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_EmpName like'%" + xSearchkey + "%'or X_Remarks like'%" + xSearchkey + "%'or X_EmpCode like'%" + xSearchkey + "%'or N_LoanID like'%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_LoanID desc";
            else if(xSortBy.Contains("d_LoanIssueDate"))
                xSortBy =" order by cast(D_LoanIssueDate as DateTime) " + xSortBy.Split(" ")[1];
            else if(xSortBy.Contains("d_LoanPeriodFrom"))
                xSortBy =" order by cast(D_LoanPeriodFrom as DateTime) " + xSortBy.Split(" ")[1];
            else if(xSortBy.Contains("d_LoanPeriodTo"))
                xSortBy =" order by cast(D_LoanPeriodTo as DateTime) " + xSortBy.Split(" ")[1];
            else
                xSortBy = " order by " + xSortBy;

                if(!bAllBranchData){
                     criteria="and n_BranchID=@nBranchID";
                }

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") N_CompanyID,N_EmpID,X_EmpCode,X_EmpName,N_LoanTransID,N_LoanID,D_LoanIssueDate,D_EntryDate,X_Remarks,D_LoanPeriodFrom,D_LoanPeriodTo,N_LoanAmount,N_Installments,N_FnYearID,B_IsSaveDraft,X_Guarantor1,X_Guarantor2,N_FormID,x_Description,X_BranchName from vw_Pay_LoanIssueList where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + Searchkey + " " + criteria + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") N_CompanyID,N_EmpID,X_EmpCode,X_EmpName,N_LoanTransID,N_LoanID,D_LoanIssueDate,D_EntryDate,X_Remarks,D_LoanPeriodFrom,D_LoanPeriodTo,N_LoanAmount,N_Installments,N_FnYearID,B_IsSaveDraft,X_Guarantor1,X_Guarantor2,N_FormID,x_Description,X_BranchName from vw_Pay_LoanIssueList where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + Searchkey + " and N_LoanTransID not in (select top(" + Count + ") N_LoanTransID from vw_Pay_LoanIssueList where  N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + xSortBy + " ) " + criteria + xSortBy;

            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, QueryParams, connection);
                    sqlCommandCount = "select count(1) as N_Count from vw_Pay_LoanIssueList where  N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + Searchkey + "";
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, QueryParams, connection);
                    OutPut.Add("Details", api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                }
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
                return Ok(api.Error(User,e));
            }
        }



    }
}