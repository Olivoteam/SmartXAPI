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
    [Route("freeTextPurchase")]
    [ApiController]
    public class Inv_FreeTextPurchase : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly int FormID;
        private readonly IMyFunctions myFunctions;
        private readonly IMyAttachments myAttachments;

        public Inv_FreeTextPurchase(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf, IMyAttachments myAtt)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            myAttachments = myAtt;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 380;
        }
        private readonly string connectionString;
        [HttpGet("list")]
        public ActionResult FreeTextPurchaseList(int nFnYearID, int nBranchID, int nPage, int nSizeperpage, bool b_AllBranchData, string xSearchkey, string xSortBy)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable dt = new DataTable();
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    string sqlCommandCount = "", xCriteria = "";
                    string xTransType = "FTPURCHASE";
                    int Count = (nPage - 1) * nSizeperpage;
                    string sqlCommandText = "";
                    string Searchkey = "";
                    Params.Add("@p1", nCompanyID);
                    Params.Add("@p2", nFnYearID);
                    Params.Add("@p3", nBranchID);
                    Params.Add("@p4", xTransType);
                    bool CheckClosedYear = Convert.ToBoolean(dLayer.ExecuteScalar("Select B_YearEndProcess From Acc_FnYear Where N_CompanyID=@p1 and N_FnYearID=@p2 ", Params, connection));

                    if (!CheckClosedYear)
                    {
                        if (b_AllBranchData)
                            xCriteria = " N_FnYearID=@p2 and N_PurchaseType=0 and X_TransType=@p4 and B_YearEndProcess=0 and N_CompanyID=@p1 ";
                        else
                            xCriteria = " N_FnYearID=@p2 and N_PurchaseType=0 and X_TransType=@p4 and B_YearEndProcess=0 and N_BranchID=@p3 and N_CompanyID=@p1 ";
                    }
                    else
                    {
                        if (b_AllBranchData)
                            xCriteria = "and N_PurchaseType=0 and X_TransType=@p4 and N_FnYearID=@p2 and N_CompanyID=@p1";
                        else
                            xCriteria = "and N_PurchaseType=0 and X_TransType=@p4 and N_FnYearID=@p2 and N_BranchID=@p3 and N_CompanyID=@p1";
                    }

                    if (xSearchkey != null && xSearchkey.Trim() != "")
                        Searchkey = "and ( [Invoice No] like '%" + xSearchkey + "%' or [Invoice Date] like '%" + xSearchkey + "%' or Vendor like '%" + xSearchkey + "%' or InvoiceNetAmt like '%" + xSearchkey + "%' or x_Description like '%" + xSearchkey + "%' or n_InvDueDays like '%" + xSearchkey + "%' ) ";

                    if (xSortBy == null || xSortBy.Trim() == "")
                        xSortBy = " order by N_PurchaseID desc";
                    else
                        xSortBy = " order by " + xSortBy;
                    if (Count == 0)
                        sqlCommandText = "select top(" + nSizeperpage + ") [Invoice Date] as invoiceDate ,[Invoice No] as invoiceNo ,Vendor,InvoiceNetAmt,x_Description,n_InvDueDays from vw_InvPurchaseInvoiceNo_Search where " + xCriteria + Searchkey;
                    else
                        sqlCommandText = "select top(" + nSizeperpage + ") [Invoice Date] as invoiceDate,[Invoice No] as invoiceNo ,Vendor,InvoiceNetAmt,x_Description,n_InvDueDays from vw_InvPurchaseInvoiceNo_Search where " + xCriteria + Searchkey + "and N_PurchaseID not in (select top(" + Count + ") N_PurchaseID from vw_InvPurchaseInvoiceNo_Search where " + xCriteria + Searchkey + " ) ";
                    SortedList OutPut = new SortedList();

                    dt = dLayer.ExecuteDataTable(sqlCommandText + xSortBy, Params, connection);
                    sqlCommandCount = "select count(*) as N_Count  from vw_InvPurchaseInvoiceNo_Search where " + xCriteria + Searchkey;
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
                return BadRequest(_api.Error(User, e));
            }
        }
        [HttpGet("accounts")]
        public ActionResult AccountList(int nFnYearID, bool bShowAllAccount, bool isSales)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            string sqlCommandText = "";
            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnYearID);

            if (isSales)
            {
                sqlCommandText = "SELECT * FROM vw_AccMastLedger WHERE N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and B_Inactive=0 ";

            }
            else
            {
                if (bShowAllAccount)
                    sqlCommandText = "SELECT * FROM vw_AccMastLedger WHERE N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and B_Inactive=0 and   N_PostingBahavID=11";
                else
                    sqlCommandText = "SELECT * FROM vw_AccMastLedger WHERE N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and B_Inactive=0 and x_type ='E' and N_PostingBahavID=11";
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    object classID = dLayer.ExecuteScalar("select N_ClassID from Inv_itemClass where X_ClassName='Non Stock Item'", Params, connection);
                    object itemID = dLayer.ExecuteScalar("select N_ItemID From Inv_ItemMaster where N_CompanyID=" + nCompanyID + " and X_ItemCode=001", Params, connection);
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    dt = myFunctions.AddNewColumnToDataTable(dt, "N_ItemID", typeof(int), 0);
                    dt = myFunctions.AddNewColumnToDataTable(dt, "N_ClassID", typeof(int), 0);
                    if (itemID != null)
                        foreach (DataRow var in dt.Rows)
                        {

                            var["N_ItemID"] = myFunctions.getIntVAL(itemID.ToString());
                            var["N_ClassID"] = myFunctions.getIntVAL(classID.ToString());
                        }

                }
                dt.AcceptChanges();
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
                return Ok(_api.Error(User, e));
            }
        }
        [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {

                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    DataTable MasterTable;
                    DataTable DetailTable;
                    DataTable CostCenterTable;
                    string DocNo = "";
                    MasterTable = ds.Tables["master"];
                    DetailTable = ds.Tables["details"];
                    CostCenterTable = ds.Tables["CostCenterTable"];
                    DataRow MasterRow = MasterTable.Rows[0];
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    int nUserID = myFunctions.GetUserID(User);
                    int nFnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearId"].ToString());
                    int nPurchaseID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_PurchaseID"].ToString());
                    string X_InvoiceNo = MasterTable.Rows[0]["X_InvoiceNo"].ToString();
                    string xTransType = "FTPURCHASE";

                    DocNo = MasterRow["X_InvoiceNo"].ToString();
                    if (nPurchaseID > 0)
                    {

                        try
                        {
                            SortedList DelParam = new SortedList();
                            DelParam.Add("N_CompanyID", nCompanyID);
                            DelParam.Add("X_TransType", xTransType);
                            DelParam.Add("N_VoucherID", nPurchaseID);
                            dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_Accounts", DelParam, connection, transaction);
                            int dltRes = dLayer.DeleteData("Inv_CostCentreTransactions", "N_InventoryID", nPurchaseID, "N_CompanyID = " + nCompanyID + " and N_FnYearID=" + nFnYearID, connection, transaction);
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            return Ok(_api.Error(User, ex.Message));
                        }
                    }


                    if (DocNo == "@Auto")
                    {

                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", this.FormID);
                       // Params.Add("N_BranchID", MasterRow["n_BranchId"].ToString());

                        X_InvoiceNo = dLayer.GetAutoNumber("Inv_Purchase", "x_InvoiceNo", Params, connection, transaction);
                        if (X_InvoiceNo == "")
                        {
                            transaction.Rollback();
                            return Ok(_api.Error(User, "Unable to generate Invoice Number"));
                        }
                        MasterTable.Rows[0]["x_InvoiceNo"] = X_InvoiceNo;
                    }
                    nPurchaseID = dLayer.SaveData("Inv_Purchase", "N_PurchaseID", MasterTable, connection, transaction);

                    if (nPurchaseID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to save Purchase Invoice!"));
                    }
                    CostCenterTable = myFunctions.AddNewColumnToDataTable(CostCenterTable, "N_LedgerID", typeof(int), 0);
                    ////costcentre
                    DetailTable.Columns.Remove("X_ItemUnit");
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {

                        DetailTable.Rows[j]["N_PurchaseID"] = nPurchaseID;

                        int N_InvoiceDetailId = dLayer.SaveDataWithIndex("Inv_PurchaseDetails", "n_PurchaseDetailsID", "", "", j, DetailTable, connection, transaction);


                        if (N_InvoiceDetailId > 0)
                        {
                            for (int k = 0; k < CostCenterTable.Rows.Count; k++)
                            {
                                if (myFunctions.getIntVAL(CostCenterTable.Rows[k]["rowID"].ToString()) == j)
                                {
                                    CostCenterTable.Rows[k]["N_VoucherID"] = nPurchaseID;
                                    CostCenterTable.Rows[k]["N_VoucherDetailsID"] = N_InvoiceDetailId;
                                    CostCenterTable.Rows[k]["N_LedgerID"] = myFunctions.getVAL(DetailTable.Rows[j]["n_LedgerID"].ToString());
                                }
                            }
                        }


                    }
                    // if (CostCenterTable.Columns.Contains("rowID"))
                    //     CostCenterTable.Columns.Remove("rowID");
                    // if (CostCenterTable.Columns.Contains("percentage"))
                    //     CostCenterTable.Columns.Remove("percentage");

                    CostCenterTable.AcceptChanges();

                    DataTable costcenter = new DataTable();
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_CostCenterTransID", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_CompanyID", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_FnYearID", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_InventoryType", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_InventoryID", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_InventoryDetailsID", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_CostCentreID", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_Amount", typeof(double), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_LedgerID", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_BranchID", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "X_Narration", typeof(string), "");
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "X_Naration", typeof(string), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "D_Entrydate", typeof(DateTime), null);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_GridLineNo", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_EmpID", typeof(int), 0);
                    costcenter = myFunctions.AddNewColumnToDataTable(costcenter, "N_ProjectID", typeof(int), 0);

                    foreach (DataRow dRow in CostCenterTable.Rows)
                    {
                        DataRow row = costcenter.NewRow();
                        row["N_CostCenterTransID"] = dRow["N_VoucherSegmentID"];
                        row["N_CompanyID"] = dRow["N_CompanyID"];
                        row["N_FnYearID"] = dRow["N_FnYearID"];
                        row["N_InventoryType"] = 1;
                        row["N_InventoryID"] = dRow["N_VoucherID"];
                        row["N_CostCentreID"] = dRow["N_VoucherDetailsID"];
                        row["N_Amount"] = dRow["N_Amount"];
                        row["N_LedgerID"] = dRow["N_LedgerID"];
                        row["N_BranchID"] = dRow["N_BranchID"];
                        row["X_Narration"] = "";
                        row["X_Naration"] = dRow["X_Naration"];
                        row["D_Entrydate"] = dRow["D_Entrydate"];
                        row["N_GridLineNo"] = dRow["rowID"];
                        row["N_EmpID"] = myFunctions.getIntVAL(dRow["N_Segment_4"].ToString());
                        row["N_ProjectID"] = myFunctions.getIntVAL(dRow["N_Segment_3"].ToString());
                        costcenter.Rows.Add(row);
                    }

                    int N_SegmentId = dLayer.SaveData("Inv_CostCentreTransactions", "N_CostCenterTransID", "", "", costcenter, connection, transaction);


                    SortedList PostingParam = new SortedList();
                    PostingParam.Add("N_CompanyID", nCompanyID);
                    PostingParam.Add("X_InventoryMode", xTransType);
                    PostingParam.Add("N_InternalID", nPurchaseID);
                    PostingParam.Add("N_UserID", nUserID);
                    PostingParam.Add("X_SystemName", "WebRequest");
                    try
                    {
                        dLayer.ExecuteNonQueryPro("SP_Acc_InventoryPosting", PostingParam, connection, transaction);
                    }
                    catch (Exception ex)
                    {

                        return Ok(_api.Error(User, ex));
                    }
                    transaction.Commit();
                    return Ok(_api.Success("Successfully saved"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }
        }
        [HttpGet("listdetails")]
        public ActionResult GetPurchaseeDetails(int nCompanyId, int nFnYearId, string xInvoiceNO, string xTransType, bool showAllBranch, int nBranchId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataSet dt = new DataSet();
                    SortedList Params = new SortedList();
                    DataTable Master = new DataTable();
                    DataTable Details = new DataTable();
                    DataTable Acc_CostCentreTrans = new DataTable();
                    int N_PurchaseID = 0;
                    string X_MasterSql = "";
                    string X_DetailsSql = "";
                    if (showAllBranch)
                        X_MasterSql = "Select * from vw_Inv_FreePurchase_Disp  Where N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + " and X_TransType='" + xTransType + "' and X_InvoiceNo='" + xInvoiceNO + "' ";
                    else
                        X_MasterSql = "Select * from vw_Inv_FreePurchase_Disp Where N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + " and X_TransType='" + xTransType + "' and X_InvoiceNo='" + xInvoiceNO + "' and N_BranchId=" + nBranchId + "";
                    Master = dLayer.ExecuteDataTable(X_MasterSql, Params, connection);
                    if (Master.Rows.Count == 0) { return Ok(_api.Warning("No Data Found")); }
                    N_PurchaseID = myFunctions.getIntVAL(Master.Rows[0]["N_PurchaseID"].ToString());


                    object purID = dLayer.ExecuteScalar("select N_PurchaseID from Inv_Purchase where N_FreeTextReturnID =" + N_PurchaseID + " and N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + "", Params, connection);
                    if (purID != null)
                    {
                        Master = myFunctions.AddNewColumnToDataTable(Master, "IsReturnDone", typeof(bool), true);
                        // Master = myFunctions.AddNewColumnToDataTable(Master, "X_ReturnCode", typeof(string), RetQty.ToString());
                    }
                    else
                    {
                        Master = myFunctions.AddNewColumnToDataTable(Master, "IsReturnDone", typeof(bool), false);
                        //dtPurchaseInvoice = myFunctions.AddNewColumnToDataTable(dtPurchaseInvoice, "X_ReturnCode", typeof(string), "");
                    }

                    object purchaseAmount = dLayer.ExecuteScalar("Select N_InvoiceAmt from vw_Inv_FreeTextPurchase_Disp  Where N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + " and X_TransType='" + xTransType + "' and X_InvoiceNo='" + xInvoiceNO + "' ", Params, connection);
                    object returnAmout = dLayer.ExecuteScalar("Select Sum(N_InvoiceAmt)  from vw_Inv_FreeTextPurchase_Disp  Where N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + " and X_TransType='CREDIT NOTE' and N_FreeTextReturnID=" + N_PurchaseID + " ", Params, connection);

                    if (myFunctions.getVAL(purchaseAmount.ToString()) == myFunctions.getVAL(returnAmout.ToString()))
                    {

                        Master.Rows[0]["IsReturnDone"] = true;
                    }
                    else
                    {
                         Master.Rows[0]["IsReturnDone"] = false;
                    }
                    Master.AcceptChanges();




                    Master = _api.Format(Master, "Master");
                    if (showAllBranch)
                        X_DetailsSql = "Select Inv_PurchaseDetails.*,Acc_MastLedger.*,vw_InvPurchaseDetails.X_DisplayName, vw_InvPurchaseDetails.N_TaxPerc1, vw_InvPurchaseDetails.N_TaxID1,  vw_InvPurchaseDetails.N_TaxID2, vw_InvPurchaseDetails.N_TaxPerc2, vw_InvPurchaseDetails.X_DisplayName2,vw_InvPurchaseDetails.X_ActVendor from Inv_PurchaseDetails " +
                            " LEFT OUTER JOIN vw_InvPurchaseDetails ON Inv_PurchaseDetails.N_CompanyID = vw_InvPurchaseDetails.N_CompanyID AND   Inv_PurchaseDetails.N_PurchaseDetailsID = vw_InvPurchaseDetails.N_PurchaseDetailsID LEFT OUTER JOIN  Acc_MastLedger ON Inv_PurchaseDetails.N_LedgerID = Acc_MastLedger.N_LedgerID AND Inv_PurchaseDetails.N_CompanyID = Acc_MastLedger.N_CompanyID" +
                            " Where Inv_PurchaseDetails.N_CompanyID=" + nCompanyId + " and Inv_PurchaseDetails.N_PurchaseID=" + N_PurchaseID + " and Acc_MastLedger.N_FnYearID=" + nFnYearId + "";

                    else
                        X_DetailsSql = "Select Inv_PurchaseDetails.*,Acc_MastLedger.*,vw_InvPurchaseDetails.X_DisplayName, vw_InvPurchaseDetails.N_TaxPerc1, vw_InvPurchaseDetails.N_TaxID1,  vw_InvPurchaseDetails.N_TaxID2, vw_InvPurchaseDetails.N_TaxPerc2, vw_InvPurchaseDetails.X_DisplayName2,vw_InvPurchaseDetails.X_ActVendor from Inv_PurchaseDetails " +
                             " LEFT OUTER JOIN vw_InvPurchaseDetails ON Inv_PurchaseDetails.N_CompanyID = vw_InvPurchaseDetails.N_CompanyID AND   Inv_PurchaseDetails.N_PurchaseDetailsID = vw_InvPurchaseDetails.N_PurchaseDetailsID LEFT OUTER JOIN  Acc_MastLedger ON Inv_PurchaseDetails.N_LedgerID = Acc_MastLedger.N_LedgerID AND Inv_PurchaseDetails.N_CompanyID = Acc_MastLedger.N_CompanyID" +
                            " Where Inv_PurchaseDetails.N_CompanyID=" + nCompanyId + " and Inv_PurchaseDetails.N_PurchaseID=" + N_PurchaseID + " and ISNULL(Inv_PurchaseDetails.N_BranchId,0)=" + nBranchId + "  and Acc_MastLedger.N_FnYearID=" + nFnYearId + "";

                    Details = dLayer.ExecuteDataTable(X_DetailsSql, Params, connection);
                    Details = _api.Format(Details, "Details");


                    SortedList ProParams = new SortedList();
                    ProParams.Add("N_CompanyID", nCompanyId);
                    ProParams.Add("N_FnYearID", nFnYearId);
                    ProParams.Add("N_VoucherID", N_PurchaseID);
                    ProParams.Add("N_Flag", 1);
                    ProParams.Add("X_Type", "PURCHASE");

                    // Acc_CostCentreTrans = dLayer.ExecuteDataTablePro("SP_Acc_Voucher_Disp_CLOUD", ProParams, connection);

                    string CostcenterSql = "SELECT X_EmpCode, X_EmpName, N_ProjectID as N_Segment_3,N_EmpID as N_Segment_4, X_ProjectCode,X_ProjectName,N_EmpID,N_ProjectID,N_CompanyID,N_FnYearID, " +
                    " N_VoucherID, N_VoucherDetailsID, N_CostCentreID,X_CostCentreName,X_CostCentreCode,N_BranchID,X_BranchName,X_BranchCode , " +
                    " N_Amount, N_LedgerID, N_CostCenterTransID, N_GridLineNo,X_Naration,0 AS N_AssetID, '' As X_AssetCode, " +
                    " GETDATE() AS D_RepaymentDate, '' AS X_AssetName,'' AS X_PayCode,0 AS N_PayID,0 AS N_Inst,CAST(0 AS BIT) AS B_IsCategory,D_Entrydate " +
                    " FROM   vw_InvFreeTextPurchaseCostCentreDetails where N_InventoryID = " + N_PurchaseID + " And N_InventoryType=1 And N_FnYearID=" + nFnYearId +
                    " And N_CompanyID=" + nCompanyId + " Order By N_InventoryID,N_VoucherDetailsID ";

                    Acc_CostCentreTrans = dLayer.ExecuteDataTable(CostcenterSql, ProParams, connection);

                    Acc_CostCentreTrans = _api.Format(Acc_CostCentreTrans, "costCenterTrans");
                    dt.Tables.Add(Acc_CostCentreTrans);

                    dt.Tables.Add(Details);
                    dt.Tables.Add(Master);
                    return Ok(_api.Success(dt));

                }
            }


            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }
        [HttpDelete("delete")]
        public ActionResult DeleteData(int nPurchaseID, string X_TransType)
        {

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    var nUserID = myFunctions.GetUserID(User);
                    SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",nCompanyID},
                                {"X_TransType",X_TransType},
                                {"N_VoucherID",nPurchaseID},
                                {"N_UserID",nUserID},
                                 {"X_SystemName","WebRequest"}};
                    int Results = dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_Accounts", DeleteParams, connection, transaction);

                    if (Results <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to delete Purchase"));
                    }
                    transaction.Commit();
                    return Ok(_api.Success(" Purchase deleted"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }
        }


    }
}