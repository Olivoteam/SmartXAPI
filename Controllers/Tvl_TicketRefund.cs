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
    [Route("ticketRefund")]
    [ApiController]
    public class Tvl_TicketRefund : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int N_FormID =1638 ;
        private readonly ITxnHandler txnHandler;

        public Tvl_TicketRefund(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf,ITxnHandler txn)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            txnHandler=txn;
        }        
       
        [HttpGet("details")]
        public ActionResult RefundDetails(string X_RefundNo)
        {
            DataSet dt = new DataSet();
            DataTable MasterTable = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId=myFunctions.GetCompanyID(User);
            string sqlCommandText = "select * from vw_TvlTicketRefund where N_CompanyID=@p1  and X_RefundNo=@p2";
            Params.Add("@p1", nCompanyId);  
            Params.Add("@p2", X_RefundNo);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    MasterTable = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    if (MasterTable.Rows.Count == 0)
                    {
                        return Ok(api.Warning("No Results Found"));
                    }

                    MasterTable = api.Format(MasterTable, "Master");
                    dt.Tables.Add(MasterTable);
                }
                return Ok(api.Success(dt));
            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }

        //Save....
        [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                MasterTable = ds.Tables["master"];
                int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyID"].ToString());
                int nFnYearId = myFunctions.getIntVAL(MasterTable.Rows[0]["n_FnYearID"].ToString());
                int nTicketRefundID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_TicketRefundID"].ToString());

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    SortedList PResult = new SortedList();
                    SortedList SResult = new SortedList();
                    // Auto Gen
                    string Code = "";
                    var values = MasterTable.Rows[0]["X_RefundNo"].ToString();
                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearId);
                        Params.Add("N_FormID", this.N_FormID);
                        Code = dLayer.GetAutoNumber("Tvl_TicketRefund", "X_RefundNo", Params, connection, transaction);
                        if (Code == "") { transaction.Rollback();return Ok(api.Error(User,"Unable to generate Refund Code")); }
                        MasterTable.Rows[0]["X_RefundNo"] = Code;
                    }
                    
                    if (nTicketRefundID > 0) 
                    {  
                        dLayer.DeleteData("Tvl_TicketRefund", "N_TicketRefundID", nTicketRefundID, "N_CompanyID =" + nCompanyID, connection, transaction);                        
                    }

                    nTicketRefundID = dLayer.SaveData("Tvl_TicketRefund", "N_TicketRefundID", MasterTable, connection, transaction);
                    if (nTicketRefundID <= 0)
                    {                       
                        transaction.Rollback();
                        return Ok(api.Error(User,"Unable to save"));
                    }
                    else
                    {
                        // //-------------------------------Purchase Save------------------------------------//
                        // DataSet Pds= new DataSet();
                        // DataTable PMasterDt = new DataTable();
                        // DataTable PDetailsDt = new DataTable();
                        // DataTable PFreightDt = new DataTable();
                        // DataTable PApprovalDt = new DataTable();
                        // DataTable PAttachmentDt = new DataTable();
                        
                        // int n_PIsCompleted=0;
                        // string x_PMessage="";

                        // string sqlPurchaseMaster ="SELECT Tvl_Ticketing.N_CompanyID, Tvl_Ticketing.N_FnyearID, 0 AS N_PurchaseID, '@Auto' AS X_InvoiceNo, Tvl_Ticketing.N_VendorID, GETDATE() AS D_EntryDate, "+
                        //                             " Tvl_Ticketing.D_TicketDate AS D_InvoiceDate, (Tvl_Ticketing.N_SuppFare + Tvl_Ticketing.N_SuppCommission - Tvl_Ticketing.N_Commission) AS N_InvoiceAmt, 0 AS N_DiscountAmt, 0 AS N_CashPaid, "+
                        //                             " 0 AS N_FreightAmt, Tvl_Ticketing.N_userID, 0 AS B_BeginingBalEntry, 7 AS N_PurchaseType, Tvl_Ticketing.N_TicketID AS N_PurchaseRefID, Inv_Location.N_LocationID, "+
                        //                             " Acc_BranchMaster.N_BranchID, 'PURCHASE' AS X_TransType, Tvl_Ticketing.X_Notes AS X_Description, 0 AS B_IsSaveDraft,Acc_Company.N_CurrencyID,1 AS N_ExchangeRate, "+
                        //                             " (Tvl_Ticketing.N_SuppFare + Tvl_Ticketing.N_SuppCommission - Tvl_Ticketing.N_Commission) AS N_InvoiceAmtF, 0 AS N_DiscountAmtF, 0 AS N_CashPaidF,0 AS N_FreightAmtF, "+
                        //                             " 1 AS B_FreightAmountDirect,Tvl_Ticketing.N_SuppTax AS N_TaxAmt,Tvl_Ticketing.N_SuppTax AS N_TaxAmtF,Tvl_Ticketing.N_TaxCategoryID,2 AS N_PaymentMethod,Tvl_Ticketing.D_TicketDate D_PrintDate, "+
                        //                             " Tvl_Ticketing.N_TaxPercentage AS n_TaxPercentage,Tvl_Ticketing.N_userID AS N_CreatedUser,GETDATE() AS D_CreatedDate,565 AS N_FormID,0 AS N_POrderID "+
                        //                             " FROM         Tvl_Ticketing INNER JOIN "+
                        //                             " Inv_Location ON Tvl_Ticketing.N_CompanyID = Inv_Location.N_CompanyID AND Inv_Location.B_IsDefault = 1 INNER JOIN "+
                        //                             " Acc_BranchMaster ON Tvl_Ticketing.N_CompanyID = Acc_BranchMaster.N_CompanyID AND Acc_BranchMaster.B_DefaultBranch = 1 INNER JOIN "+
                        //                             " Acc_Company ON Tvl_Ticketing.N_CompanyID = Acc_Company.N_CompanyID "+
                        //                             " WHERE      Tvl_Ticketing.N_CompanyID="+ nCompanyID +" AND Tvl_Ticketing.N_TicketID= "+ nTicketID;

                        // PMasterDt = dLayer.ExecuteDataTable(sqlPurchaseMaster, Params, connection,transaction);
                        // PMasterDt = api.Format(PMasterDt, "master");
                        // Pds.Tables.Add(PMasterDt);

                        // string sqlPurchaseDetails ="SELECT     Tvl_Ticketing.N_CompanyID, 0 AS N_PurchaseID, 0 AS N_PurchaseDetailsID, Inv_ItemMaster.N_ItemID, 1 AS N_Qty, "+
                        //                             " (Tvl_Ticketing.N_SuppFare + Tvl_Ticketing.N_SuppCommission - Tvl_Ticketing.N_Commission) AS N_PPrice, Inv_ItemMaster.N_ItemUnitID, 1 AS N_QtyDisplay, "+                                                    
                        //                             " (Tvl_Ticketing.N_SuppFare + Tvl_Ticketing.N_SuppCommission - Tvl_Ticketing.N_Commission) AS N_Cost, GETDATE() AS D_Entrydate, Acc_BranchMaster.N_BranchID, Inv_Location.N_LocationID, "+
                        //                             " Acc_Company.N_CurrencyID,1 AS N_ExchangeRate,(Tvl_Ticketing.N_SuppFare + Tvl_Ticketing.N_SuppCommission - Tvl_Ticketing.N_Commission) AS N_PPriceF, "+
                        //                             " (Tvl_Ticketing.N_SuppFare + Tvl_Ticketing.N_SuppCommission - Tvl_Ticketing.N_Commission) AS N_CostF,Tvl_Ticketing.N_TaxCategoryID AS N_TaxCategoryID1, "+
                        //                             " Tvl_Ticketing.N_TaxPercentage AS N_TaxPercentage1,Tvl_Ticketing.N_SuppTax AS N_TaxAmt1 ,inv_itemunit.X_ItemUnit"+
                        //                             " FROM         Tvl_Ticketing INNER JOIN "+
                        //                             " Inv_ItemMaster ON Tvl_Ticketing.N_CompanyID = Inv_ItemMaster.N_CompanyID AND Inv_ItemMaster.X_ItemCode = '001' INNER JOIN "+
                        //                             " Acc_BranchMaster ON Tvl_Ticketing.N_CompanyID = Acc_BranchMaster.N_CompanyID AND Acc_BranchMaster.B_DefaultBranch = 1 INNER JOIN "+
                        //                             " Inv_Location ON Tvl_Ticketing.N_CompanyID = Inv_Location.N_CompanyID AND Inv_Location.B_IsDefault = 1 INNER JOIN "+
                        //                             " Acc_Company ON Tvl_Ticketing.N_CompanyID = Acc_Company.N_CompanyID INNER JOIN "+
                        //                             " inv_itemunit ON inv_itemunit.N_CompanyID=Inv_ItemMaster.N_CompanyID AND inv_itemunit.N_ItemUnitID=Inv_ItemMaster.N_ItemUnitID "+
                        //                             " WHERE      Tvl_Ticketing.N_CompanyID= "+nCompanyID+" and Tvl_Ticketing.N_TicketID= "+nTicketID;

                        // PDetailsDt = dLayer.ExecuteDataTable(sqlPurchaseDetails, Params, connection,transaction);
                        // PDetailsDt = api.Format(PDetailsDt, "details");
                        // Pds.Tables.Add(PDetailsDt);       

                        // PApprovalDt = dLayer.ExecuteDataTable("select N_CompanyID ,'true' as isEditable,0 as approvalID,0 as isApprovalSystem, 565 AS formID,0 AS saveTag, 1 as nextApprovalLevel,'' AS btnSaveText from Acc_Company where N_CompanyID= "+nCompanyID, Params, connection,transaction);
                        // PApprovalDt = api.Format(PApprovalDt, "approval");
                        // Pds.Tables.Add(PApprovalDt);    

                        // PFreightDt = api.Format(PFreightDt, "freightCharges");
                        // Pds.Tables.Add(PFreightDt);   

                        // PAttachmentDt = api.Format(PAttachmentDt, "attachments");
                        // Pds.Tables.Add(PAttachmentDt);                                           

                        // PResult=txnHandler.PurchaseSaveData( Pds ,User , dLayer, connection, transaction);

                        // n_PIsCompleted=myFunctions.getIntVAL(PResult["b_IsCompleted"].ToString());
                        // x_PMessage=PResult["x_Msg"].ToString();

                        // if(n_PIsCompleted==0)
                        // {
                        //     transaction.Rollback();
                        //     return Ok(api.Error(User, x_PMessage));
                        // }
                        // //-------------------------------^^^^^^^^^^^^^^------------------------------------//

                        // //-------------------------------Sales Save------------------------------------//
                        // DataSet Sds= new DataSet();
                        // DataTable SMasterDt = new DataTable();
                        // DataTable SDetailsDt = new DataTable();
                        // DataTable SAmountDetailsDt = new DataTable();
                        // DataTable SApprovalDt = new DataTable();
                        // DataTable SAttachmentDt = new DataTable();
                        // DataTable SAdvanceDt = new DataTable();
                        
                        // int n_SIsCompleted=0;
                        // string x_SMessage="";

                        // string sqlSalesMaster=" SELECT     Tvl_Ticketing.N_CompanyID, Tvl_Ticketing.N_FnyearID, 0 AS N_SalesId, '@Auto' AS X_ReceiptNo, REPLACE(CONVERT(VARCHAR,Tvl_Ticketing.D_TicketDate,121),'.',':') AS D_SalesDate, GETDATE() AS D_EntryDate, "+
                        //                             " Tvl_Ticketing.N_CustomerID AS N_CustomerId, Tvl_Ticketing.N_CustomerAmt AS N_BillAmt, 0 AS N_DiscountAmt, 0 AS N_FreightAmt, 0 AS N_CashReceived, "+
                        //                             " Tvl_Ticketing.X_Description AS x_Notes, Tvl_Ticketing.N_userID, 0 AS N_SalesOrderID, 0 AS B_BeginingBalEntry, 7 AS N_SalesType, Tvl_Ticketing.N_TicketID AS N_SalesRefID, "+
                        //                             " Inv_Location.N_LocationID, Acc_BranchMaster.N_BranchID, 'SALES' AS X_TransType, 0 AS B_IsSaveDraft, 2 AS N_PaymentMethodId, Tvl_Ticketing.D_TicketDate AS D_PrintDate, "+
                        //                             " 565 AS N_FormID, Acc_Company.N_CurrencyID,1 AS N_ExchangeRate,Tvl_Ticketing.N_CustomerAmt AS N_BillAmtF,0 AS N_DiscountAmtF,0 AS N_CashReceivedF, "+
                        //                             " Tvl_Ticketing.N_userID AS N_CreatedUser,GETDATE() AS D_CreatedDate, 0 AS n_DeliveryNoteId, Tvl_Ticketing.N_SalesmanID "+
                        //                             " FROM         Tvl_Ticketing INNER JOIN "+
                        //                             " Inv_Location ON Tvl_Ticketing.N_CompanyID = Inv_Location.N_CompanyID AND Inv_Location.B_IsDefault = 1 INNER JOIN "+
                        //                             " Acc_BranchMaster ON Tvl_Ticketing.N_CompanyID = Acc_BranchMaster.N_CompanyID AND Acc_BranchMaster.B_DefaultBranch = 1 INNER JOIN "+
                        //                             " Acc_Company ON Tvl_Ticketing.N_CompanyID = Acc_Company.N_CompanyID "+
                        //                             " WHERE     Tvl_Ticketing.N_CompanyID= "+nCompanyID+" and Tvl_Ticketing.N_TicketID= "+nTicketID;

                        // SMasterDt = dLayer.ExecuteDataTable(sqlSalesMaster, Params, connection,transaction);
                        // SMasterDt = api.Format(SMasterDt, "master");
                        // Sds.Tables.Add(SMasterDt); 

                        // string sqlSalesDetails ="SELECT     Tvl_Ticketing.N_CompanyID, 0 AS N_SalesID, 0 AS N_SalesDetailsID, Inv_ItemMaster.N_ItemID, 1 AS N_Qty, Tvl_Ticketing.N_CustomerAmt AS N_Sprice, Inv_ItemMaster.N_ClassID, "+
                        //                         " Inv_ItemMaster.N_ItemID AS N_MainItemID, Inv_ItemMaster.N_ItemUnitID, 1 AS N_QtyDisplay, Tvl_Ticketing.N_CustomerAmt AS N_Cost, GETDATE() AS D_EntryDate, "+
                        //                         " Inv_Location.N_LocationID,Acc_BranchMaster.N_BranchID,Tvl_Ticketing.N_CustomerAmt AS N_SpriceF,0 AS n_SalesOrderID,0 AS N_SalesQuotationID "+
                        //                         " FROM         Tvl_Ticketing INNER JOIN "+
                        //                         " Inv_ItemMaster ON Tvl_Ticketing.N_CompanyID = Inv_ItemMaster.N_CompanyID AND Inv_ItemMaster.X_ItemCode = '001' INNER JOIN "+
                        //                         " Inv_Location ON Tvl_Ticketing.N_CompanyID = Inv_Location.N_CompanyID AND Inv_Location.B_IsDefault=1 INNER JOIN "+
                        //                         " Acc_BranchMaster ON Tvl_Ticketing.N_CompanyID = Acc_BranchMaster.N_CompanyID AND B_DefaultBranch=1 "+
                        //                         " WHERE         Tvl_Ticketing.N_CompanyID= "+nCompanyID+" and Tvl_Ticketing.N_TicketID= "+nTicketID;     

                        // SDetailsDt = dLayer.ExecuteDataTable(sqlSalesDetails, Params, connection,transaction);
                        // SDetailsDt = api.Format(SDetailsDt, "details");
                        // Sds.Tables.Add(SDetailsDt);  

                        // string sqlSalesAmountDetails ="SELECT     Tvl_Ticketing.N_CompanyID,Acc_BranchMaster.N_BranchID,0 AS N_SalesID,Tvl_Ticketing.N_CustomerID,Tvl_Ticketing.N_CustomerAmt AS N_Amount, "+
			            //                         " 0 AS N_CommissionAmt, 0 AS N_CommissionPer,0 AS N_SalesAmountID,0 AS N_TaxID,Tvl_Ticketing.N_CustomerAmt AS N_AmountF, 0 AS N_CommissionAmtF "+
                        //                         " FROM         Tvl_Ticketing INNER JOIN "+
                        //                         " Acc_BranchMaster ON Tvl_Ticketing.N_CompanyID = Acc_BranchMaster.N_CompanyID AND Acc_BranchMaster.B_DefaultBranch=1 "+
                        //                         " WHERE        Tvl_Ticketing.N_CompanyID= "+nCompanyID+" and Tvl_Ticketing.N_TicketID= "+nTicketID;

                        // SAmountDetailsDt = dLayer.ExecuteDataTable(sqlSalesAmountDetails, Params, connection,transaction);
                        // SAmountDetailsDt = api.Format(SAmountDetailsDt, "saleamountdetails");
                        // Sds.Tables.Add(SAmountDetailsDt);                                                  

                        // SApprovalDt = dLayer.ExecuteDataTable("select N_CompanyID ,'true' as isEditable,0 as approvalID,0 as isApprovalSystem, 565 AS formID,0 AS saveTag, 1 as nextApprovalLevel,'' AS btnSaveText from Acc_Company where N_CompanyID= "+nCompanyID, Params, connection,transaction);
                        // SApprovalDt = api.Format(SApprovalDt, "approval");
                        // Sds.Tables.Add(SApprovalDt);  

                        // SAttachmentDt = api.Format(SAttachmentDt, "attachments");
                        // Sds.Tables.Add(SAttachmentDt);  

                        // SAdvanceDt = api.Format(SAdvanceDt, "advanceTable");
                        // Sds.Tables.Add(SAdvanceDt);                                               

                        // string ipAddress = "";
                        // if (  Request.Headers.ContainsKey("X-Forwarded-For"))
                        //     ipAddress = Request.Headers["X-Forwarded-For"];
                        // else
                        //     ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();

                        // SResult=txnHandler.SalesSaveData( Sds ,ipAddress,User , dLayer, connection, transaction);
                        
                        // n_SIsCompleted=myFunctions.getIntVAL(SResult["b_IsCompleted"].ToString());
                        // x_SMessage=SResult["x_Msg"].ToString();

                        // if(n_SIsCompleted==0)
                        // {
                        //     transaction.Rollback();
                        //     return Ok(api.Error(User, x_SMessage));
                        // }
                        // //-------------------------------^^^^^^^^^^^^^^------------------------------------//

                        transaction.Commit();
                        return Ok(api.Success("Ticket Created"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }
        }

         [HttpGet("list")]
        public ActionResult GetRefundList(int? nCompanyId, int nFnYearId ,int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_RefundNo like '%" + xSearchkey + "%' or X_TicketNo like '%" + xSearchkey + "%' or X_InvoiceNo like '%" + xSearchkey + "%' or X_Passenger like '%" + xSearchkey + "%' or X_Route like '%" + xSearchkey +"%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_TicketRefundID desc";
            else
            {
                switch (xSortBy.Split(" ")[0])
                {
                    case "X_RefundNo":
                        xSortBy = "X_RefundNo " + xSortBy.Split(" ")[1];
                        break;
                    default: break;
                }
                xSortBy = " order by " + xSortBy;
            }


            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_TvlTicketRefund where N_CompanyID=@nCompanyId and N_FnyearID=@nFnYearId  " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_TvlTicketRefund where N_CompanyID=@nCompanyId and N_FnyearID=@nFnYearId " + Searchkey + " and N_TicketRefundID not in (select top(" + Count + ") N_TicketRefundID from vw_TvlTicketRefund where N_CompanyID=@nCompanyId and N_FnyearID=@nFnYearId " + xSortBy + " ) " + " " + xSortBy;

            Params.Add("@nCompanyId", nCompanyID);
            Params.Add("@nFnYearId", nFnYearId);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    SortedList OutPut = new SortedList();

                    sqlCommandCount = "select count(*) as N_Count  from vw_TvlTicketRefund  where N_CompanyID=@nCompanyId and N_FnyearID=@nFnYearId " + Searchkey + "";
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
                return Ok(api.Error(User, e));
            }
        }

   
      
        [HttpDelete("delete")]
        public ActionResult DeleteData(int nTicketRefundID)
        {

            int Results = 0;
            int nCompanyID=myFunctions.GetCompanyID(User);
            try
            {                        
                SortedList Params = new SortedList();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    Results = dLayer.DeleteData("Tvl_TicketRefund ", "N_TicketRefundID", nTicketRefundID, "N_CompanyID =" + nCompanyID, connection, transaction);
                
                    if (Results > 0)
                    {
                        transaction.Commit();
                        return Ok(api.Success("Ticket refund deleted"));
                    }
                    else
                    {
                        return Ok(api.Error(User,"Unable to delete "));
                    }
                }

            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }



        }
    }
}

