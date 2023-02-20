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
    [Route("freeTextSales")]
    [ApiController]
    public class Inv_FreeTextSales : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly int FormID;
        private readonly IMyFunctions myFunctions;
        private readonly IMyAttachments myAttachments;

        public Inv_FreeTextSales(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf, IMyAttachments myAtt)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            myAttachments = myAtt;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 372;
        }
        private readonly string connectionString;
        [HttpGet("list")]
        public ActionResult FreeTextSalesList(int nFnYearID, int nBranchID, int nPage, int nSizeperpage, bool b_AllBranchData, string xSearchkey, string xSortBy)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable dt = new DataTable();
                    SortedList Params = new SortedList();
                    //int nCompanyID = myFunctions.GetCompanyID(User);
                    int nCompanyId = myFunctions.GetCompanyID(User);
                    string sqlCommandCount = "", xCriteria = "";
                    string xTransType = "FTSALES";
                    int Count = (nPage - 1) * nSizeperpage;
                    string sqlCommandText = "";
                    string Searchkey = "";
                    Params.Add("@p1", nCompanyId);
                    Params.Add("@p2", nFnYearID);
                    Params.Add("@p3", nBranchID);
                    Params.Add("@p4", xTransType);
                    bool CheckClosedYear = Convert.ToBoolean(dLayer.ExecuteScalar("Select B_YearEndProcess From Acc_FnYear Where N_CompanyID=@p1 and N_FnYearID=@p2 ", Params, connection));

                    if (!CheckClosedYear)
                    {
                        if (b_AllBranchData)
                            xCriteria = "N_SalesType=0 and X_TransType=@p4 and B_YearEndProcess=0 and N_CompanyId=@p1 and N_FnYearID=@p2 ";
                        else
                            xCriteria = "N_SalesType=0 and X_TransType=@p4 and B_YearEndProcess=0 and N_BranchId=@p3 and N_CompanyId=@p1 and N_FnYearID=@p2 ";
                    }
                    else
                    {
                        if (b_AllBranchData)
                            xCriteria = "N_SalesType=0 and X_TransType=@p4 and N_FnYearID=@p2 and N_CompanyId=@p1 ";
                        else
                            xCriteria = "N_SalesType=0 and X_TransType=@p4 and N_FnYearID=@p2 and N_BranchId=@p3 and N_CompanyId=@p1 ";
                    }

                    if (xSearchkey != null && xSearchkey.Trim() != "")
                        Searchkey = " and ( [Invoice No] like '%" + xSearchkey + "%' or X_BillAmt like '%" + xSearchkey + "%' or [Customer] like '%" + xSearchkey + "%' or n_InvDueDays like '%" + xSearchkey + "%' ) ";

                    if (xSortBy == null || xSortBy.Trim() == "")
                        xSortBy = " order by N_SalesId desc";
                    else
                        xSortBy = " order by " + xSortBy;
                    if (Count == 0)
                        sqlCommandText = "select top(" + nSizeperpage + ") [Invoice Date] as invoiceDate,[Customer] as X_Customer,[Invoice No] as invoiceNo,X_BillAmt,n_InvDueDays,X_CustomerName_Ar from vw_InvSalesInvoiceNo_Search_cloud where " + xCriteria + Searchkey+ xSortBy;
                    else
                        sqlCommandText = "select top(" + nSizeperpage + ") [Invoice Date] as invoiceDate,[Customer] as X_Customer,[Invoice No] as invoiceNo,X_BillAmt,n_InvDueDays,X_CustomerName_Ar from vw_InvSalesInvoiceNo_Search_cloud where " + xCriteria + Searchkey + "and N_SalesId not in (select top(" + Count + ") N_SalesId from vw_InvSalesInvoiceNo_Search_cloud where " + xCriteria + Searchkey + xSortBy + " ) " + xSortBy;

                    SortedList OutPut = new SortedList();

                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(1) as N_Count  from vw_InvSalesInvoiceNo_Search_cloud where " + xCriteria + Searchkey;
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
                    DataTable dtsaleamountdetails; ;
                    dtsaleamountdetails = ds.Tables["saleamountdetails"];
                    DataRow MasterRow = MasterTable.Rows[0];
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    SortedList AmountParams = new SortedList();
                    AmountParams.Add("N_CompanyID", nCompanyID);
                    int nUserID = myFunctions.GetUserID(User);
                    int nFnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearId"].ToString());
                    int nSalesID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_SalesID"].ToString());
                    int N_CustomerID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_CustomerID"].ToString());
                    string X_ReceiptNo = MasterTable.Rows[0]["X_ReceiptNo"].ToString();
                    double N_BillAmt = myFunctions.getVAL(MasterTable.Rows[0]["N_BillAmt"].ToString());
                    string xTransType = "FTSALES";
                    DocNo = MasterRow["X_ReceiptNo"].ToString();
                    DataTable Attachment = ds.Tables["attachments"];
                  
                    

                     if (!myFunctions.CheckActiveYearTransaction(nCompanyID, nFnYearID, Convert.ToDateTime(MasterTable.Rows[0]["D_SalesDate"].ToString()), dLayer, connection, transaction))
                    {
                        object DiffFnYearID = dLayer.ExecuteScalar("select N_FnYearID from Acc_FnYear where N_CompanyID=" + nCompanyID + " and convert(date ,'" + MasterTable.Rows[0]["D_SalesDate"].ToString() + "') between D_Start and D_End", Params, connection, transaction);
                        if (DiffFnYearID != null)
                        {
                            MasterTable.Rows[0]["n_FnYearID"] = DiffFnYearID.ToString();
                            nFnYearID = myFunctions.getIntVAL(DiffFnYearID.ToString());
                        }
                        else
                        {
                            transaction.Rollback();
                            return Ok(_api.Error(User, "Transaction date must be in the active Financial Year."));
                        }
                    }


                    if (nSalesID > 0)
                    {
                        try
                        {
                            SortedList DelParam = new SortedList();
                            DelParam.Add("N_CompanyID", nCompanyID);
                            DelParam.Add("X_TransType", xTransType);
                            DelParam.Add("N_VoucherID", nSalesID);
                            dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_Accounts", DelParam, connection, transaction);
                            int dltRes = dLayer.DeleteData("Inv_CostCentreTransactions", "N_InventoryID", nSalesID, " N_CompanyID = " + nCompanyID + " and N_FnYearID=" + nFnYearID, connection, transaction);
                    

                       
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
                        //Params.Add("N_BranchID", MasterRow["n_BranchId"].ToString());

                        X_ReceiptNo = dLayer.GetAutoNumber("Inv_Sales", "X_ReceiptNo", Params, connection, transaction);
                   
                        if (X_ReceiptNo == "")
                        {
                            transaction.Rollback();
                            return Ok(_api.Error(User, "Unable to generate Invoice Number"));
                        }
                        MasterTable.Rows[0]["X_ReceiptNo"] = X_ReceiptNo;
                    }

                     object DupCount = dLayer.ExecuteScalar("Select COUNT(X_ReceiptNo) from Inv_Sales where X_ReceiptNo ='" + DocNo + "' and N_CompanyID=" + nCompanyID + "and N_FnYearID=" + nFnYearID , Params, connection, transaction);
                       
                       if (myFunctions.getVAL(DupCount.ToString()) >= 1)
                       {
                        transaction.Rollback();   
                        return Ok(_api.Error(User, "Invoice Number Already Exist"));
                       }

                    nSalesID = dLayer.SaveData("Inv_Sales", "N_SalesID", MasterTable, connection, transaction);

                    if (nSalesID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to save free text sales!"));
                    }

                    dtsaleamountdetails.Rows[0]["N_SalesId"] = nSalesID;
                    double N_SChrgAmt = 0;
                    double N_SChrgAmtMax = 0;
                    object N_ServiceCharge = dLayer.ExecuteScalar("Select ISNULL(N_ServiceCharge , 0) from Inv_Customer where N_CustomerID=" + N_CustomerID + " and N_CompanyID=" + nCompanyID + "and N_FnYearID=" + nFnYearID, AmountParams, connection, transaction);
                    object N_ServiceChargeMax = dLayer.ExecuteScalar("Select ISNULL(N_ServiceChargeLimit , 0) from Inv_Customer where N_CustomerID=" + N_CustomerID + " and N_CompanyID=" + nCompanyID + "and N_FnYearID=" + nFnYearID, AmountParams, connection, transaction);
                    object N_TaxID = dLayer.ExecuteScalar("Select ISNULL(N_TaxCategoryID , 0) from Inv_Customer where N_CustomerID=" + N_CustomerID + " and N_CompanyID=" + nCompanyID + "and N_FnYearID=" + nFnYearID, AmountParams, connection, transaction);
                    if (myFunctions.getVAL(N_ServiceCharge.ToString()) > 0)
                    {
                        N_SChrgAmt = (N_BillAmt * myFunctions.getVAL((N_ServiceCharge.ToString())) / 100);
                        N_SChrgAmtMax = myFunctions.getVAL(N_ServiceChargeMax.ToString());
                        if (N_SChrgAmtMax > 0)
                        {
                            if (N_SChrgAmt > N_SChrgAmtMax)
                                N_SChrgAmt = myFunctions.getVAL(N_ServiceChargeMax.ToString());
                        }
                    }
                    dtsaleamountdetails.Rows[0]["N_CommissionAmt"] = N_SChrgAmt.ToString();
                    dtsaleamountdetails.Rows[0]["N_CommissionPer"] = N_ServiceCharge.ToString();
                    dtsaleamountdetails.Rows[0]["N_TaxID"] = N_TaxID.ToString();
                    dtsaleamountdetails.Rows[0]["N_CommissionAmtF"] = N_SChrgAmt.ToString();
                    dtsaleamountdetails.AcceptChanges();


                    int N_SalesAmountID = dLayer.SaveData("Inv_SaleAmountDetails", "n_SalesAmountID", dtsaleamountdetails, connection, transaction);
                    if (N_SalesAmountID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to save Sales Invoice!"));
                    }
                    SortedList PostingParam = new SortedList();
                    PostingParam.Add("N_CompanyID", nCompanyID);
                    PostingParam.Add("X_InventoryMode", "FTSALES");
                    PostingParam.Add("N_InternalID", nSalesID);
                    PostingParam.Add("N_UserID", nUserID);
                    PostingParam.Add("X_SystemName", "ERP Cloud");
                    CostCenterTable = myFunctions.AddNewColumnToDataTable(CostCenterTable, "N_LedgerID", typeof(int), 0);
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {

                        DetailTable.Rows[j]["N_SalesID"] = nSalesID;
                        int N_InvoiceDetailId = dLayer.SaveDataWithIndex("Inv_SalesDetails", "n_SalesDetailsID", "", "", j, DetailTable, connection, transaction);
                        if (N_InvoiceDetailId > 0)
                        {
                            for (int k = 0; k < CostCenterTable.Rows.Count; k++)
                            {
                                if (myFunctions.getIntVAL(CostCenterTable.Rows[k]["rowID"].ToString()) == j)
                                {
                                    CostCenterTable.Rows[k]["N_VoucherID"] = nSalesID;
                                    CostCenterTable.Rows[k]["N_VoucherDetailsID"] = N_InvoiceDetailId;
                                    CostCenterTable.Rows[k]["N_LedgerID"] = myFunctions.getIntVAL(DetailTable.Rows[j]["n_LedgerID"].ToString());

                                }
                            }
                        }

                    }
                    //   if (CostCenterTable.Columns.Contains("rowID"))
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
                        row["N_InventoryType"] = 0;
                        row["N_InventoryID"] = dRow["N_VoucherID"];
                       row["N_InventoryDetailsID"] = dRow["N_VoucherDetailsID"];
                        row["N_CostCentreID"] = dRow["n_Segment_2"];
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
                   
                     SortedList freeTextSalesParams = new SortedList();
                           freeTextSalesParams.Add("@N_SalesId", nSalesID);

                     DataTable freeTextSalesInfo = dLayer.ExecuteDataTable("Select X_ReceiptNo,X_TransType from Inv_Sales where N_SalesId=@N_SalesId", freeTextSalesParams, connection, transaction);
                        if (freeTextSalesInfo.Rows.Count > 0)
                        {
                            try
                            {
                                myAttachments.SaveAttachment(dLayer, Attachment, X_ReceiptNo, nSalesID, freeTextSalesInfo.Rows[0]["X_TransType"].ToString().Trim(), freeTextSalesInfo.Rows[0]["X_ReceiptNo"].ToString(), nSalesID, "Free Text Sales Document", User, connection, transaction);
                            }
                             catch (Exception ex)
                            {
                                transaction.Rollback();
                                return Ok(_api.Error(User, ex));
                            }
                        }

                    int N_SegmentId = dLayer.SaveData("Inv_CostCentreTransactions", "N_CostCenterTransID", "", "", costcenter, connection, transaction);
                

                    try
                    {
                        dLayer.ExecuteNonQueryPro("SP_Acc_Inventory_Sales_Posting", PostingParam, connection, transaction);
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, ex));
                    }

                    transaction.Commit();
                    return Ok(_api.Success("free text Sales saved"));

                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }
        }
        [HttpGet("listdetails")]
        public ActionResult GetSalesDetails(int nCompanyId, int nFnYearId, string xInvoiceNO, string xTransType, bool showAllBranch, int nBranchId)
        {


            xInvoiceNO = xInvoiceNO.Replace("%2F", "/");
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
                    int N_SalesID = 0;
                    string X_MasterSql = "";
                    string X_DetailsSql = "";
                    if (showAllBranch)
                        X_MasterSql = "Select Inv_Sales.*,Inv_CustomerProjects.X_Projectname,Inv_Salesman.X_SalesmanName,Inv_Salesman.X_SalesmanCode, Inv_Customer.*,Acc_CurrencyMaster.X_ShortName,Acc_CurrencyMaster.B_default" +
                                      " from Inv_Sales Left Outer Join  Inv_CustomerProjects ON Inv_Sales.N_ProjectID = Inv_CustomerProjects.N_ProjectID And Inv_Sales.N_CompanyID = Inv_CustomerProjects.N_CompanyID " +
                                      " Left Outer Join  Inv_Salesman ON Inv_Sales.N_SalesmanID = Inv_Salesman.N_SalesmanID And Inv_Sales.N_CompanyID = Inv_Salesman.N_CompanyID " +
                                      " Inner Join  Inv_Customer ON Inv_Sales.N_CustomerID = Inv_Customer.N_CustomerID And Inv_Sales.N_CompanyID = Inv_Customer.N_CompanyID  Left outer join Acc_CurrencyMaster on inv_sales.N_CompanyId = Acc_CurrencyMaster.N_companyID and Inv_Sales.N_CurrencyID = Acc_CurrencyMaster.N_CurrencyID" +
                                      //  " Inner Join Acc_BranchMaster on Inv_Sales.N_BranchID = Acc_BranchMaster.N_BranchID And Inv_Sales.N_CompanyID = Acc_BranchMaster.N_CompanyID " +
                                      " Where  Inv_Sales.N_CompanyID=" + nCompanyId + " and Inv_Sales.X_TransType = '" + xTransType + "' and Inv_Sales.X_ReceiptNo='" + xInvoiceNO + "' and  Inv_Sales.N_FnYearID=" + nFnYearId;
                    else
                        X_MasterSql = "Select Inv_Sales.*,Inv_CustomerProjects.X_Projectname,Inv_Salesman.X_SalesmanName,Inv_Salesman.X_SalesmanCode, Inv_Customer.* ,Acc_CurrencyMaster.X_ShortName,Acc_CurrencyMaster.B_default" +
                                      " from Inv_Sales Left Outer Join  Inv_CustomerProjects ON Inv_Sales.N_ProjectID = Inv_CustomerProjects.N_ProjectID And Inv_Sales.N_CompanyID = Inv_CustomerProjects.N_CompanyID " +
                                      " Left Outer Join  Inv_Salesman ON Inv_Sales.N_SalesmanID = Inv_Salesman.N_SalesmanID And Inv_Sales.N_CompanyID = Inv_Salesman.N_CompanyID " +
                                      " Inner Join  Inv_Customer ON Inv_Sales.N_CustomerID = Inv_Customer.N_CustomerID And Inv_Sales.N_CompanyID = Inv_Customer.N_CompanyID  Left outer join Acc_CurrencyMaster on inv_sales.N_CompanyId = Acc_CurrencyMaster.N_companyID and Inv_Sales.N_CurrencyID = Acc_CurrencyMaster.N_CurrencyID " +
                                      //  " Inner Join Acc_BranchMaster on Inv_Sales.N_BranchID = Acc_BranchMaster.N_BranchID And Inv_Sales.N_CompanyID = Acc_BranchMaster.N_CompanyID " +
                                      "Where  Inv_Sales.N_CompanyID=" + nCompanyId + " and Inv_Sales.X_TransType = '" + xTransType + "' and Inv_Sales.X_ReceiptNo='" + xInvoiceNO + "' and  Inv_Sales.N_FnYearID=" + nFnYearId + " and Inv_Sales.N_BranchId=" + nBranchId + "";
                    Master = dLayer.ExecuteDataTable(X_MasterSql, Params, connection);
                    if (Master.Rows.Count == 0) { return Ok(_api.Warning("No Data Found")); }
                    N_SalesID = myFunctions.getIntVAL(Master.Rows[0]["N_SalesID"].ToString());

                    
                    object saleID = dLayer.ExecuteScalar("select N_SalesID from Inv_Sales where N_FreeTextReturnID =" + N_SalesID + " and N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + "", Params, connection);
                    if (saleID != null)
                    {
                        Master = myFunctions.AddNewColumnToDataTable(Master, "IsReturnDone", typeof(bool), true);
                        // Master = myFunctions.AddNewColumnToDataTable(Master, "X_ReturnCode", typeof(string), RetQty.ToString());
                    }
                    else
                    {
                        Master = myFunctions.AddNewColumnToDataTable(Master, "IsReturnDone", typeof(bool), false);
                        //dtPurchaseInvoice = myFunctions.AddNewColumnToDataTable(dtPurchaseInvoice, "X_ReturnCode", typeof(string), "");
                    }

                    object salesAmount = dLayer.ExecuteScalar("Select N_BillAmt from Vw_FreeTextSalesToReturn  Where N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + " and X_TransType='" + xTransType + "' and X_ReceiptNo='" + xInvoiceNO + "' ", Params, connection);
                    object returnAmout = dLayer.ExecuteScalar("Select Sum(N_BillAmt)  from Vw_FreeTextSalesToReturn  Where N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + " and X_TransType='DEBIT NOTE' and N_FreeTextReturnID=" + N_SalesID + " ", Params, connection);

                    if (myFunctions.getVAL(salesAmount.ToString()) == myFunctions.getVAL(returnAmout.ToString()))
                    {

                        Master.Rows[0]["IsReturnDone"] = true;
                    }
                    else
                    {
                         Master.Rows[0]["IsReturnDone"] = false;
                    }

                    object count = dLayer.ExecuteScalar("select count(1) from Inv_Sales where N_FreeTextReturnID =" + N_SalesID + " and N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + "", Params, connection);

                    if (myFunctions.getVAL(count.ToString())>0)
                    {
                        Master = myFunctions.AddNewColumnToDataTable(Master, "ReturnDone", typeof(bool), true);
                     }
                     else{
                        Master = myFunctions.AddNewColumnToDataTable(Master, "ReturnDone", typeof(bool), false);
                     }

                      object isReturn = dLayer.ExecuteScalar("select x_ReceiptNo from Inv_Sales where N_FreeTextReturnID =" + N_SalesID + " and N_CompanyID=" + nCompanyId + " and N_FnYearID=" + nFnYearId + "", Params, connection);

                      Master = myFunctions.AddNewColumnToDataTable(Master, "x_InvoiceNo", typeof(string), isReturn);



                    Master.AcceptChanges();



                    Master = _api.Format(Master, "Master");

                    X_DetailsSql = "Select Inv_SalesDetails.*,Acc_MastLedger.*,Inv_ItemUnit.X_ItemUnit ,Acc_TaxCategory_1.N_Amount as N_TaxPerc1, Acc_TaxCategory_1.X_DisplayName, Acc_TaxCategory_1.N_PkeyID as N_TaxId1, Acc_TaxCategory.N_PkeyID AS N_TaxId2,  Acc_TaxCategory.N_Amount AS N_TaxPerc2, Acc_TaxCategory.X_DisplayName AS X_displayName2" +
                                   " FROM         Inv_SalesDetails LEFT OUTER JOIN Acc_TaxCategory ON Inv_SalesDetails.N_TaxCategoryID2 = Acc_TaxCategory.N_PkeyID AND Inv_SalesDetails.N_CompanyID = Acc_TaxCategory.N_CompanyID LEFT OUTER JOIN Acc_TaxCategory AS Acc_TaxCategory_1 ON Inv_SalesDetails.N_TaxCategoryID1 = Acc_TaxCategory_1.N_PkeyID AND" +
                                   " Inv_SalesDetails.N_CompanyID = Acc_TaxCategory_1.N_CompanyID LEFT OUTER JOIN Acc_MastLedger ON Inv_SalesDetails.N_LedgerID = Acc_MastLedger.N_LedgerID AND Inv_SalesDetails.N_CompanyID = Acc_MastLedger.N_CompanyID LEFT OUTER JOIN Inv_ItemUnit ON Inv_SalesDetails.N_ItemUnitID = Inv_ItemUnit.N_ItemUnitID AND Inv_SalesDetails.N_CompanyID = Inv_ItemUnit.N_CompanyID " +
                                   " Where Inv_SalesDetails.N_CompanyID=" + nCompanyId + " and Inv_SalesDetails.N_SalesID=" + N_SalesID + " and Acc_MastLedger.N_FnYearID=" + nFnYearId;
                    Details = dLayer.ExecuteDataTable(X_DetailsSql, Params, connection);
                    Details = _api.Format(Details, "Details");

                    SortedList ProParams = new SortedList();
                    ProParams.Add("N_CompanyID", nCompanyId);
                    ProParams.Add("N_FnYearID", nFnYearId);
                    ProParams.Add("N_VoucherID", N_SalesID);
                    ProParams.Add("N_Flag", 1);
                    ProParams.Add("X_Type", "SALES");

                    // Acc_CostCentreTrans = dLayer.ExecuteDataTablePro("SP_Acc_Voucher_Disp_CLOUD", ProParams, connection);

                    string CostcenterSql = "SELECT X_EmpCode, X_EmpName, N_ProjectID as N_Segment_3,N_EmpID as N_Segment_4, X_ProjectCode,X_ProjectName,N_EmpID,N_ProjectID,N_CompanyID,N_FnYearID, " +
" N_VoucherID, N_VoucherDetailsID, N_CostCentreID,X_CostCentreName,X_CostCentreCode,N_BranchID,X_BranchName,X_BranchCode , " +
" N_Amount, N_LedgerID, N_CostCenterTransID, N_GridLineNo,X_Naration,0 AS N_AssetID, '' As X_AssetCode, " +
" GETDATE() AS D_RepaymentDate, '' AS X_AssetName,'' AS X_PayCode,0 AS N_PayID,0 AS N_Inst,CAST(0 AS BIT) AS B_IsCategory,D_Entrydate " +
" FROM   vw_InvFreeTextPurchaseCostCentreDetails where N_InventoryID = " + N_SalesID + " And N_InventoryType=0 And N_FnYearID=" + nFnYearId +
" And N_CompanyID=" + nCompanyId + " Order By N_InventoryID,N_VoucherDetailsID ";

                    Acc_CostCentreTrans = dLayer.ExecuteDataTable(CostcenterSql, ProParams, connection);


                    Acc_CostCentreTrans = _api.Format(Acc_CostCentreTrans, "costCenterTrans");
                    dt.Tables.Add(Acc_CostCentreTrans);
                  
                    DataTable Attachments = myAttachments.ViewAttachment(dLayer, myFunctions.getIntVAL(Master.Rows[0]["N_SalesID"].ToString()), myFunctions.getIntVAL(Master.Rows[0]["N_SalesID"].ToString()), this.FormID, myFunctions.getIntVAL(Master.Rows[0]["N_FnYearID"].ToString()), User, connection);
                    Attachments = _api.Format(Attachments, "attachments");
                    dt.Tables.Add(Attachments);

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
        public ActionResult DeleteData(int nSalesID, string X_TransType,int nFnYearID)
        {

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    var nUserID = myFunctions.GetUserID(User);
                      SortedList Params = new SortedList();
                       string xButtonAction = "Delete";
                      string X_ReceiptNo = "";

                       object count = dLayer.ExecuteScalar("select count(1) from Inv_Sales where N_FreeTextReturnID =" + nSalesID + " and N_CompanyID=" + nCompanyID, Params, connection,transaction);
                     if (myFunctions.getVAL(count.ToString())>0)
                     {
                         return Ok(_api.Error(User, "Unable to delete Free text sales"));
                     }
                      string ipAddress = "";
                            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                                ipAddress = Request.Headers["X-Forwarded-For"];
                            else
                                ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                 
                       myFunctions.LogScreenActivitys(nFnYearID,nSalesID,X_ReceiptNo.ToString(),372,xButtonAction,ipAddress,"",User,dLayer,connection,transaction);

                     object n_FnYearID = dLayer.ExecuteScalar("select N_FnyearID from Inv_Sales where N_SalesId =" + nSalesID + " and N_CompanyID=" + nCompanyID, Params, connection,transaction);
                    SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",nCompanyID},
                                {"X_TransType",X_TransType},
                                {"N_VoucherID",nSalesID},
                                {"N_UserID",nUserID},
                                 {"X_SystemName","WebRequest"}};
                    int Results = dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_Accounts", DeleteParams, connection, transaction);

                    if (Results <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to delete Free text Sales"));
                    }
                    transaction.Commit();
                    return Ok(_api.Success("Free text Sales deleted"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }
        }
    }
}