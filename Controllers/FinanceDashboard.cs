using AutoMapper;
using SmartxAPI.Data;
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
    [Route("financeDashboard")]
    [ApiController]
    public class FinanceDashboard : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;

        public FinanceDashboard(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }
        [HttpGet("details")]
        public ActionResult GetDashboardDetails(int nFnYearId, int nBranchID,bool bAllBranchData )
        {
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);
            string crieteria="";
            string crieteria1="";
           string crieteriaReceiv="";
            if (bAllBranchData == true)
            {
            crieteria="";
            crieteria1="";
            crieteriaReceiv="";
            }
            else
            {
            crieteria=" and N_BranchID="+nBranchID;
            crieteria1=" and Acc_VoucherDetails.N_BranchID="+nBranchID;
            crieteriaReceiv=" and BranchID="+nBranchID;
            }
            string sqlReceivables = "select CAST(CONVERT(VARCHAR, CAST(sum(N_BalanceAmount) AS MONEY), 1) AS VARCHAR) as N_Amount from vw_InvReceivables where N_CompanyId = "+nCompanyID+"   and X_Type<>'SALES RETURN' and X_Type<>'SA'";
            string sqlPayables= "select CAST(CONVERT(VARCHAR, CAST(sum(N_BalanceAmount) AS MONEY), 1) AS VARCHAR) as N_Amount from vw_InvPayables where N_CompanyId = "+nCompanyID +"  and X_Type<>'PURCHASE RETURN' "+crieteria;
            string sqlProfitMargin= "select CAST(CONVERT(VARCHAR, CAST((((select sum(ABS(N_Amount)) from vw_AccVoucherDetailsMonthWise where X_Type='I' and N_CompanyID ="+nCompanyID+" and N_FnYearId= "+nFnYearId+crieteria+") -(select sum(ABS(N_Amount))  from vw_AccVoucherDetailsMonthWise where X_Type='E' and N_CompanyID ="+nCompanyID+" and N_FnYearId= "+nFnYearId+crieteria+"))  / (select sum(ABS(N_Amount)) from vw_AccVoucherDetailsMonthWise where X_Type='I' and N_CompanyID = "+nCompanyID+" and N_FnYearId= "+nFnYearId+crieteria+" ) * 100) AS MONEY), 1) AS VARCHAR) as N_ProfitMargin";

            string sqlMonthWiseData = "select X_Month,N_Expense,N_Income from vw_IncomeExpenseMonthWise where N_CompanyID ="+nCompanyID+" and N_FnYearId= "+nFnYearId+crieteria;
            string sqlIncomeExpense = "select (select CAST(CONVERT(VARCHAR, CAST(ABS(sum(N_Amount)) AS MONEY), 1) AS VARCHAR) from vw_AccVoucherDetailsMonthWise where X_Type='I' and N_CompanyID = "+nCompanyID+" and N_FnYearId= "+nFnYearId+crieteria+") as N_Income, (select CAST(CONVERT(VARCHAR, CAST(ABS(sum(N_Amount)) AS MONEY), 1) AS VARCHAR)  from vw_AccVoucherDetailsMonthWise where X_Type='E' and N_CompanyID = "+nCompanyID+" and N_FnYearId= "+nFnYearId+crieteria+") as N_Expense";
            string sqlAssetLiability = "SELECT Acc_VoucherDetails.N_CompanyID,Acc_VoucherDetails.N_FnYearID,ABS (sum(Acc_VoucherDetails.N_Amount)) as  N_Amount,case when vw_AccMastLedger.X_Type  = 'A' then 'Asset'  when vw_AccMastLedger.X_Type  = 'L' then 'Liability' end as X_Type FROM   Acc_VoucherDetails INNER JOIN vw_AccMastLedger ON Acc_VoucherDetails.N_CompanyID = vw_AccMastLedger.N_CompanyID AND Acc_VoucherDetails.N_LedgerID = vw_AccMastLedger.N_LedgerID AND Acc_VoucherDetails.N_FnYearID = vw_AccMastLedger.N_FnYearID where  vw_AccMastLedger.X_Type in ('A','L') and Acc_VoucherDetails.N_CompanyID ="+nCompanyID+"    and Acc_VoucherDetails.N_FnYearID = "+nFnYearId+ crieteria1+"  group by Acc_VoucherDetails.N_CompanyID,Acc_VoucherDetails.N_FnYearID,vw_AccMastLedger.X_Type,Acc_VoucherDetails.N_BranchID ";
            string sqlCashBalance = "SELECT sum(Acc_VoucherDetails.N_Amount) as  N_Amount,vw_AccMastLedger.N_CashBahavID FROM   Acc_VoucherDetails INNER JOIN vw_AccMastLedger ON Acc_VoucherDetails.N_CompanyID = vw_AccMastLedger.N_CompanyID AND Acc_VoucherDetails.N_LedgerID = vw_AccMastLedger.N_LedgerID AND Acc_VoucherDetails.N_FnYearID = vw_AccMastLedger.N_FnYearID where  vw_AccMastLedger.N_CashBahavID =4 and Acc_VoucherDetails.N_CompanyID ="+nCompanyID+"  and Acc_VoucherDetails.N_FnYearID = "+nFnYearId+crieteria1+" group by Acc_VoucherDetails.N_CompanyID,Acc_VoucherDetails.N_FnYearID,vw_AccMastLedger.N_CashBahavID,Acc_VoucherDetails.N_BranchID ";
            string sqlBankBalance = "SELECT sum(Acc_VoucherDetails.N_Amount) as  N_Amount,vw_AccMastLedger.N_CashBahavID FROM   Acc_VoucherDetails INNER JOIN vw_AccMastLedger ON Acc_VoucherDetails.N_CompanyID = vw_AccMastLedger.N_CompanyID AND Acc_VoucherDetails.N_LedgerID = vw_AccMastLedger.N_LedgerID AND Acc_VoucherDetails.N_FnYearID = vw_AccMastLedger.N_FnYearID where  vw_AccMastLedger.N_CashBahavID =5 and Acc_VoucherDetails.N_CompanyID ="+nCompanyID+"  and Acc_VoucherDetails.N_FnYearID = "+nFnYearId+crieteria1+"  group by Acc_VoucherDetails.N_CompanyID,Acc_VoucherDetails.N_FnYearID,vw_AccMastLedger.N_CashBahavID,Acc_VoucherDetails.N_BranchID ";

            // string sqlOpenQuotation = "SELECT count(1) as N_ThisMonth,sum(Cast(REPLACE(N_Amount,',','') as Numeric(10,2)) ) as TotalAmount FROM vw_InvSalesQuotationNo_Search WHERE MONTH(D_QuotationDate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_QuotationDate) = YEAR(CURRENT_TIMESTAMP)";
            // "select X_LeadSource,CAST(count(1) as varchar(50)) as N_Percentage from vw_CRMLeads group by X_LeadSource";
            // string sqlPipelineoppotunity = "select count(1) as N_Count from CRM_Opportunity where N_ClosingStatusID=0 or N_ClosingStatusID is null";
            //string sqlCurrentSales =""

            SortedList Data = new SortedList();
            DataTable Receivables = new DataTable();
            DataTable Payables = new DataTable();
            DataTable ProfitMargin = new DataTable();
            DataTable MonthWiseData = new DataTable();
            DataTable IncomeExpense = new DataTable();
            DataTable AssetLiability = new DataTable();
            DataTable CashBalance = new DataTable();
            DataTable BankBalance = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    Receivables = dLayer.ExecuteDataTable(sqlReceivables, Params, connection);
                    Payables = dLayer.ExecuteDataTable(sqlPayables, Params, connection);
                    ProfitMargin = dLayer.ExecuteDataTable(sqlProfitMargin, Params, connection);
                    MonthWiseData = dLayer.ExecuteDataTable(sqlMonthWiseData, Params, connection);
                    IncomeExpense = dLayer.ExecuteDataTable(sqlIncomeExpense, Params, connection);
                    AssetLiability = dLayer.ExecuteDataTable(sqlAssetLiability, Params, connection);
                    CashBalance = dLayer.ExecuteDataTable(sqlCashBalance, Params, connection);
                    BankBalance = dLayer.ExecuteDataTable(sqlBankBalance, Params, connection);
                }


                Receivables.AcceptChanges();
                Payables.AcceptChanges();
                ProfitMargin.AcceptChanges();
                MonthWiseData.AcceptChanges();
                IncomeExpense.AcceptChanges();
                AssetLiability.AcceptChanges();
                CashBalance.AcceptChanges();
                BankBalance.AcceptChanges();


                if (Receivables.Rows.Count > 0) Data.Add("receivableData", Receivables);
                if (Payables.Rows.Count > 0) Data.Add("payableData", Payables);
                if (ProfitMargin.Rows.Count > 0) Data.Add("profitMarginData", ProfitMargin);
                if (MonthWiseData.Rows.Count > 0) Data.Add("monthWiseData", MonthWiseData);
                if (IncomeExpense.Rows.Count > 0) Data.Add("incomeExpenseData", IncomeExpense);
                if (AssetLiability.Rows.Count > 0) Data.Add("assetLiabilityData", AssetLiability);
                if (CashBalance.Rows.Count > 0) Data.Add("cashBalanceData", CashBalance);
                if (BankBalance.Rows.Count > 0) Data.Add("bankBalanceData", BankBalance);


                return Ok(api.Success(Data));

            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }

        [HttpGet("trialBalancelist")]
        public ActionResult TrialBalanceList(int nComapanyID,int nFnYearID,int nBranchID,int nPage, int nSizeperpage, bool b_AllBranchData,DateTime d_Start,DateTime d_end, string xSearchkey, string xSortBy)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    DataTable dt = new DataTable();
                    DataTable tb = new DataTable();
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    int nUserID = myFunctions.GetUserID(User);
                    int Count = (nPage - 1) * nSizeperpage;
                    string sqlCommandText = "";
                    string Searchkey = "";
                    string d_Date =d_Start.ToString("dd-MMM-yyyy") + "|" + d_end.ToString("dd-MMM-yyyy")  + "|";

                    Params.Add("@p1", nCompanyID);
                    Params.Add("@p2", nFnYearID);
                    Params.Add("@p3", nUserID);
                    Params.Add("@p4",nBranchID);
                    Params.Add("@p5",d_Date);
                    
         
                    if (xSearchkey != null && xSearchkey.Trim() != "")
                        Searchkey = "and ( X_LedgerName like '%" + xSearchkey + "%' or X_LedgerCode like '%" + xSearchkey + "%' or N_Opening like '%" + xSearchkey + "%' or N_Debit like '%" + xSearchkey + "%' or N_Credit like '%" + xSearchkey + "%' or N_Balance like '%" + xSearchkey + "%' ) ";

                    // if (xSortBy == null || xSortBy.Trim() == "")
                    //     xSortBy = " order by N_LedgerID desc";
                    // else
                    //  {
                    //      switch (xSortBy.Split(" ")[0])
                    //        {
                    //     case "X_LedgerCode":
                    //     xSortBy = "X_LedgerCode " + xSortBy.Split(" ")[1];
                    //     break;
                    //     default: break;
                    //       }
                    //      xSortBy = " order by " + xSortBy;
                    //       }
                         if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " Order By X_Level";
                   else
                   {
                switch (xSortBy.Split(" ")[0])
                   {
                    case "X_LedgerCode":
                        xSortBy = "X_LedgerCode " + xSortBy.Split(" ")[1];
                        break;
                     default: break;
                     }
                xSortBy = " order by " + xSortBy;
                     }

                      dLayer.ExecuteNonQuery("delete from Acc_LedgerBalForReporting Where N_UserID=@p3 and N_CompanyID=@p1", Params, connection, transaction);

                       if (b_AllBranchData == true)
                            {
                                sqlCommandText = "SP_OpeningBalanceGenerate @p1,@p2,0,11,@p5,@p3,0";
                             
                            }
                            else
                            {
                                sqlCommandText = "SP_OpeningBalanceGenerate @p1,@p2,0,11,@p5,@p3,@p4";
                                
                            }
                    tb = dLayer.ExecuteDataTable(sqlCommandText,Params,connection,transaction);
                    tb = api.Format(tb,"Master");
                    
                    if (Count == 0)
                        sqlCommandText = "select top(" + nSizeperpage + ") N_FnYearID,X_Level,N_LedgerID,N_Type,N_CompanyID,N_UserID,X_LedgerCode,X_LedgerName,N_Opening,N_Debit,N_Credit,N_Balance from vw_Acc_LedgerBalForReporting where N_CompanyID=@p1 and N_FnYearID=@p2 and N_UserID=@p3 " + Searchkey + " group by N_FnYearID,N_LedgerID,X_Level,N_CompanyID,N_UserID,X_LedgerCode,X_LedgerName,N_Opening,N_Debit,N_Credit,N_Balance,N_Type   "+  xSortBy  ;
                    else
                        sqlCommandText = "select top(" + nSizeperpage + ") N_FnYearID,X_Level,N_LedgerID,N_Type,N_CompanyID,N_UserID,X_LedgerCode,X_LedgerName,N_Opening,N_Debit,N_Credit,N_Balance from vw_Acc_LedgerBalForReporting where N_CompanyID=@p1 and N_FnYearID=@p2 and N_UserID=@p3 " + Searchkey +   xSortBy +"and N_LedgerID not in (select top(" + Count + ") N_LedgerID from vw_Acc_LedgerBalForReporting where N_CompanyID=@p1 and N_FnYearID=@p2  " + Searchkey+ xSortBy +" ) group by N_FnYearID,N_LedgerID,X_Level,N_CompanyID,N_UserID,X_LedgerCode,X_LedgerName,N_Opening,N_Debit,N_Credit,N_Balance,N_Type " +xSortBy;
                    SortedList OutPut = new SortedList();

                    dt = dLayer.ExecuteDataTable(sqlCommandText , Params, connection);
                    dt= myFunctions.AddNewColumnToDataTable(dt, "X_GroupName", typeof(string), "");
                    dt= myFunctions.AddNewColumnToDataTable(dt, "X_GroupCode", typeof(string), "");
                    foreach (DataRow Avar in dt.Rows)
                    {
                        if(myFunctions.getIntVAL(Avar["N_Type"].ToString())==0)
                        {
                            Avar["X_GroupName"]=Avar["X_LedgerName"];
                            Avar["X_LedgerName"]="";
                             Avar["X_GroupCode"]=Avar["X_LedgerCode"];
                            Avar["X_LedgerCode"]="";
                        }
                      
                    }
                    dt.AcceptChanges();





                    string sqlCommandCount = "select count(1) as N_Count  from vw_Acc_LedgerBalForReporting where N_CompanyID=@p1 and N_FnYearID=@p2 " + Searchkey;
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("Details", api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                    if (dt.Rows.Count == 0)
                    {
                        return Ok(api.Warning("No Results Found"));
                    }
                    else
                    {
                        return Ok(api.Success(OutPut));
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(api.Error(User, e));
            }
        }
       [HttpGet("statementsOfAccounts")]
        public ActionResult TrialBalanceList(string xLedgerCode,int nFnYearID, int nPage, int nSizeperpage, bool b_AllBranchData, string xSearchkey, string xSortBy)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable dt = new DataTable();
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    int nUserID = myFunctions.GetUserID(User);
                    int Count = (nPage - 1) * nSizeperpage;
                    string sqlCommandText = "";
                    string Searchkey = "";
                    Params.Add("@p1", nCompanyID);
                    Params.Add("@p2", nFnYearID);
                    Params.Add("@p3", nUserID);
                    Params.Add("@p4", xLedgerCode);
         

                  object LedgeridObj = dLayer.ExecuteScalar("select N_LedgerID From Acc_MastLedger where X_LedgerCode = @p4 and N_CompanyID =@p1 and N_FnYearID=@p2",Params, connection);

                  int nLedgerID = myFunctions.getIntVAL(LedgeridObj.ToString());
                   Params.Add("@p5", nLedgerID);
                    if (xSearchkey != null && xSearchkey.Trim() != "")
                        Searchkey = " and ( X_LedgerName like '%" + xSearchkey + "%' or X_LedgerCode like '%" + xSearchkey + "%' or N_Opening like '%" + xSearchkey + "%' or N_Debit like '%" + xSearchkey + "%' or N_Credit like '%" + xSearchkey + "%' or N_Balance like '%" + xSearchkey + "%' ) ";

                    if (xSortBy == null || xSortBy.Trim() == "")
                        xSortBy = " order by N_LedgerID desc";
                    else
                        xSortBy = " order by " + xSortBy;

                    if (Count == 0)
                        sqlCommandText = "select top(" + nSizeperpage + ") * from vw_StatementsOfAccounts_Detailed  where N_CompanyID=@p1 and N_FnYearID=@p2 and N_LedgerID=@p5" + Searchkey ;
                    else
                        sqlCommandText = "select top(" + nSizeperpage + ") * from vw_StatementsOfAccounts_Detailed  where N_CompanyID=@p1 and N_FnYearID=@p2 and N_LedgerID=@p5 " + Searchkey + "and N_RowNumber not in (select top(" + Count + ") N_RowNumber from vw_StatementsOfAccounts_Detailed where N_CompanyID=@p1 and N_FnYearID=@p2 and N_LedgerID=@p5" + Searchkey+ " ) ";
                    SortedList OutPut = new SortedList();

                    dt = dLayer.ExecuteDataTable(sqlCommandText , Params, connection);
                    string sqlCommandCount = "select count(1) as N_Count  from vw_StatementsOfAccounts_Detailed  where N_CompanyID=@p1 and N_FnYearID=@p2 and N_LedgerID=@p5" + Searchkey;
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("Details", api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                    if (dt.Rows.Count == 0)
                    {
                        return Ok(api.Warning("No Results Found"));
                    }
                    else
                    {
                        return Ok(api.Success(OutPut));
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(api.Error(User, e));
            }
        }

    }
}