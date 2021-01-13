using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SmartxAPI.GeneralFunctions;
using System;
using System.Data;
using System.Collections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SmartxAPI.Controllers

{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("purchaseinvoice")]
    [ApiController]
    public class Inv_PurchaseInvoice : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int N_FormID;
        public Inv_PurchaseInvoice(IApiFunctions api, IDataAccessLayer dl, IMyFunctions fun, IConfiguration conf)
        {
            _api = api;
            dLayer = dl;
            myFunctions = fun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            N_FormID = 65;
        }


        [HttpGet("list")]
        public ActionResult GetPurchaseInvoiceList(int? nCompanyId, int nFnYearId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            DataTable CountTable = new DataTable();
            SortedList Params = new SortedList();
            DataSet dataSet = new DataSet();
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and [Invoice No] like '%" + xSearchkey + "%' or Vendor like '%" + xSearchkey + "%'";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_PurchaseID desc";
            else
                xSortBy = " order by " + xSortBy;

            int Count = (nPage - 1) * nSizeperpage;
            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") N_PurchaseID,[Invoice No],[Vendor Code],Vendor,[Invoice Date],InvoiceNetAmt from vw_InvPurchaseInvoiceNo_Search where N_CompanyID=@p1 and N_FnYearID=@p2 " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") N_PurchaseID,[Invoice No],[Vendor Code],Vendor,[Invoice Date],InvoiceNetAmt from vw_InvPurchaseInvoiceNo_Search where N_CompanyID=@p1 and N_FnYearID=@p2 " + Searchkey + " and N_PurchaseID not in (select top(" + Count + ") N_PurchaseID from vw_InvPurchaseInvoiceNo_Search where N_CompanyID=@p1 and N_FnYearID=@p2 " + xSortBy + " ) " + xSortBy;

            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    //dt = _api.Format(dt,"Details");
                    sqlCommandCount = "select count(*) as N_Count  from vw_InvPurchaseInvoiceNo_Search where N_CompanyID=@p1 and N_FnYearID=@p2";
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("Details", _api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                }
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Warning("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(OutPut));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }
        [HttpGet("listOrder")]
        public ActionResult GetPurchaseOrderList(int? nCompanyId, int nFnYearId, int nVendorId, bool showAllBranch)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            string sqlCommandText = "";
            sqlCommandText = "select top(30) N_PurchaseID,[Invoice No],[Vendor Code],Vendor,[Invoice Date],InvoiceNetAmt from vw_InvPurchaseInvoiceNo_Search where N_CompanyID=@p1 and N_FnYearID=@p2";

            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);

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
                    return StatusCode(200, _api.Warning("No Results Found"));
                }
                else
                {
                    return Ok(dt);
                }
            }
            catch (Exception e)
            {
                return StatusCode(404, _api.Error(e.Message));
            }
        }
        [HttpGet("listdetails")]
        public ActionResult GetPurchaseInvoiceDetails(int nCompanyId, int nFnYearId, string nPurchaseNO, bool showAllBranch, int nBranchId, string xPOrderNo)
        {
            DataSet dt = new DataSet();
            SortedList Params = new SortedList();
            DataTable dtPurchaseInvoice = new DataTable();
            DataTable dtPurchaseInvoiceDetails = new DataTable();
            int N_PurchaseID = 0;
            int N_POrderID = 0;

            Params.Add("@CompanyID", nCompanyId);
            Params.Add("@YearID", nFnYearId);
            Params.Add("@TransType", "PURCHASE");
            Params.Add("@BranchID", nBranchId);
            string X_MasterSql = "";
            string X_DetailsSql = "";

            if (nPurchaseNO != null)

            {
                Params.Add("@PurchaseNo", nPurchaseNO);
                X_MasterSql = "select * from vw_Inv_PurchaseDisp where N_CompanyID=@CompanyID and X_InvoiceNo=@PurchaseNo and N_FnYearID=@YearID and X_TransType=@TransType" + (showAllBranch ? "" : " and  N_BranchId=@BranchID");
            }
            else if (xPOrderNo != null)
            {
                Params.Add("@POrderNo", xPOrderNo);
                X_MasterSql = "select * from vw_Inv_PurchaseOrderAsInvoiceMaster where N_CompanyID=@CompanyID and X_POrderNo=@POrderNo and N_FnYearID=@YearID " + (showAllBranch ? "" : " and  N_BranchId=@BranchID");
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    dtPurchaseInvoice = dLayer.ExecuteDataTable(X_MasterSql, Params, connection);
                    if (dtPurchaseInvoice.Rows.Count == 0) { return Ok(_api.Warning("No Data Found")); }
                    dtPurchaseInvoice = _api.Format(dtPurchaseInvoice, "Master");
                    N_PurchaseID = myFunctions.getIntVAL(dtPurchaseInvoice.Rows[0]["N_PurchaseID"].ToString());
                    N_POrderID = myFunctions.getIntVAL(dtPurchaseInvoice.Rows[0]["N_POrderID"].ToString());


                    //PURCHASE INVOICE DETAILS
                    bool B_MRNVisible = myFunctions.CheckPermission(nCompanyId, 556, "Administrator", "X_UserCategory", dLayer, connection);

                    if (nPurchaseNO != null)
                    {
                        if (B_MRNVisible)
                            X_DetailsSql = "Select vw_InvPurchaseDetails.*,Inv_PurchaseOrder.X_POrderNo,Inv_MRN.X_MRNNo,dbo.SP_Cost(vw_InvPurchaseDetails.N_ItemID,vw_InvPurchaseDetails.N_CompanyID,'') As N_UnitLPrice ,dbo.SP_SellingPrice(vw_InvPurchaseDetails.N_ItemID,vw_InvPurchaseDetails.N_CompanyID) As N_UnitSPrice   from vw_InvPurchaseDetails Left Outer Join Inv_PurchaseOrder On vw_InvPurchaseDetails.N_POrderID=Inv_PurchaseOrder.N_POrderID Left Outer Join Inv_MRN On vw_InvPurchaseDetails.N_RsID=Inv_MRN.N_MRNID  Where vw_InvPurchaseDetails.N_CompanyID=@CompanyID and vw_InvPurchaseDetails.N_PurchaseID=" + N_PurchaseID + (showAllBranch ? "" : " and vw_InvPurchaseDetails.N_BranchId=@BranchID");
                        else
                            X_DetailsSql = "Select vw_InvPurchaseDetails.*,Inv_PurchaseOrder.X_POrderNo,dbo.SP_Cost(vw_InvPurchaseDetails.N_ItemID,vw_InvPurchaseDetails.N_CompanyID,'') As N_UnitLPrice ,dbo.SP_SellingPrice(vw_InvPurchaseDetails.N_ItemID,vw_InvPurchaseDetails.N_CompanyID) As N_UnitSPrice   from vw_InvPurchaseDetails Left Outer Join Inv_PurchaseOrder On vw_InvPurchaseDetails.N_POrderID=Inv_PurchaseOrder.N_POrderID Where vw_InvPurchaseDetails.N_CompanyID=@CompanyID and vw_InvPurchaseDetails.N_PurchaseID=" + N_PurchaseID + (showAllBranch ? "" : " and vw_InvPurchaseDetails.N_BranchId=@BranchID");
                    }
                    else if (xPOrderNo != null)
                    {
                        X_DetailsSql = "select * from vw_Inv_PurchaseOrderAsInvoiceDetails where N_CompanyID=@CompanyID and N_POrderID=" + N_POrderID + (showAllBranch ? "" : " and  N_BranchId=@BranchID");
                    }
                    dtPurchaseInvoiceDetails = dLayer.ExecuteDataTable(X_DetailsSql, Params, connection);

                    if (nPurchaseNO != null)
                    {
                        object RetQty = dLayer.ExecuteScalar("select X_CreditNoteNo from Inv_PurchaseReturnMaster where N_PurchaseId =" + N_PurchaseID + " and N_CompanyID=@CompanyID and N_FnYearID=@YearID", Params, connection);
                        if (RetQty != null)
                        {
                            dtPurchaseInvoice = myFunctions.AddNewColumnToDataTable(dtPurchaseInvoice, "IsReturnDone", typeof(bool), true);
                            dtPurchaseInvoice = myFunctions.AddNewColumnToDataTable(dtPurchaseInvoice, "X_ReturnCode", typeof(string), RetQty.ToString());
                        }
                        else
                        {
                            dtPurchaseInvoice = myFunctions.AddNewColumnToDataTable(dtPurchaseInvoice, "IsReturnDone", typeof(bool), false);
                            dtPurchaseInvoice = myFunctions.AddNewColumnToDataTable(dtPurchaseInvoice, "X_ReturnCode", typeof(string), "");
                        }
                    }
                    dt.Tables.Add(dtPurchaseInvoice);

                    dtPurchaseInvoiceDetails = _api.Format(dtPurchaseInvoiceDetails, "Details");
                    dt.Tables.Add(dtPurchaseInvoiceDetails);
                }

                return Ok(_api.Success(dt));

            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }

        //Save....
        [HttpPost("Save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {

            DataTable MasterTable;
            DataTable DetailTable;
            MasterTable = ds.Tables["master"];
            DetailTable = ds.Tables["details"];
            SortedList Params = new SortedList();
            // Auto Gen
            string InvoiceNo = "";
            DataRow masterRow = MasterTable.Rows[0];
            var values = masterRow["x_InvoiceNo"].ToString();
            int N_PurchaseID = 0;
            int N_SaveDraft = 0;
            int nUserID = myFunctions.GetUserID(User);
            int nCompanyID = myFunctions.GetCompanyID(User);
            int N_InvoiceId = 0;
            try
            {

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction();
                    N_PurchaseID = myFunctions.getIntVAL(masterRow["n_PurchaseID"].ToString());

                    if (N_PurchaseID > 0)
                    {
                        if (CheckProcessed(N_PurchaseID))
                            return Ok(_api.Error("Transaction Started!"));
                    }
                    if (values == "@Auto")
                    {
                        N_SaveDraft = myFunctions.getIntVAL(masterRow["b_IsSaveDraft"].ToString());

                        Params.Add("N_CompanyID", masterRow["n_CompanyId"].ToString());
                        Params.Add("N_YearID", masterRow["n_FnYearId"].ToString());
                        Params.Add("N_FormID", this.N_FormID);
                        Params.Add("N_BranchID", masterRow["n_BranchId"].ToString());

                        InvoiceNo = dLayer.GetAutoNumber("Inv_Purchase", "x_InvoiceNo", Params, connection, transaction);
                        if (InvoiceNo == "") { return Ok(_api.Error("Unable to generate Invoice Number")); }
                        MasterTable.Rows[0]["x_InvoiceNo"] = InvoiceNo;
                    }

                    if (N_PurchaseID > 0)
                    {
                        SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",masterRow["n_CompanyId"].ToString()},
                                {"X_TransType","PURCHASE"},
                                {"N_VoucherID",N_PurchaseID}};
                        dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_Accounts", DeleteParams, connection, transaction);
                    }

                    N_InvoiceId = dLayer.SaveData("Inv_Purchase", "N_PurchaseID", MasterTable, connection, transaction);

                    if (N_InvoiceId <= 0)
                    {
                        transaction.Rollback();
                    }
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {
                        DetailTable.Rows[j]["N_PurchaseID"] = N_InvoiceId;
                    }
                    int N_InvoiceDetailId = dLayer.SaveData("Inv_PurchaseDetails", "n_PurchaseDetailsID", DetailTable, connection, transaction);
                    if (N_InvoiceDetailId <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error("Unable to save Purchase Invoice!"));
                    }
                    if (N_SaveDraft == 0)
                    {
                        SortedList PostingMRNParam = new SortedList();
                        PostingMRNParam.Add("N_CompanyID", masterRow["n_CompanyId"].ToString());
                        PostingMRNParam.Add("N_PurchaseID", N_InvoiceId);
                        PostingMRNParam.Add("N_UserID", nUserID);
                        PostingMRNParam.Add("X_SystemName", "ERP Cloud");
                        PostingMRNParam.Add("X_UseMRN", "");
                        PostingMRNParam.Add("N_SaveDraft", N_SaveDraft);
                        PostingMRNParam.Add("N_MRNID", 0);

                        dLayer.ExecuteNonQueryPro("[SP_Inv_MRNposting]", PostingMRNParam, connection, transaction);


                        SortedList PostingParam = new SortedList();
                        PostingParam.Add("N_CompanyID", masterRow["n_CompanyId"].ToString());
                        PostingParam.Add("X_InventoryMode", "PURCHASE");
                        PostingParam.Add("N_InternalID", N_InvoiceId);
                        PostingParam.Add("N_UserID", nUserID);
                        PostingParam.Add("X_SystemName", "ERP Cloud");

                        dLayer.ExecuteNonQueryPro("SP_Acc_Inventory_Purchase_Posting", PostingParam, connection, transaction);
                    }
                    transaction.Commit();
                }
                SortedList Result = new SortedList();
                Result.Add("n_InvoiceID", N_InvoiceId);
                Result.Add("x_InvoiceNo", InvoiceNo);
                return Ok(_api.Success(Result, "Purchase Invoice Saved"));
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(ex));
            }
        }
        private bool CheckProcessed(int nPurchaseID)
        {
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);
            object AdvancePRProcessed = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string sqlCommand = "Select COUNT(N_TransID) From Inv_PaymentRequest Where  N_CompanyID=@p1 and N_TransID=@p2 and N_FormID=65";
                Params.Add("@p1", nCompanyID);
                Params.Add("@p2", nPurchaseID);
                AdvancePRProcessed = dLayer.ExecuteScalar(sqlCommand, Params, connection);

            }
            if (AdvancePRProcessed != null)
            {
                if (myFunctions.getIntVAL(AdvancePRProcessed.ToString()) > 0)
                {
                    return true;
                }
            }
            return false;
        }
        //Delete....
        [HttpDelete("delete")]
        public ActionResult DeleteData(int nPurchaseID)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);
            int Results = 0;
            if (CheckProcessed(nPurchaseID))
                return Ok(_api.Error("Transaction Started"));
            try
            {

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",nCompanyID},
                                {"X_TransType","PURCHASE"},
                                {"N_VoucherID",nPurchaseID},
                                {"N_UserID",nUserID},
                                {"X_SystemName","WebRequest"},
                                {"@B_MRNVisible","0"}};

                    Results = dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_PurchaseAccounts", DeleteParams, connection, transaction);
                    if (Results <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error("Unable to Delete PurchaseInvoice"));
                    }

                    transaction.Commit();
                    return Ok(_api.Success("Purchase invoice deleted"));

                }

            }
            catch (Exception ex)
            {
                return Ok(_api.Error(ex));
            }


        }


    }
}