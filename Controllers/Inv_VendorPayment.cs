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
    [Route("vendorpayment")]
    [ApiController]
    public class Inv_VendorPayment : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions api;
        private readonly string connectionString;
        private readonly IMyFunctions myFunctions;


        public Inv_VendorPayment(IDataAccessLayer dl, IApiFunctions apiFun, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            api = apiFun;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");

        }


        [HttpGet("list")]
        public ActionResult GetVendorPayment(int? nCompanyId, int nFnYearId,int nListID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int Count= (nListID - 1) * 30;
            string sqlCommandText ="";

            if(Count==0)
                 sqlCommandText = "select top(30) * from vw_InvPayment_Search where N_CompanyID=@p1 and N_FnYearID=@p2";
            else
                 sqlCommandText = "select top(30) * from vw_InvPayment_Search where N_CompanyID=@p1 and N_FnYearID=@p2 and n_PayReceiptID not in (select top("+ Count +") n_PayReceiptID from vw_InvPayment_Search where N_CompanyID=@p1 and N_FnYearID=@p2)";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);

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
                    return Ok(api.Warning("No Results Found"));
                }
                else
                {
                    return Ok(dt);
                }
            }
            catch (Exception e)
            {
                return StatusCode(403, api.Error(e));
            }
        }

        [HttpGet("details")]
        public ActionResult GetVendorPaymentDetails(int? nCompanyId, int nQuotationId, int nFnYearId)
        {
            DataSet dt = new DataSet();
            SortedList Params = new SortedList();

            string sqlCommandText = "select * from vw_InvSalesQuotationNo_Search where N_CompanyID=@p1 and N_FnYearID=@p2 and N_QuotationID=@p3";

            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);
            Params.Add("@p3", nQuotationId);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable Quotation = new DataTable();

                    Quotation = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    Quotation = api.Format(Quotation, "Master");
                    dt.Tables.Add(Quotation);

                    //Quotation Details

                    string sqlCommandText2 = "select * from vw_InvQuotationDetails where N_CompanyID=@p1 and N_FnYearID=@p2 and N_QuotationID=@p3";

                    DataTable QuotationDetails = new DataTable();
                    QuotationDetails = dLayer.ExecuteDataTable(sqlCommandText2, Params, connection);
                    QuotationDetails = api.Format(QuotationDetails, "Details");
                    dt.Tables.Add(QuotationDetails);
                }
                return Ok(dt);
            }
            catch (Exception e)
            {
                return StatusCode(403, api.Error(e));
            }
        }
        [HttpGet("payDetails")]
        public ActionResult GetVendorPayDetails(int nVendorID, int nFnYearId, string dTransDate, int nBranchID, bool bShowAllbranch, string xInvoiceNo, string xTransType)
        {
            SortedList OutPut = new SortedList();
            DataTable PayReceipt = new DataTable();
            DataTable PayInfo = new DataTable();

            string sql = "";
            int AllBranch = 0;
            int nPayReceiptID = 0;
            int nCompanyId = myFunctions.GetCompanyID(User);
            OutPut.Add("totalAmtDue", 0);
            OutPut.Add("totalBalance", 0);
            OutPut.Add("txnStarted", false);
            if (bShowAllbranch == true)
            {
                AllBranch = 1;
                nBranchID = 0;
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    if (bShowAllbranch == true)
                        sql = "SELECT  -1 * Sum(n_Amount)  as N_BalanceAmount from  vw_InvVendorStatement Where N_AccType=1 and isnull(N_PaymentMethod,0)<>1 and N_AccID=@nVendorID and N_CompanyID=@nCompanyID and  D_TransDate<=@dTransDate";
                    else
                        sql = "SELECT  -1 * Sum(n_Amount)  as N_BalanceAmount from  vw_InvVendorStatement Where N_AccType=1 and isnull(N_PaymentMethod,0)<>1 and N_AccID=@nVendorID and N_CompanyID=@nCompanyID and N_BranchId=@nBranchID and  D_TransDate<=@dTransDate";

                    if (xInvoiceNo != null && myFunctions.getIntVAL(xInvoiceNo) > 0)
                    {
                        SortedList proParams1 = new SortedList(){
                                {"N_CompanyID",nCompanyId},
                                {"X_VoucherNo",xInvoiceNo},
                                {"N_FnYearID",nFnYearId},
                                {"N_BranchID",nBranchID}};
                        PayInfo = dLayer.ExecuteDataTablePro("SP_Inv_InvPayReceipt_Disp", proParams1, connection);
                        if (PayInfo.Rows.Count > 0)
                        {
                            nPayReceiptID = myFunctions.getIntVAL(PayInfo.Rows[0]["N_PayReceiptId"].ToString());
                            xTransType = PayInfo.Rows[0]["X_Type"].ToString();
                            nVendorID = myFunctions.getIntVAL(PayInfo.Rows[0]["N_PartyID"].ToString());
                        }
                    }

                    SortedList paramList = new SortedList();
                    paramList.Add("@nVendorID", nVendorID);
                    paramList.Add("@dTransDate", dTransDate);
                    paramList.Add("@nBranchID", nBranchID);
                    paramList.Add("@nPayReceiptID", nPayReceiptID);
                    paramList.Add("@xTransType", xTransType);
                    paramList.Add("@nCompanyID", nCompanyId);
                    DataTable VendorBalance = dLayer.ExecuteDataTable(sql, paramList, connection);

                    if (VendorBalance.Rows.Count > 0)
                    {
                        OutPut["totalAmtDue"] = myFunctions.getVAL(VendorBalance.Rows[0]["N_BalanceAmount"].ToString());
                        if (myFunctions.getVAL(VendorBalance.Rows[0]["N_BalanceAmount"].ToString()) < 0)
                            OutPut["totalBalance"] = Convert.ToDouble(-1 * myFunctions.getVAL(VendorBalance.Rows[0]["N_BalanceAmount"].ToString()));
                        else if (myFunctions.getVAL(VendorBalance.Rows[0]["N_BalanceAmount"].ToString()) > 0)
                            OutPut["totalBalance"] = myFunctions.getVAL(VendorBalance.Rows[0]["N_BalanceAmount"].ToString());
                        else
                            OutPut["totalBalance"] = 0;
                    }

                    SortedList proParams2 = new SortedList(){
                                {"N_CompanyID",nCompanyId},
                                {"N_FnYearID",nFnYearId},
                                {"N_CustomerID",nVendorID},
                                {"N_PayReceiptId",nPayReceiptID},
                                {"D_InvoiceDate",dTransDate},
                                {"BranchFlag",AllBranch},
                                {"BranchId",nBranchID}};


                    if (nPayReceiptID > 0)
                    {

                        if (xTransType == "PA")
                        {
                            PayReceipt = dLayer.ExecuteDataTablePro("SP_Inv_InvPayReceipt_View", proParams2, connection);
                            if (PayReceipt.Rows.Count > 0)
                            {
                                object obj = dLayer.ExecuteScalar("Select isnull(Count(N_InventoryId),0) as CountExists from Inv_PayReceiptDetails where N_CompanyID=@nCompanyID and N_InventoryId<>N_PayReceiptId and N_InventoryId=@nPayReceiptID and X_TransType =@xTransType", paramList, connection);
                                if (obj != null)
                                {
                                    if (myFunctions.getIntVAL(obj.ToString()) > 0)
                                    {
                                        OutPut["txnStarted"] = true;
                                    }
                                }
                                // return Ok(api.Success(api.Format(PayReceipt,"details")));

                            }
                        }
                        else
                        {
                            PayReceipt = dLayer.ExecuteDataTablePro("SP_Inv_InvPayReceipt_View", proParams2, connection);
                        }
                    }
                    else
                    {
                        PayReceipt = dLayer.ExecuteDataTablePro("SP_Inv_InvPayReceipt_View", proParams2, connection);
                    }

                }

                PayReceipt = myFunctions.AddNewColumnToDataTable(PayReceipt, "n_DueAmount", typeof(double), 0);

                if (PayReceipt.Rows.Count > 0)
                {
                    double N_ListedAmtTotal = 0;
                    foreach (DataRow dr in PayReceipt.Rows)
                    {

                        double N_InvoiceDueAmt = myFunctions.getVAL(dr["N_Amount"].ToString()) + myFunctions.getVAL(dr["N_BalanceAmount"].ToString()) + myFunctions.getVAL(dr["N_DiscountAmt"].ToString());// +myFunctions.getVAL(dr["N_DiscountAmt"].ToString());
                        N_ListedAmtTotal += N_InvoiceDueAmt;
                        if (N_InvoiceDueAmt == 0) { dr.Delete(); continue; }
                        if (nPayReceiptID > 0 && (myFunctions.getVAL(dr["N_DiscountAmt"].ToString()) == 0 && myFunctions.getVAL(dr["N_Amount"].ToString()) == 0)) { dr.Delete(); continue; }
                        dr["n_DueAmount"] = N_InvoiceDueAmt.ToString(myFunctions.decimalPlaceString(2));
                    }
                }
                PayReceipt.AcceptChanges();
                return Ok(api.Success(new SortedList() { { "details", api.Format(PayReceipt) }, { "masterData", OutPut }, { "master", PayInfo } }));
            }
            catch (Exception e)
            {
                return BadRequest(api.Error(e));
            }
        }

        [HttpPost("Save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                DataTable DetailTable;
                MasterTable = ds.Tables["master"];
                DetailTable = ds.Tables["details"];
                SortedList Params = new SortedList();

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction;


                    // Auto Gen
                    string PorderNo = "";
                    if (MasterTable.Rows.Count > 0)
                    {

                    }
                    var x_VoucherNo = MasterTable.Rows[0]["x_VoucherNo"].ToString();
                    DataRow Master = MasterTable.Rows[0];
                    int nCompanyId = myFunctions.getIntVAL(Master["n_CompanyId"].ToString());

                    int n_PayReceiptID = myFunctions.getIntVAL(Master["n_PayReceiptID"].ToString());
                    string x_Type = MasterTable.Rows[0]["x_Type"].ToString();

                    transaction = connection.BeginTransaction();

                    if (x_VoucherNo == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyId);
                        Params.Add("N_YearID", Master["n_FnYearID"].ToString());
                        Params.Add("N_FormID", 80);
                        Params.Add("N_BranchID", Master["n_BranchID"].ToString());

                        PorderNo = dLayer.GetAutoNumber("Inv_PayReceipt", "x_VoucherNo", Params, connection, transaction);
                        if (PorderNo == "") { return Ok(api.Warning("Unable to generate Receipt Number")); }
                        MasterTable.Rows[0]["x_VoucherNo"] = PorderNo;
                    }
                    else
                    {

                        if (n_PayReceiptID > 0)
                        {

                            SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",nCompanyId},
                                {"X_TransType",x_Type},
                                {"N_VoucherID",n_PayReceiptID}};
                            dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_Accounts", DeleteParams, connection, transaction);


                            // if (myFunctions.CheckPermission(nCompanyId, 576, "Administrator", dLayer, connection, transaction))
                            // {
                            //     dLayer.DeleteData("Inv_PurchasePaymentStatus", "N_PaymentID",)
                            //     dba.DeleteDataNoTry("Inv_PurchasePaymentStatus", "N_PaymentID", PayReceiptId_Loc.ToString(), "N_CompanyID = " + myCompanyID._CompanyID + " and N_FnYearID=" + myCompanyID._FnYearID);
                            //     dba.DeleteDataNoTry("Inv_PaymentDetails", "N_PaymentID", PayReceiptId_Loc.ToString(), "N_CompanyID = " + myCompanyID._CompanyID + " and N_FnYearID=" + myCompanyID._FnYearID);

                            // }



                        }
                    }


                    n_PayReceiptID = dLayer.SaveData("Inv_PayReceipt", "n_PayReceiptID", MasterTable, connection, transaction);
                    if (n_PayReceiptID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error("Error"));
                    }
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {
                        DetailTable.Rows[j]["n_PayReceiptID"] = n_PayReceiptID;
                    }
                    int n_PayReceiptDetailId = dLayer.SaveData("Inv_PayReceiptDetails", "n_PayReceiptDetailsID", DetailTable, connection, transaction);
                    transaction.Commit();
                }
                return Ok(api.Success("Vendor Payment Saved"));
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }
        [HttpDelete]
        public ActionResult DeleteData(int nPayReceiptId, string xTransType)
        {
           try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    if (nPayReceiptId > 0)
                    {
                        SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",myFunctions.GetCompanyID(User)},
                                {"X_TransType",xTransType},
                                {"N_VoucherID",nPayReceiptId}};
                        if(myFunctions.getBoolVAL(dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_Accounts", DeleteParams, connection).ToString())){
                            return Ok(api.Success("Vendor Payment Deleted"));
                        }
                    }
                }
                    return Ok(api.Warning("Unable to delete Vendor Payment"));

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }






        //  [HttpGet("dummy")]
        // public ActionResult GetPurchaseInvoiceDummy(int? Id)
        // {
        //     try
        //     {
        //         using (SqlConnection connection = new SqlConnection(connectionString))
        //         {
        //             connection.Open();
        //         string sqlCommandText = "select * from Inv_PayReceipt where N_PayReceiptId=@p1";
        //         SortedList mParamList = new SortedList() { { "@p1", Id } };
        //         DataTable masterTable = dLayer.ExecuteDataTable(sqlCommandText, mParamList,connection);
        //         masterTable = api.Format(masterTable, "master");

        //         string sqlCommandText2 = "select * from Inv_PayReceiptDetails where N_PayReceiptId=@p1";
        //         SortedList dParamList = new SortedList() { { "@p1", Id } };
        //         DataTable detailTable = dLayer.ExecuteDataTable(sqlCommandText2, dParamList,connection);
        //         detailTable = api.Format(detailTable, "details");

        //         if (detailTable.Rows.Count == 0) { return Ok(new { }); }
        //         DataSet dataSet = new DataSet();
        //         dataSet.Tables.Add(masterTable);
        //         dataSet.Tables.Add(detailTable);

        //         return Ok(dataSet);
        //         }

        //     }
        //     catch (Exception e)
        //     {
        //         return StatusCode(403, api.Error(e));
        //     }
        // }
    }



}