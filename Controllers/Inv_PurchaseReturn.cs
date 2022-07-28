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
    [Route("purchasereturn")]
    [ApiController]
    public class Inv_PurchaseReturn : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;


        public Inv_PurchaseReturn(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");

        }


        [HttpGet("list")]
        public ActionResult GetPurchaseReturnList(int? nCompanyId, int nFnYearId, bool bAllBranchData, int nBranchID, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable dt = new DataTable();
                    SortedList Params = new SortedList();

                    int Count = (nPage - 1) * nSizeperpage;
                    string sqlCommandText = "";
                    string sqlCommandCount = "";
                    string Searchkey = "";
                     int nCompanyID = myFunctions.GetCompanyID(User);
                       int N_decimalPlace = 2;
                    N_decimalPlace = myFunctions.getIntVAL(myFunctions.ReturnSettings("Purchase", "Decimal_Place", "N_Value", nCompanyID, dLayer, connection));
                    N_decimalPlace = N_decimalPlace == 0 ? 2 : N_decimalPlace;
                    int nUserID = myFunctions.GetUserID(User);
                    // string UserPattern = myFunctions.GetUserPattern(User);
                    // string Pattern = "";
                    
                    //  if (UserPattern != "")
                    //  {
                    // Pattern = " and Left(X_Pattern,Len(@UserPattern))=@UserPattern ";
                    // Params.Add("@UserPattern",UserPattern);

                    //    }
                    //  else
                    //    {
                    // object HierarchyCount = dLayer.ExecuteScalar("select count(N_HierarchyID) from Sec_UserHierarchy where N_CompanyID="+nCompanyId,Params,connection);

                    // if(myFunctions.getIntVAL(HierarchyCount.ToString())>0)
                    // Pattern = " and N_CreatedUser=" + nUserID;
                    //     }

                    bool CheckClosedYear = Convert.ToBoolean(dLayer.ExecuteScalar("Select B_YearEndProcess From Acc_FnYear Where N_CompanyID=" + nCompanyId + " and N_FnYearID = " + nFnYearId, Params, connection));
                    if (xSearchkey != null && xSearchkey.Trim() != "")
                        Searchkey = "and (X_CreditNoteNo like '%" + xSearchkey + "%' or X_VendorName like '%" + xSearchkey + "%' or X_InvoiceNo like '%" + xSearchkey + "%')";

                    if (xSortBy == null || xSortBy.Trim() == "")
                        xSortBy = " order by N_CreditNoteId desc";
                    else
                    {
                        switch (xSortBy.Split(" ")[0])
                        {
                            case "x_CreditNoteNo":
                                xSortBy = "N_CreditNoteId " + xSortBy.Split(" ")[1];
                                break;
                            default: break;
                        }
                        xSortBy = " order by " + xSortBy;
                    }
                    if (CheckClosedYear == false)
                    {
                        if (bAllBranchData == true)
                        {
                            Searchkey = Searchkey + " and  N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + " ";
                        }
                        else
                        {
                            Searchkey = Searchkey + " and  N_CompanyID=" + nCompanyId + " and N_BranchID=" + nBranchID + " and N_FnYearID=" + nFnYearId + " and B_YearEndProcess =0";
                        }
                    }
                    else
                    {
                        if (bAllBranchData == true)
                        {
                            Searchkey = Searchkey + " and  N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + " ";
                        }
                        else
                        {
                            Searchkey = Searchkey + " and  N_CompanyID=" + nCompanyId + " and N_BranchID=" + nBranchID + " and N_FnYearID=" + nFnYearId + " and B_YearEndProcess =0";
                        }
                    }



                    if (Count == 0)
                        sqlCommandText = "select top(" + nSizeperpage + ") * from vw_InvCreditNo_Search_Cloud where N_CompanyID=@p1 and N_FnYearID=@p2 "   + Searchkey + " " + xSortBy;
                    else
                        sqlCommandText = "select top(" + nSizeperpage + ") * from vw_InvCreditNo_Search_Cloud where N_CompanyID=@p1 and N_FnYearID=@p2 "   + Searchkey + " and N_CreditNoteID not in (select top(" + Count + ") N_CreditNoteID from vw_InvCreditNo_Search_Cloud where N_CompanyID=@p1 and N_FnYearID=@p2 " + xSortBy + " ) " + xSortBy;

                    Params.Add("@p1", nCompanyId);
                    Params.Add("@p2", nFnYearId);
                    SortedList OutPut = new SortedList();


                    // connection.Open();
                    //  dt=dLayer.ExecuteDataTable(sqlCommandText,Params, connection);
                    //  sqlCommandCount = "select count(*) as N_Count  from vw_InvCreditNo_Search_Cloud where N_CompanyID=@p1 and N_FnYearID=@p2";
                    // object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    // OutPut.Add("Details",_api.Format(dt));
                    // OutPut.Add("TotalCount",TotalCount);


                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(*) as N_Count,sum(Cast(REPLACE(n_TotalReturnAmount,',','') as Numeric(10,"+N_decimalPlace+")) ) as TotalAmount from vw_InvCreditNo_Search_Cloud where N_CompanyID=@p1 and N_FnYearID=@p2 " + Searchkey + "";
                    DataTable Summary = dLayer.ExecuteDataTable(sqlCommandCount, Params, connection);
                    string TotalCount = "0";
                    string TotalSum = "0";
                    if (Summary.Rows.Count > 0)
                    {
                        DataRow drow = Summary.Rows[0];
                        TotalCount = drow["N_Count"].ToString();
                        TotalSum = drow["TotalAmount"].ToString();
                    }
                    OutPut.Add("Details", _api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                    OutPut.Add("TotalSum", TotalSum);

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
                return Ok(_api.Error(User, e));
            }
        }


        [HttpGet("listInvoice")]
        public ActionResult GetPurchaseInvoiceList(int nFnYearId, int nPage, int nSizeperpage)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);

            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_InvCreditNo_Search_Cloud where N_CompanyID=@p1 and N_FnYearID=@p2";
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_InvCreditNo_Search_Cloud where N_CompanyID=@p1 and N_FnYearID=@p2 and N_CreditNoteID not in (select top(" + Count + ") N_CreditNoteID from vw_InvCreditNo_Search_Cloud where N_CompanyID=@p1 and N_FnYearID=@p2)";

            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(*) as N_Count  from vw_InvCreditNo_Search_Cloud where N_CompanyID=@p1 and N_FnYearID=@p2";
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
                return Ok(_api.Error(User, e));
            }
        }

        [HttpGet("listDetails")]
        public ActionResult GetPurchaseReturnDetails(int nCompanyId, string xCreditNoteNo, string xInvoiceNo, int nFnYearId, bool bAllBranchData, int nBranchID)
        {

            DataSet dt = new DataSet();
            SortedList Params = new SortedList();
            SortedList NewParams = new SortedList();
             NewParams.Add("@p1", nCompanyId);
            DataTable MasterTable = new DataTable();
            DataTable DetailTable = new DataTable();
            DataTable DataTable = new DataTable();
            if (xCreditNoteNo == null) xCreditNoteNo = "";
            if (xInvoiceNo == null) xInvoiceNo = "0";
            string Mastersql = "";
           

            if (bAllBranchData == true)
            {
                if (xInvoiceNo == "0")
                    Mastersql = "SP_Inv_PurchaseReturn_Display @p1, 0, @p3,@p2,'PURCHASE',0";
                else
                {
                    Mastersql = "SP_Inv_PurchaseReturn_Display @p1, 1, @p4,@p2,'PURCHASE',0";
                    Params.Add("@p4", xInvoiceNo);
                }
            }
            else
            {
                if (xInvoiceNo == "0")
                {
                    Mastersql = "SP_Inv_PurchaseReturn_Display @p1, 0, @p3,@p2,'PURCHASE',@p5";
                    Params.Add("@p5", nBranchID);
                }
                else
                {
                    Mastersql = "SP_Inv_PurchaseReturn_Display @p1, 1, @p4,@p2,'PURCHASE',@p5";
                    Params.Add("@p5", nBranchID);
                    Params.Add("@p4", xInvoiceNo);
                }
            }

            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);
            Params.Add("@p3", xCreditNoteNo);

            try
            {

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    if (xCreditNoteNo != "")
                    {
                     object purchaseID=dLayer.ExecuteScalar("select N_PurchaseID from Inv_PurchaseReturnMaster where N_CompanyID="+nCompanyId+" and N_FnYearID ="+nFnYearId+" and X_CreditNoteNo='"+xCreditNoteNo+"'",NewParams,connection);
                     if((purchaseID==null || purchaseID =="")|| myFunctions.getIntVAL(purchaseID.ToString())==0)
                     {
                         if (bAllBranchData == true)
                         {
                           if (xInvoiceNo == "0")
                             Mastersql = "[SP_Inv_PurchaseReturnDirect_Display] @p1, 0, @p3,@p2,'PURCHASE',0";
                        
                        }
                        else
                         {
                           if (xInvoiceNo == "0")
                             {
                              Mastersql = "[SP_Inv_PurchaseReturnDirect_Display] @p1, 0, @p3,@p2,'PURCHASE',@p5";

                              }
                       }
                     }
                    }

                    MasterTable = dLayer.ExecuteDataTable(Mastersql, Params, connection);

                    MasterTable = _api.Format(MasterTable, "Master");
                    dt.Tables.Add(MasterTable);

                    //PurchaseOrder Details
                    string DetailSql = "";
                    if (xCreditNoteNo != "")
                    {
                            int N_PurchaseID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_PurchaseID"].ToString());
                            if(N_PurchaseID>0)
                        {
                                if (bAllBranchData == true)
                        {
                            DetailSql = "Select vw_InvPurchaseReturnEdit_Disp.*,dbo.SP_LocationStock(vw_InvPurchaseReturnEdit_Disp.N_ItemID,vw_InvPurchaseReturnEdit_Disp.N_LocationID) As N_Stock from vw_InvPurchaseReturnEdit_Disp Where N_CompanyID=@p1 and X_CreditNoteNo=@p3 and N_FnYearID =@p2 and N_RetQty<>0";
                        }
                        else
                        {
                            DetailSql = "Select vw_InvPurchaseReturnEdit_Disp.*,dbo.SP_LocationStock(vw_InvPurchaseReturnEdit_Disp.N_ItemID,vw_InvPurchaseReturnEdit_Disp.N_LocationID) As N_Stock from vw_InvPurchaseReturnEdit_Disp Where N_CompanyID=@p1 and X_CreditNoteNo=@p3 and N_FnYearID =@p2 and N_RetQty<>0 and N_BranchId=@p5";
                        }

                        }
                        else
                        {
                        if (bAllBranchData == true)
                        {
                            DetailSql = "Select Vw_invpurchasereturnDirect.*,dbo.SP_LocationStock(Vw_invpurchasereturnDirect.N_ItemID,Vw_invpurchasereturnDirect.N_LocationID) As N_Stock from Vw_invpurchasereturnDirect Where N_CompanyID=@p1 and X_CreditNoteNo=@p3 and N_FnYearID =@p2 and N_RetQty<>0";
                        }
                        else
                        {
                            DetailSql = "Select Vw_invpurchasereturnDirect.*,dbo.SP_LocationStock(Vw_invpurchasereturnDirect.N_ItemID,Vw_invpurchasereturnDirect.N_LocationID) As N_Stock from Vw_invpurchasereturnDirect Where N_CompanyID=@p1 and X_CreditNoteNo=@p3 and N_FnYearID =@p2 and N_RetQty<>0 and N_BranchId=@p5";
                        }
                        }
                        DetailTable = dLayer.ExecuteDataTable(DetailSql, Params, connection);


                    }
                    else
                    {
                        if (bAllBranchData == true)
                            DetailSql = "Select vw_InvPurchaseDetails_Display.*,dbo.SP_LocationStock(vw_InvPurchaseDetails_Display.N_ItemID,vw_InvPurchaseDetails_Display.N_LocationID) As N_Stock  from vw_InvPurchaseDetails_Display Where N_CompanyID=@p1 and X_InvoiceNo=@p4";
                        else
                            DetailSql = "Select vw_InvPurchaseDetails_Display.*,dbo.SP_LocationStock(vw_InvPurchaseDetails_Display.N_ItemID,vw_InvPurchaseDetails_Display.N_LocationID) As N_Stock  from vw_InvPurchaseDetails_Display Where N_CompanyID=@p1 and X_InvoiceNo=@p4 and N_BranchID=@p5";

                        DetailTable = dLayer.ExecuteDataTable(DetailSql, Params, connection);
                        if (DetailTable.Columns.Contains("RetQty"))
                        {
                            foreach (DataRow var1 in DetailTable.Rows)
                            {
                                if (var1["RetQty"] != null && var1["RetQty"].ToString() != "")
                                {
                                    var1["n_PQty"] = (myFunctions.getIntVAL(var1["N_PQty"].ToString()) - myFunctions.getIntVAL(var1["RetQty"].ToString())).ToString();
                                    var1["RetQty"] = 0.00;
                                    MasterTable.Rows[0]["N_CreditNoteId"] = 0;
                                    MasterTable.Rows[0]["X_CreditNoteNo"] = "@Auto";
                                }

                            }
                        }
                        DetailTable.AcceptChanges();
                    }

                    DetailTable = _api.Format(DetailTable, "Details");
                    dt.Tables.Add(DetailTable);
                }
                return Ok(_api.Success(dt));
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
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
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    string ReturnNo = "";
                    int N_CreditNoteID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_CreditNoteId"].ToString());
                    int N_UserID = myFunctions.GetUserID(User);
                    double N_TotalReceived = myFunctions.getVAL(MasterTable.Rows[0]["n_TotalReceived"].ToString());
                    MasterTable.Rows[0]["n_TotalReceived"] = N_TotalReceived;
                    double N_TotalReceivedF = myFunctions.getVAL(MasterTable.Rows[0]["n_TotalReceivedF"].ToString());
                    MasterTable.Rows[0]["n_TotalReceivedF"] = N_TotalReceivedF;
                    var values = MasterTable.Rows[0]["X_CreditNoteNo"].ToString();

                     if (!myFunctions.CheckActiveYearTransaction(myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyId"].ToString()), myFunctions.getIntVAL(MasterTable.Rows[0]["n_FnYearId"].ToString()), Convert.ToDateTime(MasterTable.Rows[0]["D_RetDate"].ToString()), dLayer, connection, transaction))
                    {
                        object DiffFnYearID = dLayer.ExecuteScalar("select N_FnYearID from Acc_FnYear where N_CompanyID=" + myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyId"].ToString()) + " and convert(date ,'" + MasterTable.Rows[0]["D_RetDate"].ToString() + "') between D_Start and D_End", Params, connection, transaction);
                        if (DiffFnYearID != null)
                        {
                            MasterTable.Rows[0]["n_FnYearID"] = DiffFnYearID.ToString();
                            //nFnYearID = myFunctions.getIntVAL(DiffFnYearID.ToString());
                        }
                        else
                        {
                            transaction.Rollback();
                            return Ok(_api.Error(User, "Transaction date must be in the active Financial Year."));
                        }
                    }

                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", MasterTable.Rows[0]["n_CompanyId"].ToString());
                        Params.Add("N_YearID", MasterTable.Rows[0]["n_FnYearId"].ToString());
                        Params.Add("N_FormID", 80);
                        Params.Add("N_BranchID", MasterTable.Rows[0]["n_BranchId"].ToString());
                        ReturnNo = dLayer.GetAutoNumber("Inv_PurchaseReturnMaster", "X_CreditNoteNo", Params, connection, transaction);
                        if (ReturnNo == "") { transaction.Rollback(); return Ok(_api.Warning("Unable to generate Quotation Number")); }
                        MasterTable.Rows[0]["X_CreditNoteNo"] = ReturnNo;
                    }

                    if (N_CreditNoteID > 0)
                    {
                        SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",MasterTable.Rows[0]["n_CompanyId"].ToString()},
                                {"X_TransType","PURCHASE RETURN"},
                                {"N_VoucherID",N_CreditNoteID}};
                        try
                        {
                            dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_PurchaseAccounts", DeleteParams, connection, transaction);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Ok(_api.Error(User, ex));
                        }
                    }

                    N_CreditNoteID = dLayer.SaveData("Inv_PurchaseReturnMaster", "N_CreditNoteID", MasterTable, connection, transaction);
                    if (N_CreditNoteID <= 0)
                    {
                        transaction.Rollback();
                    }


                    if(!DetailTable.Columns.Contains("N_QtyDisplay"))
                    DetailTable= myFunctions.AddNewColumnToDataTable(DetailTable,"N_QtyDisplay",typeof(double),0);
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {
                        DetailTable.Rows[j]["N_CreditNoteID"] = N_CreditNoteID;
                        // DetailTable.Rows[j]["n_RetQty"] = (myFunctions.getVAL(DetailTable.Rows[j]["n_RetQty"].ToString())) * (myFunctions.getVAL(DetailTable.Rows[j]["N_UnitQty"].ToString()));
                        // DetailTable.Rows[j]["N_QtyDisplay"] = DetailTable.Rows[j]["n_RetQty"];
                    }
                    if(DetailTable.Columns.Contains("N_UnitQty"))
                    DetailTable.Columns.Remove("N_UnitQty");

                    int N_QuotationDetailId = dLayer.SaveData("Inv_PurchaseReturnDetails", "n_CreditNoteDetailsID", DetailTable, connection, transaction);


                    SortedList InsParams = new SortedList(){
                                {"N_CompanyID",MasterTable.Rows[0]["n_CompanyId"].ToString()},
                                {"N_CreditNoteID",N_CreditNoteID}};
                    dLayer.ExecuteNonQueryPro("[SP_PurchaseReturn_Ins]", InsParams, connection, transaction);

                    SortedList PostParams = new SortedList(){
                                {"N_CompanyID",MasterTable.Rows[0]["n_CompanyId"].ToString()},
                                {"X_InventoryMode","PURCHASE RETURN"},
                                {"N_InternalID",N_CreditNoteID},
                                {"N_UserID",N_UserID}};
                    dLayer.ExecuteNonQueryPro("SP_Acc_Inventory_Purchase_Posting", PostParams, connection, transaction);

                    SortedList Result = new SortedList();
                    Result.Add("n_PurchaseReturnID", N_CreditNoteID);
                    Result.Add("x_PurchaseReturnNo", ReturnNo);
                    transaction.Commit();
                    return Ok(_api.Success(Result, "Purchase Return Saved"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }
        }

        //Delete....
        [HttpDelete("delete")]
        public ActionResult DeleteData(int? nCreditNoteId, int? nCompanyId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    object objPaymentProcessed = dLayer.ExecuteScalar("Select Isnull(N_PayReceiptId,0) from Inv_PayReceiptDetails where N_InventoryId=" + nCreditNoteId + " and X_TransType='PURCHASE RETURN'", connection, transaction);
                    if (objPaymentProcessed == null)
                        objPaymentProcessed = 0;

                    SortedList deleteParams = new SortedList()
                            {
                                {"N_CompanyID",nCompanyId},
                                {"X_TransType","PURCHASE RETURN"},
                                {"N_VoucherID",nCreditNoteId}
                            };
                    if (myFunctions.getIntVAL(objPaymentProcessed.ToString()) == 0)
                        dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_PurchaseAccounts", deleteParams, connection, transaction);
                    else
                        return Ok(_api.Error(User, "Payment processed! Unable to delete"));
                    transaction.Commit();
                }

                return Ok(_api.Success("Purchase Return Deleted"));

            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }


        }
        [HttpGet("dummy")]
        public ActionResult GetPurchaseReturnDummy(int? nPurchaseReturnId)
        {
            try
            {
                using (SqlConnection Con = new SqlConnection(connectionString))
                {
                    Con.Open();
                    string sqlCommandText = "select * from Inv_PurchaseReturnMaster where N_CreditNoteId=@p1";
                    SortedList mParamList = new SortedList() { { "@p1", nPurchaseReturnId } };
                    DataTable masterTable = dLayer.ExecuteDataTable(sqlCommandText, mParamList, Con);
                    masterTable = _api.Format(masterTable, "master");

                    string sqlCommandText2 = "select * from Inv_PurchaseReturnDetails where N_CreditNoteId=@p1";
                    SortedList dParamList = new SortedList() { { "@p1", nPurchaseReturnId } };
                    DataTable detailTable = dLayer.ExecuteDataTable(sqlCommandText2, dParamList, Con);
                    detailTable = _api.Format(detailTable, "details");

                    // string sqlCommandText3 = "select * from Inv_SaleAmountDetails where N_SalesId=@p1";
                    // DataTable dtAmountDetails = dLayer.ExecuteDataTable(sqlCommandText3, dParamList, Con);
                    // dtAmountDetails = _api.Format(dtAmountDetails, "saleamountdetails");

                    if (detailTable.Rows.Count == 0) { return Ok(new { }); }
                    DataSet dataSet = new DataSet();
                    dataSet.Tables.Add(masterTable);
                    dataSet.Tables.Add(detailTable);
                    //dataSet.Tables.Add(dtAmountDetails);

                    return Ok(dataSet);

                }
            }
            catch (Exception e)
            {
                return StatusCode(403, _api.Error(User, e));
            }
        }



    }
}