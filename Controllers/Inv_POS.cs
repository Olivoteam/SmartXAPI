using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SmartxAPI.GeneralFunctions;
using System;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.Security.Claims;

namespace SmartxAPI.Controllers

{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("pos")]
    [ApiController]
    public class Inv_POS : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int N_FormID;
        public Inv_POS(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            N_FormID = 64;
        }

        [HttpGet("holdlist")]
        public ActionResult GetInvoiceHoldList(int nFnYearId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy, string xDate)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and [Invoice No] like '%" + xSearchkey + "%' or Customer like '%" + xSearchkey + "%'";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_SalesId desc";
            else
                xSortBy = " order by " + xSortBy;


            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_InvSalesInvoiceNo_Search where  B_IsSaveDraft=1 and N_Hold=1 and D_SalesDate='" + xDate + "' and N_CompanyID=@p1 and N_FnYearID=@p2 " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_InvSalesInvoiceNo_Search where  B_IsSaveDraft=1 and N_Hold=1 and D_SalesDate='" + xDate + "' and N_CompanyID=@p1 and N_FnYearID=@p2 " + Searchkey + " and N_SalesID not in (select top(" + Count + ") N_SalesID from vw_InvSalesInvoiceNo_Search where N_CompanyID=@p1 and N_FnYearID=@p2 " + xSearchkey + xSortBy + " ) " + xSortBy;

            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(*) as N_Count  from vw_InvSalesInvoiceNo_Search where  B_IsSaveDraft=1 and N_Hold=1 and D_SalesDate='" + xDate + "' and N_CompanyID=@p1 and N_FnYearID=@p2 " + xSearchkey;
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
        [HttpGet("listcategory")]
        public ActionResult GetDepartmentList()
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nCompanyID", nCompanyID);

            string sqlCommandText = "Select N_CategoryID,X_Category from Inv_ItemCategory Where N_CompanyID= @nCompanyID";

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
         [HttpGet("items")]
        public ActionResult GetItems(int nCategoryID,string xSearchkey)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey="";
            if(xSearchkey!=null)
                Searchkey = "and X_ItemName like '%" + xSearchkey + "%'";

            if(nCategoryID>0)
                sqlCommandText = "select N_CompanyID, N_ItemID, X_ItemCode, X_ItemName, X_PartNo, B_InActive, N_LocationID, X_Category, N_ClassID, X_ClassName, N_BranchID, N_SPrice, N_CategoryID, X_Barcode,X_ItemName_a, N_ItemTypeID, N_TaxCategoryID, X_DisplayName, X_CategoryName, N_Amount, X_ItemUnit,'' as I_Image from vw_ItemPOSCloud where N_CompanyID=@p1 and X_ItemCode<>'001' and N_CategoryID=@p2"+ Searchkey;
            else
                sqlCommandText = "select N_CompanyID, N_ItemID, X_ItemCode, X_ItemName, X_PartNo, B_InActive, N_LocationID, X_Category, N_ClassID, X_ClassName, N_BranchID, N_SPrice, N_CategoryID, X_Barcode,X_ItemName_a, N_ItemTypeID, N_TaxCategoryID, X_DisplayName, X_CategoryName, N_Amount, X_ItemUnit,'' as I_Image from vw_ItemPOSCloud where N_CompanyID=@p1 and X_ItemCode<>'001'"+ Searchkey;

            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nCategoryID);
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(*) from vw_ItemPOS where N_CompanyID=@p1 and X_ItemCode<>'001' and N_CategoryID=@p2";
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
         [HttpGet("barcode")]
        public ActionResult GetItemsUsingBarcode(string xBarcode)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "";
            sqlCommandText = "select N_CompanyID, N_ItemID, X_ItemCode, X_ItemName, X_PartNo, B_InActive, N_LocationID, X_Category, X_ClassName, N_BranchID, N_SPrice, N_CategoryID, X_Barcode,X_ItemName_a, N_ItemTypeID, N_TaxCategoryID, X_DisplayName, X_CategoryName, N_Amount, X_ItemUnit,'' as I_Image from vw_ItemPOSCloud where N_CompanyID=@p1 and X_ItemCode<>'001' and x_barcode=@p2";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", xBarcode);
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    OutPut.Add("Details", _api.Format(dt));
                  
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
        [HttpGet("images")]
        public ActionResult GetImages(int nItemID)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "";
            sqlCommandText = "select X_ItemCode,I_Image from vw_ItemPOSCloud where N_CompanyID=@p1 and X_ItemCode<>'001' and N_ItemID=@p2";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nItemID);
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    if (dt.Rows[0]["I_Image"] != null)
                    {
                        dt = myFunctions.AddNewColumnToDataTable(dt, "Image", typeof(string), "");
                        DataRow dataRow = dt.Rows[0];
                        string ImageData = dataRow["I_Image"].ToString();
                        if (ImageData != "" || ImageData != "0x"  || ImageData != null)
                        {
                            byte[] Image = (byte[])dataRow["I_Image"];
                            if(Image.Length>0)
                            dt.Rows[0]["Image"] = "data:image/png;base64," + Convert.ToBase64String(Image, 0, Image.Length);
                           
                        }
                         dt.Columns.Remove("I_Image");
                        dt.AcceptChanges();
                    }
                    OutPut.Add("image",dt.Rows[0]["Image"]);
                    OutPut.Add("x_ItemCode",dt.Rows[0]["X_ItemCode"]);

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
        [HttpGet("listallitems")]
        public ActionResult GetAllItemDetails(int nFnYearId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy, string xDate, int nTerminalID, int nTerminalLocationID, int nCategory)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();

         
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string _sqlQuery = "";

                    if (nTerminalID > 0 && nTerminalLocationID > 0)
                        _sqlQuery = "SELECT * from vw_ItemPOSCloud where X_ItemCode<>'001' and N_LocationID=@nTerminalLocationID and N_CompanyID=@nCompanyID";
                    else
                        _sqlQuery = "SELECT * from vw_ItemPOSCloud where X_ItemCode<>'001' and N_CompanyID=@nCompanyID";
                    dt = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);


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


        [HttpPost("Save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                DataTable DetailTable;
                DataTable dtsaleamountdetails; ;
                MasterTable = ds.Tables["master"];
                DetailTable = ds.Tables["details"];
                dtsaleamountdetails = ds.Tables["saleamountdetails"];

                SortedList Params = new SortedList();
                SortedList QueryParams = new SortedList();
                // Auto Gen 
                string InvoiceNo = "";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction;
                    DataRow MasterRow = MasterTable.Rows[0];
                    transaction = connection.BeginTransaction();


                    int N_SalesID = myFunctions.getIntVAL(MasterRow["n_SalesID"].ToString());
                    int N_FnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearID"].ToString());
                    int N_CompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyID"].ToString());
                    int N_BranchID = myFunctions.getIntVAL(MasterRow["n_BranchID"].ToString());
                    int N_LocationID = myFunctions.getIntVAL(MasterRow["n_LocationID"].ToString());
                    int N_CustomerID = myFunctions.getIntVAL(MasterRow["n_CustomerID"].ToString());
                    int N_PaymentMethodID = myFunctions.getIntVAL(MasterRow["n_PaymentMethodID"].ToString());
                    int N_UserID = myFunctions.getIntVAL(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    int UserCategoryID = myFunctions.getIntVAL(User.FindFirst(ClaimTypes.GroupSid)?.Value);
                    int N_AmtSplit = 0;
                    int N_SaveDraft = myFunctions.getIntVAL(MasterRow["b_IsSaveDraft"].ToString());
                    bool B_AllBranchData = false, B_AllowCashPay = false, B_DirectPosting = false;


                    QueryParams.Add("@nCompanyID", N_CompanyID);
                    QueryParams.Add("@nFnYearID", N_FnYearID);
                    QueryParams.Add("@nSalesID", N_SalesID);
                    QueryParams.Add("@nBranchID", N_BranchID);
                    QueryParams.Add("@nLocationID", N_LocationID);
                    QueryParams.Add("@nCustomerID", N_CustomerID);
                    object DirectPosting=dLayer.ExecuteScalar("select B_DirPosting from Inv_Customer where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_CustomerID=@nCustomerID", QueryParams, connection, transaction);
                    if(DirectPosting!=null)
                        B_DirectPosting = myFunctions.getBoolVAL(DirectPosting.ToString());
                    object objAllBranchData = dLayer.ExecuteScalar("Select B_ShowAllData From Acc_BranchMaster where N_BranchID=@nBranchID and N_CompanyID=@nCompanyID", QueryParams, connection, transaction);
                    if (objAllBranchData != null)
                        B_AllBranchData = myFunctions.getBoolVAL(objAllBranchData.ToString());

                    if (B_AllBranchData)
                        B_AllowCashPay = myFunctions.getBoolVAL(dLayer.ExecuteScalar("select cast(count(N_CustomerID) as bit) from Inv_Customer where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_CustomerID=@nCustomerID  and N_AllowCashPay=1", QueryParams, connection, transaction).ToString());
                    else
                        B_AllowCashPay = myFunctions.getBoolVAL(dLayer.ExecuteScalar("select cast(count(N_CustomerID) as bit) from Inv_Customer where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_CustomerID=@nCustomerID  and N_AllowCashPay=1 and (N_BranchId=@nBranchID or N_BranchId=0)", QueryParams, connection, transaction).ToString());

                    // if (N_PaymentMethodID == 2 && B_AllowCashPay || B_POS)
                    // {
                    //     int count = myFunctions.getIntVAL(dLayer.ExecuteScalar("select count(N_CustomerID) from Inv_Customer where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and (N_BranchId=@nBranchID or N_BranchId=0) and N_EnablePopup=1", QueryParams, connection, transaction).ToString());
                    //     if (count > 0)
                    //     {
                    //         N_AmtSplit = 1;
                    //         //Filling sales amount details
                    //         //                            DataTable dtsaleamountdetails = new DataTable();
                    //         // if (ds.Tables.Contains("saleamountdetails"))
                    //         //     ds.Tables.Remove("saleamountdetails");
                    //         string qry = "";
                    //         if (N_SalesID > 0)
                    //         {
                    //             if (ds.Tables.Contains("saleamountdetails"))
                    //                 ds.Tables.Remove("saleamountdetails");
                    //             object ObjSaleAmountCustID = dLayer.ExecuteScalar("Select TOP (1) ISNULL(N_CustomerID,0) from vw_SalesAmount_Customer where N_SalesID=@nSalesID", QueryParams, connection, transaction);
                    //             if (ObjSaleAmountCustID != null)
                    //             {
                    //                 if (myFunctions.getIntVAL(ObjSaleAmountCustID.ToString()) == N_CustomerID)
                    //                     qry = "Select * from vw_SalesAmount_Customer where N_SalesID=@nSalesID";
                    //                 else
                    //                     qry = "Select * from vw_SalesAmount_Customer where N_SalesID=0";
                    //             }
                    //             else
                    //                 qry = "Select * from vw_SalesAmount_Customer where N_SalesID=0";
                    //             dtsaleamountdetails = dLayer.ExecuteDataTable(qry, QueryParams, connection, transaction);
                    //         }
                    //         // else
                    //         //     qry = "Select * from vw_SalesAmount_Customer where N_SalesID=0";

                    //         dtsaleamountdetails = _api.Format(dtsaleamountdetails, "saleamountdetails");
                    //         //ds.Tables.Add(dtsaleamountdetails);

                    //     }
                    // }
                    //saving data
                    var values = MasterRow["x_ReceiptNo"].ToString();
                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", MasterRow["n_CompanyId"].ToString());
                        Params.Add("N_YearID", MasterRow["n_FnYearId"].ToString());
                        Params.Add("N_FormID", this.N_FormID);
                        Params.Add("N_BranchID", MasterRow["n_BranchId"].ToString());
                        InvoiceNo = dLayer.GetAutoNumber("Inv_Sales", "x_ReceiptNo", Params, connection, transaction);
                        if (InvoiceNo == "") { transaction.Rollback(); return Ok(_api.Error("Unable to generate Invoice Number")); }
                        MasterTable.Rows[0]["x_ReceiptNo"] = InvoiceNo;
                    }
                    else
                    {
                        if (N_SalesID > 0)
                        {
                            SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",N_CompanyID},
                                {"X_TransType","SALES"},
                                {"N_VoucherID",N_SalesID}};
                            dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_Accounts", DeleteParams, connection, transaction);
                        }
                    }
                    N_SalesID = dLayer.SaveData("Inv_Sales", "N_SalesId", MasterTable, connection, transaction);
                    if (N_SalesID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error("Unable to save Sales Invoice!"));
                    }
                    // if (B_UserLevel)
                    // {
                    //     Inv_WorkFlowCatalog saving code here
                    // }
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {
                        DetailTable.Rows[j]["N_SalesId"] = N_SalesID;
                    }
                    int N_InvoiceDetailId = dLayer.SaveData("Inv_SalesDetails", "n_SalesDetailsID", DetailTable, connection, transaction);
                    if (N_InvoiceDetailId <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error("Unable to save Sales Invoice!"));
                    }
                    else
                    {


                        //Inv_WorkFlowCatalog insertion here
                        //DataTable dtsaleamountdetails = ds.Tables["saleamountdetails"];
                        DataTable dtloyalitypoints = ds.Tables["loyalitypoints"];
                        int N_IsSave = 1;
                        int N_CurrentSalesID = 0;
                        if (ds.Tables["saleamountdetails"].Rows.Count > 0)
                        {
                            DataRow Rowsaleamountdetails = ds.Tables["saleamountdetails"].Rows[0];
                            // N_IsSave = myFunctions.getIntVAL(Rowsaleamountdetails["n_IsSave"].ToString());
                            // dtsaleamountdetails.Columns.Remove("n_IsSave");
                            // dtsaleamountdetails.AcceptChanges();
                            N_CurrentSalesID = myFunctions.getIntVAL(Rowsaleamountdetails["N_SalesID"].ToString());
                        }

                        DataRow Rowloyalitypoints = null;
                        if (ds.Tables.Contains("loyalitypoints"))
                            Rowloyalitypoints = ds.Tables["loyalitypoints"].Rows[0];

                        // int N_IsSave = myFunctions.getIntVAL(Rowsaleamountdetails["n_IsSave"].ToString());
                        // dtsaleamountdetails.Columns.Remove("n_IsSave");
                        // dtsaleamountdetails.AcceptChanges();

                        // int N_CurrentSalesID = myFunctions.getIntVAL(Rowsaleamountdetails["N_SalesID"].ToString());
                        bool B_EnablePointSystem = Convert.ToBoolean(myFunctions.getIntVAL(myFunctions.ReturnSettings("64", "AllowLoyaltyPoint", "N_Value", "N_UserCategoryID", UserCategoryID.ToString(), N_CompanyID, dLayer, connection, transaction)));
                        bool B_SalesOrder = myFunctions.CheckPermission(N_CompanyID, 81, "Administrator","X_UserCategory", dLayer, connection, transaction);
                        //Sales amount details/payment popup
                        for (int i = 0; i < dtsaleamountdetails.Rows.Count; i++)
                            dtsaleamountdetails.Rows[i]["N_SalesId"] = N_SalesID;
                        if (N_AmtSplit == 1)
                        {

                            if (N_IsSave == 1)
                            {

                                int N_SalesAmountID = dLayer.SaveData("Inv_SaleAmountDetails", "n_SalesAmountID", dtsaleamountdetails, connection, transaction);
                                if (N_SalesAmountID <= 0)
                                {
                                    transaction.Rollback();
                                    return Ok(_api.Error("Unable to save Sales Invoice!"));
                                }
                                else
                                {
                                    if (B_EnablePointSystem)
                                    {
                                        if (ds.Tables.Contains("loyalitypoints") && dtloyalitypoints.Rows.Count > 0)
                                        {
                                            int N_PointOutId = dLayer.SaveData("Inv_LoyaltyPointOut", "n_PointOutId", dtloyalitypoints, connection, transaction);
                                            if (N_SalesAmountID <= 0)
                                            {
                                                transaction.Rollback();
                                                return Ok(_api.Error("Unable to save Sales Invoice!"));
                                            }
                                            else
                                            {
                                                double N_DiscountAmt = myFunctions.getVAL(Rowloyalitypoints["N_AppliedAmt"].ToString()) + myFunctions.getVAL(MasterRow["N_DiscountAmt"].ToString());
                                                dLayer.ExecuteNonQuery("update  Inv_Sales  Set N_DiscountAmt=" + N_DiscountAmt + " where N_SalesID=@nSalesID and N_CompanyID=@nCompanyID and N_CustomerID=@nCustomerID", QueryParams, connection, transaction);
                                            }
                                        }
                                    }
                                }
                            }
                            else if (N_IsSave == 0)
                            {
                                if (N_CurrentSalesID != N_SalesID)
                                    dLayer.ExecuteNonQuery("update  Inv_SaleAmountDetails set N_SalesID=" + N_SalesID + " where N_SalesID=@nSalesID and N_CompanyID=@nCompanyID and N_BranchID=@nBranchID", QueryParams, connection, transaction);
                            }
                        }
                        else
                        {
                            int N_SalesAmountID = dLayer.SaveData("Inv_SaleAmountDetails", "n_SalesAmountID", dtsaleamountdetails, connection, transaction);
                            if (N_SalesAmountID <= 0)
                            {
                                transaction.Rollback();
                                return Ok(_api.Error("Unable to save Sales Invoice!"));
                            }
                        }
                        for (int j = 0; j < DetailTable.Rows.Count; j++)
                        {
                            /*if (B_salesOrder == true)
                         {
                             if (myFunctions.getIntVAL(flxSales.get_TextMatrix(i, mcSalesOrderID)) > 0)
                             {
                                 dba.ExecuteNonQuery("Update Inv_SalesOrder Set N_SalesID=" + SalesId_Loc + ", N_Processed=1 Where N_SalesOrderID=" + flxSales.get_TextMatrix(i, mcSalesOrderID) + " and N_FnYearID=" + myCompanyID._FnYearID + " and N_CompanyID=" + myCompanyID._CompanyID.ToString(), "TEXT", new DataTable());
                                 if(B_ServiceSheet)
                                     dba.ExecuteNonQuery("Update Inv_ServiceSheetMaster Set N_Processed=1  Where N_RefID=" + flxSales.get_TextMatrix(i, mcSalesOrderID) + " and N_FnYearID=" + myCompanyID._FnYearID + " and N_CompanyID=" + myCompanyID._CompanyID.ToString(), "TEXT", new DataTable());

                             }

                         }
                         else
                         {
                             if (myFunctions.getIntVAL(flxSales.get_TextMatrix(i, mcQuotationID)) > 0)
                                 dba.ExecuteNonQuery("Update Inv_SalesQuotation Set N_SalesID=" + SalesId_Loc + ", N_Processed=1 Where N_QuotationID=" + flxSales.get_TextMatrix(i, mcQuotationID) + " and N_FnYearID=" + myCompanyID._FnYearID + " and N_CompanyID=" + myCompanyID._CompanyID.ToString(), "TEXT", new DataTable());
                         }*/
                        }
                        // Warranty Save Code here
                        //optical prescription saving here

                        if (N_SaveDraft == 0)
                        {
                            SortedList PostingParam = new SortedList();
                            PostingParam.Add("N_CompanyID", N_CompanyID);
                            PostingParam.Add("X_InventoryMode", "SALES");
                            PostingParam.Add("N_InternalID", N_SalesID);
                            PostingParam.Add("N_UserID", N_UserID);
                            PostingParam.Add("X_SystemName", "ERP Cloud");

                            dLayer.ExecuteNonQueryPro("SP_Acc_Inventory_Sales_Posting", PostingParam, connection, transaction);
                            bool B_AmtpaidEnable = Convert.ToBoolean(myFunctions.getIntVAL(myFunctions.ReturnSettings("Inventory", "Show SalesAmt Paid", "N_Value", "N_UserCategoryID", "0", N_CompanyID, dLayer, connection, transaction)));
                            if (B_AmtpaidEnable)
                            {
                                if (!B_DirectPosting)
                                {
                                    if (myFunctions.getVAL(MasterRow["N_CashReceived"].ToString()) > 0)
                                    {
                                        SortedList ParamCustomerRcpt_Ins = new SortedList();
                                        ParamCustomerRcpt_Ins.Add("N_CompanyID", N_CompanyID);
                                        ParamCustomerRcpt_Ins.Add("N_Fn_Year", N_FnYearID);
                                        ParamCustomerRcpt_Ins.Add("N_SalesId", N_SalesID);
                                        ParamCustomerRcpt_Ins.Add("N_Amount", myFunctions.getVAL(MasterRow["N_CashReceived"].ToString()));
                                        dLayer.ExecuteNonQueryPro("SP_CustomerRcpt_Ins", ParamCustomerRcpt_Ins, connection, transaction);
                                    }
                                }

                            }
                        }
                        //dispatch saving here
                        transaction.Commit();
                    }
                    //return GetSalesInvoiceDetails(int.Parse(MasterRow["n_CompanyId"].ToString()), int.Parse(MasterRow["n_FnYearId"].ToString()), int.Parse(MasterRow["n_BranchId"].ToString()), InvoiceNo);
                    return Ok(_api.Success("Sales invoice saved" + ":" + InvoiceNo));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(ex));
            }
        }
        //Delete....
        [HttpDelete("delete")]
        public ActionResult DeleteData(int nInvoiceID, int nCustomerID, int nCompanyID, int nFnYearID, int nBranchID, int nQuotationID)
        {
            int Results = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    int UserCategory = myFunctions.GetUserCategory(User);
                    int nUserID = myFunctions.GetUserID(User);
                    //Results = dLayer.DeleteData("Inv_SalesInvoice", "n_InvoiceID", N_InvoiceID, "",connection,transaction);
                    SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",nCompanyID},
                                {"N_UserID",nUserID},
                                {"X_TransType","SALES"},
                                {"X_SystemName","WebRequest"},
                                {"N_VoucherID",nInvoiceID}};

                    SortedList QueryParams = new SortedList(){
                                {"@nCompanyID",nCompanyID},
                                {"@nFnYearID",nFnYearID},
                                {"@nUserID",nUserID},
                                {"@xTransType","SALES"},
                                {"@xSystemName","WebRequest"},
                                {"@nSalesID",nInvoiceID},
                                {"@nPartyID",nCustomerID},
                                {"@nQuotationID",nQuotationID},
                                {"@nBranchID",nBranchID}};

                    Results = dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_SaleAccounts", DeleteParams, connection, transaction);
                    if (Results <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error("Unable to delete sales Invoice"));
                    }
                    else
                    {
                        dLayer.ExecuteNonQuery("delete from Inv_DeliveryDispatch where n_InvoiceID=@nSalesID and n_CompanyID=@nCompanyID", QueryParams, connection, transaction);
                        //   if (N_AmtSplit == 1)
                        //     {                                                
                        dLayer.ExecuteNonQuery("delete from Inv_SaleAmountDetails where n_SalesID=@nSalesID and n_BranchID=@nBranchID and n_CompanyID=@nCompanyID", QueryParams, connection, transaction);
                        dLayer.ExecuteNonQuery("delete from Inv_LoyaltyPointOut where n_SalesID=@nSalesID and n_PartyID=@nPartyID and n_CompanyID=@nCompanyID", QueryParams, connection, transaction);
                        // }                        
                        dLayer.ExecuteNonQuery("delete from Inv_ServiceContract where n_SalesID=@nSalesID and n_FnYearID=@nFnYearID and n_BranchID=@nBranchID and n_CompanyID=@nCompanyID", QueryParams, connection, transaction);
                        if (dLayer.ExecuteNonQuery("delete from Inv_StockMaster where n_SalesID=@nSalesID and x_Type='Negative' and n_InventoryID = 0 and n_CompanyID=@nCompanyID", QueryParams, connection, transaction) <= 0)
                        {
                            // transaction.Rollback();
                            // return Ok(_api.Error("Unable to delete sales Invoice"));
                        }
                        if (myFunctions.CheckPermission(nCompanyID, 724, "Administrator","X_UserCategory", dLayer, connection, transaction))
                            if (myFunctions.CheckPermission(nCompanyID, 81, UserCategory.ToString(),"N_UserCategoryID", dLayer, connection, transaction))
                                if (nQuotationID > 0)
                                    dLayer.ExecuteNonQuery("update Inv_SalesQuotation set N_Processed=0 where N_QuotationId= @nQuotationID and N_CompanyId=@nCompanyID and N_FnYearId= @nFnYearID", QueryParams, connection, transaction);
                    }
                    //Attachment delete code here

                    transaction.Commit();
                    return Ok(_api.Success("Sales invoice deleted"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(ex));
            }


        }


        [HttpGet("dummy")]
        public ActionResult GetSalesInvoiceDummy(int? nSalesId)
        {
            try
            {
                using (SqlConnection Con = new SqlConnection(connectionString))
                {
                    Con.Open();
                    string sqlCommandText = "select * from Inv_Sales where N_SalesId=@p1";
                    SortedList mParamList = new SortedList() { { "@p1", nSalesId } };
                    DataTable masterTable = dLayer.ExecuteDataTable(sqlCommandText, mParamList, Con);
                    masterTable = _api.Format(masterTable, "master");

                    string sqlCommandText2 = "select * from Inv_SalesDetails where N_SalesId=@p1";
                    SortedList dParamList = new SortedList() { { "@p1", nSalesId } };
                    DataTable detailTable = dLayer.ExecuteDataTable(sqlCommandText2, dParamList, Con);
                    detailTable = _api.Format(detailTable, "details");

                    string sqlCommandText3 = "select * from Inv_SaleAmountDetails where N_SalesId=@p1";
                    DataTable dtAmountDetails = dLayer.ExecuteDataTable(sqlCommandText3, dParamList, Con);
                    dtAmountDetails = _api.Format(dtAmountDetails, "saleamountdetails");

                    if (detailTable.Rows.Count == 0) { return Ok(new { }); }
                    DataSet dataSet = new DataSet();
                    dataSet.Tables.Add(masterTable);
                    dataSet.Tables.Add(detailTable);
                    dataSet.Tables.Add(dtAmountDetails);

                    return Ok(dataSet);

                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }





        [HttpGet("deliveryNoteSearch")]
        public ActionResult GetInvoiceList(int? nCompanyId, int nCustomerId, bool bAllBranchData, int nBranchId, int nLocationId)
        {
            SortedList Params = new SortedList();

            string crieteria = "";


            if (bAllBranchData == true)
            {
                if (nCustomerId > 0)
                    crieteria = " where X_TransType='DELIVERY' and N_DeliveryType = 0 and N_CustomerID=@nCustomerId and N_CompanyID=@nCompanyId and B_IsSaveDraft=0";
                else
                    crieteria = " where X_TransType='DELIVERY' and N_DeliveryType = 0 and N_CompanyID=@nCompanyId and B_IsSaveDraft=0";
            }
            else
            {
                if (nCustomerId > 0)
                    crieteria = " where X_TransType='DELIVERY' and N_DeliveryType = 0 and N_CustomerID=@nCustomerId and N_CompanyID=@nCompanyId and N_BranchID=@nBranchId and N_LocationID=@nLocationId and B_IsSaveDraft=0";
                else
                    crieteria = " where X_TransType='DELIVERY' and N_DeliveryType = 0 and N_CompanyID=@nCompanyId and N_BranchID=@nBranchId and N_LocationID=@nLocationId and B_IsSaveDraft=0";
            }

            Params.Add("@nCompanyId", nCompanyId);
            Params.Add("@nCustomerId", nCustomerId);
            Params.Add("@bAllBranchData", bAllBranchData);
            Params.Add("@nBranchId", nBranchId);
            Params.Add("@nLocationId", nLocationId);
            string sqlCommandText = "select [Invoice No],[Invoice Date],[Customer] as X_CustomerName,N_CompanyID,N_CustomerID,N_DeliveryNoteId,N_DeliveryType,X_TransType,N_FnYearID,N_BranchID,X_LocationName,N_LocationID,B_IsSaveDraft from vw_InvDeliveryNote_Search " + crieteria + " order by N_DeliveryNoteId DESC,[Invoice No]";
            try
            {
                DataTable SalesInvoiceList = new DataTable();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SalesInvoiceList = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    SalesInvoiceList = _api.Format(SalesInvoiceList);
                    if (SalesInvoiceList.Rows.Count == 0) { return Ok(_api.Notice("No Sales Invoices Found")); }
                }
                return Ok(_api.Success(SalesInvoiceList));
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }

        [HttpGet("updateSalesPrice")]
        public ActionResult ValidateSellingPrice(int nBranchID, int nItemID, decimal nSPrice)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SortedList Params = new SortedList();
                    Params.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    Params.Add("@nBranchID", nBranchID);
                    Params.Add("@nItemID", nItemID);
                    Params.Add("@nSPrice", nSPrice);
                    DataTable Sprice = dLayer.ExecuteDataTable("Select N_PriceID,N_PriceVal,N_itemId From Inv_ItemPriceMaster  Where N_CompanyID=@nCompanyID and N_BranchID=@nBranchID and N_itemId=@nItemID and N_PriceID in(Select N_PkeyId from Gen_LookupTable where X_Name=@nSPrice and N_CompanyID=@nCompanyID)", Params, connection);
                    return Ok(_api.Success(Sprice));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }


        [HttpGet("getItemDetails")]
        public ActionResult GetItem(int nLocationID, int nBranchID, string xInputVal, int nCustomerID, string xBatch, string xInvoiceNo)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                int N_DefSPriceID = 0;
                int nCompanyID = myFunctions.GetCompanyID(User);

                var UserCategoryID = myFunctions.GetUserCategory(User);
                N_DefSPriceID = myFunctions.getIntVAL(myFunctions.ReturnSettings("Inventory", "DefSPriceTypeID", "N_Value", "N_UserCategoryID", UserCategoryID.ToString(), myFunctions.getIntVAL(nCompanyID.ToString()), dLayer, connection));
                int nSPriceID = N_DefSPriceID;
                string ItemClass = "", ItemCondition = "";

                ItemCondition = "([Item Code] =@xItemCode)";

                if (ItemCondition != "")
                    ItemCondition += "and B_InActive=0";

                SortedList Params = new SortedList();
                Params.Add("@xItemCode", xInputVal);
                Params.Add("@nLocationID", nLocationID);
                Params.Add("@xBatch", xBatch == null ? "" : xBatch);
                Params.Add("@nCompanyID", nCompanyID);
                Params.Add("@nSPriceID", nSPriceID);
                Params.Add("@nDefSPriceID", N_DefSPriceID);
                Params.Add("@nBranchID", nBranchID);

                bool B_SPRiceType = false;

                object objSPrice = dLayer.ExecuteScalar("Select Isnull(N_Value,0) from Gen_Settings where N_CompanyID=@nCompanyID and X_Group='Inventory' and X_Description='Selling Price Calculation'", Params, connection);
                if (objSPrice != null)
                {
                    if (myFunctions.getIntVAL(objSPrice.ToString()) == 4)
                        B_SPRiceType = true;
                    else
                        B_SPRiceType = false;

                }

                string SQL = "";
                if (xBatch != "")
                    SQL = "Select *,dbo.SP_BatchStock(vw_InvItem_Search.N_ItemID,@nLocationID,@xBatch,NULL)As N_AvlStock ,0 As N_LPrice ,0 As N_SPrice,vw_InvItem_Search.N_TaxCategoryID, vw_InvItem_Search.X_DisplayName  From vw_InvItem_Search Where " + ItemCondition + " and N_CompanyID=@nCompanyID";
                else
                    SQL = "Select *,dbo.SP_GenGetStock(vw_InvItem_Search.N_ItemID,@nLocationID,@xBatch, 'location')As N_AvlStock ,0 As N_LPrice ,0 As N_SPrice,vw_InvItem_Search.N_TaxCategoryID, vw_InvItem_Search.X_DisplayName  From vw_InvItem_Search Where " + ItemCondition + " and N_CompanyID=@nCompanyID";
                if (B_SPRiceType)
                {
                    if (nSPriceID > 0)
                    {
                        SQL = "Select *,dbo.SP_GenGetStock(vw_InvItem_Search.N_ItemID,@nLocationID,@xBatch, 'location') As N_AvlStock ,0 As N_Stock ,0 As N_LPrice ,0 As N_SPrice  From vw_InvItem_Search Where " + ItemCondition + " and N_CompanyID=@nCompanyID";
                    }
                    else
                        SQL = "Select *,dbo.SP_GenGetStock(vw_InvItem_Search.N_ItemID,@nLocationID,@xBatch, 'location')As N_AvlStock ,0 As N_Stock ,dbo.SP_Cost_Loc(vw_InvItem_Search.N_ItemID,0 As N_LPrice ,0 As N_SPrice  From vw_InvItem_Search Where " + ItemCondition + " and N_CompanyID=@nCompanyID";
                }
                
                DataTable ItemDetails = dLayer.ExecuteDataTable(SQL, Params, connection);
                if (ItemDetails.Rows.Count != 1)
                    return Ok(_api.Warning("Invalid Item"));

                ItemClass = ItemDetails.Rows[0]["N_ClassID"].ToString();

                string Query = "select isnull(s.N_Qty,'0') from Inv_SalesDetails s inner join Inv_Sales sm on s.N_SalesID = sm.N_SalesId inner join Inv_ItemMaster i on s.N_ItemID = i.N_ItemID where i.X_ItemCode =@xItemCode and sm.X_ReceiptNo = '" + xInvoiceNo + "'";
                if (xBatch != "")
                    Query += "and X_BatchCode = '" + xBatch + "' ";
                object O_InvoicedQty = dLayer.ExecuteScalar(Query, Params, connection);
                int X_Stock = myFunctions.getIntVAL(ItemDetails.Rows[0]["N_AvlStock"].ToString());
                double stock = O_InvoicedQty != null ? myFunctions.getVAL(O_InvoicedQty.ToString()) + X_Stock : X_Stock;
                if (myFunctions.getIntVAL(ItemClass.ToString()) == 4)
                {
                    ItemDetails.Rows[0]["n_Qty"] = 1;
                }

                int N_ItemID = myFunctions.getIntVAL(ItemDetails.Rows[0]["N_ItemID"].ToString());
                object Mrp = dLayer.ExecuteScalar("Select top 1 N_Mrp from Inv_PurchaseDetails where N_ItemId=" + N_ItemID.ToString() + " and N_CompanyID=" + nCompanyID + " Order By N_PurchaseDetailsId desc", connection);
                myFunctions.AddNewColumnToDataTable(ItemDetails, "Mrp", typeof(decimal), Mrp);
                myFunctions.AddNewColumnToDataTable(ItemDetails, "stock", typeof(double), stock);
                myFunctions.AddNewColumnToDataTable(ItemDetails, "N_DefSPriceID", typeof(int), N_DefSPriceID);
                string message = null;

                bool B_LastPurchaseCost = Convert.ToBoolean(myFunctions.getIntVAL(myFunctions.ReturnSettings(this.N_FormID.ToString(), "LastPurchaseCost", "N_Value", "N_UserCategoryID", UserCategoryID.ToString(), myFunctions.getIntVAL(nCompanyID.ToString()), dLayer, connection)));

                object LastPurchaseCost = 0;
                bool B_NegStockEnabled = true;

                if (B_LastPurchaseCost)
                {
                    LastPurchaseCost = dLayer.ExecuteScalar("Select TOP(1) ISNULL(N_LPrice,0) from Inv_StockMaster Where N_ItemID=" + N_ItemID.ToString() + " and N_CompanyID=@nCompanyID and N_LocationID=@nLocationID and (X_Type='Purchase' or X_Type='Opening'or X_Type='TransferRecive') Order by N_StockID Desc", Params, connection);
                }
                myFunctions.AddNewColumnToDataTable(ItemDetails, "LastPurchaseCost", typeof(decimal), LastPurchaseCost);

                if (ItemClass != "1" && ItemClass != "4")
                {
                    B_NegStockEnabled = Convert.ToBoolean(myFunctions.getIntVAL(myFunctions.ReturnSettings("Inventory", "Negative Stock Enabled", "N_Value", myFunctions.getIntVAL(nCompanyID.ToString()), dLayer, connection)));

                    if (!B_NegStockEnabled)
                        if (stock <= 0)
                        {
                            //message="There is no enough stock qty to proceed... Do You Want To Continue";
                            message = "There is no enough stock";
                        }
                }

                object value = dLayer.ExecuteScalar("select N_DiscPerc from inv_CustomerDiscount where N_ProductID = '" + N_ItemID + "' and N_CustomerID = '" + nCustomerID + "' and N_CompanyID = '" + nCompanyID + "'", connection);
                myFunctions.AddNewColumnToDataTable(ItemDetails, "N_DiscPerc", typeof(decimal), value);


                ItemDetails.AcceptChanges();
                ItemDetails = _api.Format(ItemDetails);
                return Ok(_api.Success(ItemDetails, message));
            }
        }

        [HttpGet("details")]
        public ActionResult GetSalesInvoiceDetails(int nCompanyId, int nFnYearId, int nBranchId, string xInvoiceNo)
        {
            try
            {
                using (SqlConnection Con = new SqlConnection(connectionString))
                {
                    Con.Open();
                    DataSet dsSalesInvoice = new DataSet();
                    SortedList QueryParamsList = new SortedList();
                    QueryParamsList.Add("@nCompanyID", nCompanyId);
                    QueryParamsList.Add("@nFnYearID", nFnYearId);
                    QueryParamsList.Add("@nBranchId", nBranchId);
                    QueryParamsList.Add("@xTransType", "SALES");           
                    QueryParamsList.Add("@xInvoiceNo", xInvoiceNo);

                    SortedList mParamsList = new SortedList()
                    {
                        {"N_CompanyID",nCompanyId},
                        {"X_ReceiptNo",xInvoiceNo},
                        {"X_TransType","SALES"},
                        {"N_FnYearID",nFnYearId},
                        {"N_BranchId",nBranchId}
                    };
                    DataTable masterTable = dLayer.ExecuteDataTablePro("SP_InvSales_Disp", mParamsList, Con);
                    masterTable = _api.Format(masterTable, "Master");
                    if (masterTable.Rows.Count == 0) { return Ok(_api.Warning("No Data Found")); }
                    DataRow MasterRow = masterTable.Rows[0];
                    int nSalesID = myFunctions.getIntVAL(MasterRow["N_SalesID"].ToString());
                    QueryParamsList.Add("@nSalesID", nSalesID);
                    int N_TruckID = myFunctions.getIntVAL(MasterRow["N_TruckID"].ToString());
                    object objPlateNo = null;
                    if (N_TruckID > 0)
                    {
                        myFunctions.AddNewColumnToDataTable(masterTable, "X_PlateNo", typeof(string), "");
                        QueryParamsList.Add("@nTruckID", myFunctions.getIntVAL(masterTable.Rows[0]["N_TermsID"].ToString()));
                        objPlateNo = dLayer.ExecuteScalar("Select X_PlateNumber from Inv_TruckMaster where N_TruckID=@nTruckID and N_companyID=@nCompanyID", QueryParamsList, Con);
                        if (objPlateNo != null)
                            masterTable.Rows[0]["X_PlateNo"] = objPlateNo.ToString();
                    }

                    if (masterTable.Rows[0]["X_TandC"].ToString() == "")
                        masterTable.Rows[0]["X_TandC"] = myFunctions.ReturnSettings("64", "TermsandConditions", "X_Value", "N_UserCategoryID", "0", QueryParamsList, dLayer, Con);
                    int N_TermsID = myFunctions.getIntVAL(masterTable.Rows[0]["N_TermsID"].ToString());
                    if (N_TermsID > 0)
                    {
                        myFunctions.AddNewColumnToDataTable(masterTable, "X_Terms", typeof(string), "");
                        QueryParamsList.Add("@nTermsID", myFunctions.getIntVAL(masterTable.Rows[0]["N_TermsID"].ToString()));
                        masterTable.Rows[0]["X_Terms"] = myFunctions.ReturnValue("Inv_Terms", "X_Terms", "N_TermsID =@nTermsID and N_CompanyID =@nCompanyID", QueryParamsList, dLayer, Con);
                    }

                    object objPayment = dLayer.ExecuteScalar("SELECT dbo.Inv_PayReceipt.X_Type, dbo.Inv_PayReceiptDetails.N_InventoryId,Inv_PayReceiptDetails.N_Amount FROM  dbo.Inv_PayReceipt INNER JOIN dbo.Inv_PayReceiptDetails ON dbo.Inv_PayReceipt.N_PayReceiptId = dbo.Inv_PayReceiptDetails.N_PayReceiptId Where dbo.Inv_PayReceipt.X_Type='SR' and dbo.Inv_PayReceiptDetails.N_InventoryId=@nSalesID", QueryParamsList, Con);
                    if (objPayment != null)
                        myFunctions.AddNewColumnToDataTable(masterTable, "B_PaymentProcessed", typeof(Boolean), true);
                    else
                        myFunctions.AddNewColumnToDataTable(masterTable, "B_PaymentProcessed", typeof(Boolean), false);

                    //sales return count(draft and non draft)
                    object objSalesReturn = dLayer.ExecuteScalar("select X_DebitNoteNo from Inv_SalesReturnMaster where N_SalesId =@nSalesID and B_IsSaveDraft=0 and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID", QueryParamsList, Con);

                    myFunctions.AddNewColumnToDataTable(masterTable, "X_DebitNoteNo", typeof(string), objSalesReturn);

                    object objSalesReturnDraft = dLayer.ExecuteScalar("select Isnull(Count(N_DebitNoteId),0) from Inv_SalesReturnMaster where N_SalesId =@nSalesID and B_IsSaveDraft=1 and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID", QueryParamsList, Con);
                    if (objSalesReturnDraft != null)
                        myFunctions.AddNewColumnToDataTable(masterTable, "N_SalesReturnDraft", typeof(int), myFunctions.getIntVAL(objSalesReturnDraft.ToString()));
                        QueryParamsList.Add("@nCustomerID", masterTable.Rows[0]["N_CustomerID"].ToString());
                        object obPaymentMenthodid = dLayer.ExecuteScalar("Select N_TypeID From vw_InvCustomer Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_CustomerID=@nCustomerID and (N_BranchID=0 or N_BranchID=@nBranchID) and B_Inactive = 0", QueryParamsList, Con);
                    if (obPaymentMenthodid != null)
                    {
                        QueryParamsList.Add("@nPaymentMethodID", myFunctions.getIntVAL(obPaymentMenthodid.ToString()));
                        myFunctions.AddNewColumnToDataTable(masterTable, "N_PaymentMethodID", typeof(int), myFunctions.getIntVAL(obPaymentMenthodid.ToString()));
                        myFunctions.AddNewColumnToDataTable(masterTable, "X_PaymentMethod", typeof(string), myFunctions.ReturnValue("Inv_CustomerType", "X_TypeName", "N_TypeID =@nPaymentMethodID", QueryParamsList, dLayer, Con));
                    }

                    string qry = "";
                    bool B_DeliveryDispatch = myFunctions.CheckPermission(nCompanyId, 948, "Administrator", "X_UserCategory", dLayer, Con);
                    if (B_DeliveryDispatch)
                    {
                        DataTable dtDispatch = new DataTable();
                        qry = "Select * From Inv_DeliveryDispatch Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_InvoiceID=@nSalesID";
                        dtDispatch = dLayer.ExecuteDataTable(qry, QueryParamsList, Con);
                        dtDispatch = _api.Format(dtDispatch, "Delivery Dispatch");
                        dsSalesInvoice.Tables.Add(dtDispatch);
                    }

                    //invoice status
                    object objInvoiceRecievable = null, objBal = null;
                    double N_InvoiceRecievable = 0, N_BalanceAmt = 0;

                    objInvoiceRecievable = dLayer.ExecuteScalar("SELECT isnull((Inv_Sales.N_BillAmt-Inv_Sales.N_DiscountAmt + Inv_Sales.N_FreightAmt +isnull(Inv_Sales.N_OthTaxAmt,0)+ Inv_Sales.N_TaxAmt),0) as N_InvoiceAmount FROM Inv_Sales where Inv_Sales.N_SalesId=@nSalesID and Inv_Sales.N_CompanyID=@nCompanyID", QueryParamsList, Con);
                    objBal = dLayer.ExecuteScalar("SELECT SUM(N_BalanceAmount) from  vw_InvReceivables where N_SalesId=@nSalesID and X_Type= @xTransType and N_CompanyID=@nCompanyID", QueryParamsList, Con);
                    if (objInvoiceRecievable != null)
                        myFunctions.AddNewColumnToDataTable(masterTable, "N_InvoiceRecievable", typeof(double), N_InvoiceRecievable);
                    if (objBal != null)
                        myFunctions.AddNewColumnToDataTable(masterTable, "N_BalanceAmt", typeof(double), N_BalanceAmt);

                    DataTable dtPayment = new DataTable();
                    string qry1 = "SELECT  dbo.Inv_PayReceipt.X_VoucherNo FROM  dbo.Inv_PayReceipt INNER JOIN dbo.Inv_PayReceiptDetails ON dbo.Inv_PayReceipt.N_PayReceiptId = dbo.Inv_PayReceiptDetails.N_PayReceiptId Where dbo.Inv_PayReceipt.X_Type='SR' and dbo.Inv_PayReceiptDetails.N_InventoryId =@nSalesID";
                    dtPayment = dLayer.ExecuteDataTable(qry1, QueryParamsList, Con);
                    string InvoiceNos = "";
                    foreach (DataRow var in dtPayment.Rows)
                        InvoiceNos += var["X_VoucherNo"].ToString() + " , ";
                    myFunctions.AddNewColumnToDataTable(masterTable, "X_SalesReceiptNos", typeof(string), InvoiceNos);


                    dLayer.ExecuteDataTable( "Select * from vw_SalesAmount_Customer where N_SalesID=@nSalesID and N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and (N_BranchId=@nBranchId Or N_BranchId=0)",QueryParamsList,Con);

                    //Details
                    SortedList dParamList = new SortedList()
                    {
                        {"N_CompanyID",nCompanyId},
                        {"N_SalesID",masterTable.Rows[0]["n_SalesId"].ToString()},
                        {"D_Date",Convert.ToDateTime(masterTable.Rows[0]["d_SalesDate"].ToString())}
                    };
                    DataTable detailTable = dLayer.ExecuteDataTablePro("SP_InvSalesDtls_Disp", dParamList, Con);
                    detailTable = _api.Format(detailTable, "Details");
                    if (detailTable.Rows.Count == 0) { return Ok(_api.Warning("No Data Found")); }                   
                    dsSalesInvoice.Tables.Add(masterTable);
                    dsSalesInvoice.Tables.Add(detailTable);
                    return Ok(_api.Success(dsSalesInvoice));

                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }

        private SortedList StatusSetup(int nSalesID, int nFnYearID, SqlConnection connection)
        {

            object objInvoiceRecievable = null, objBal = null;
            double InvoiceRecievable = 0, BalanceAmt = 0;
            SortedList TxnStatus = new SortedList();
            TxnStatus.Add("Label", "");
            TxnStatus.Add("LabelColor", "");
            TxnStatus.Add("Alert", "");
            TxnStatus.Add("DeleteEnabled", true);
            TxnStatus.Add("SaveEnabled", true);
            TxnStatus.Add("ReceiptNumbers", "");
            int nCompanyID = myFunctions.GetCompanyID(User);

            objInvoiceRecievable = dLayer.ExecuteScalar("SELECT isnull((Inv_Sales.N_BillAmt-Inv_Sales.N_DiscountAmt + Inv_Sales.N_FreightAmt +isnull(Inv_Sales.N_OthTaxAmt,0)+ Inv_Sales.N_TaxAmt),0) as N_InvoiceAmount FROM Inv_Sales where Inv_Sales.N_SalesId=" + nSalesID + " and Inv_Sales.N_CompanyID=" + nCompanyID, connection);
            objBal = dLayer.ExecuteScalar("SELECT SUM(N_BalanceAmount) from  vw_InvReceivables where N_SalesId=" + nSalesID + " and X_Type='SALES' and N_CompanyID=" + nCompanyID, connection);


            object RetQty = dLayer.ExecuteScalar("select Isnull(Count(N_DebitNoteId),0) from Inv_SalesReturnMaster where N_SalesId =" + nSalesID + " and Isnull(B_IsSaveDraft,0) =0 and N_CompanyID=" + nCompanyID + " and N_FnYearID=" + nFnYearID, connection);
            object RetQtyDrft = dLayer.ExecuteScalar("select Isnull(Count(N_DebitNoteId),0) from Inv_SalesReturnMaster where N_SalesId =" + nSalesID + " and Isnull(B_IsSaveDraft,0)=1 and N_CompanyID=" + nCompanyID + " and N_FnYearID=" + nFnYearID, connection);


            if (objInvoiceRecievable != null)
                InvoiceRecievable = myFunctions.getVAL(objInvoiceRecievable.ToString());
            if (objBal != null)
                BalanceAmt = myFunctions.getVAL(objBal.ToString());

            if ((InvoiceRecievable == BalanceAmt) && (InvoiceRecievable > 0 && BalanceAmt > 0))
            {
                TxnStatus["Label"] = "NotPaid";
                TxnStatus["LabelColor"] = "Red";
                TxnStatus["Alert"] = "";
            }
            else
            {
                if (BalanceAmt == 0)
                {
                    //IF PAYMENT DONE
                    TxnStatus["Label"] = "Paid";
                    TxnStatus["LabelColor"] = "Green";
                    TxnStatus["Alert"] = "Customer Receipt is Processed for this Invoice.";


                    //IF PAYMENT DONE AND HAVING RETURN
                    if (RetQty != null && myFunctions.getIntVAL(RetQty.ToString()) > 0)
                    {
                        TxnStatus["SaveEnabled"] = false;
                        TxnStatus["DeleteEnabled"] = false;
                        TxnStatus["Alert"] = "Sales Return Processed for this invoice.";
                        TxnStatus["Label"] = "Paid(Return)";
                        TxnStatus["LabelColor"] = "Green";
                    }
                    else if (RetQtyDrft != null && myFunctions.getIntVAL(RetQtyDrft.ToString()) > 0)
                    {
                        TxnStatus["SaveEnabled"] = true;
                        TxnStatus["DeleteEnabled"] = false;
                        TxnStatus["Alert"] = "Sales Return Processed for this invoice.";
                        TxnStatus["Label"] = "Paid(Return)";
                        TxnStatus["LabelColor"] = "Green";
                    }
                }
                else
                {
                    //IF HAVING BALANCE AMOUNT
                    TxnStatus["Alert"] = "Customer Receipt is Processed for this Invoice.";
                    TxnStatus["Label"] = "ParPaid";
                    TxnStatus["LabelColor"] = "Green";

                    //IF HAVING BALANCE AMOUNT AND HAVING RETURN
                    if (RetQty != null && myFunctions.getIntVAL(RetQty.ToString()) > 0)
                    {
                        TxnStatus["SaveEnabled"] = false;
                        TxnStatus["DeleteEnabled"] = false;
                        TxnStatus["Alert"] = "Sales Return Processed for this invoice.";
                        TxnStatus["Label"] = "Partially Paid(Return)";
                        TxnStatus["LabelColor"] = "Green";
                    }
                    else if (RetQtyDrft != null && myFunctions.getIntVAL(RetQtyDrft.ToString()) > 0)
                    {
                        TxnStatus["SaveEnabled"] = true;
                        TxnStatus["DeleteEnabled"] = false;
                        TxnStatus["Alert"] = "Sales Return Processed for this invoice.";
                        TxnStatus["Label"] = "Partially Paid(Return)";
                        TxnStatus["LabelColor"] = "Green";
                    }
                }


                //PAYMENT NO DISPLAY IN TOP LABEL ON MOUSE HOVER
                DataTable Receipts = dLayer.ExecuteDataTable("SELECT  dbo.Inv_PayReceipt.X_VoucherNo FROM  dbo.Inv_PayReceipt INNER JOIN dbo.Inv_PayReceiptDetails ON dbo.Inv_PayReceipt.N_PayReceiptId = dbo.Inv_PayReceiptDetails.N_PayReceiptId Where dbo.Inv_PayReceipt.X_Type='SR' and dbo.Inv_PayReceiptDetails.N_InventoryId =" + nSalesID, connection);
                string InvoiceNos = "";
                foreach (DataRow var in Receipts.Rows)
                {
                    InvoiceNos += var["X_VoucherNo"].ToString() + " , ";
                }
                char[] trim = { ',', ' ' };
                if (InvoiceNos != "")
                    TxnStatus["ReceiptNumbers"] = InvoiceNos.ToString().TrimEnd(trim);

            }

            return TxnStatus;
        }
    }
}