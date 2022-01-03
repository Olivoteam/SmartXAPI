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

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("openingStock")]
    [ApiController]
    public class Inv_OpeningStock : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly int FormID;
        private readonly IMyFunctions myFunctions;

        private readonly string connectionString;



        public Inv_OpeningStock(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 88;
        }

        [HttpGet("ProductList")]
        public ActionResult GetAllItems(string query, int PageSize, int Page, int nLocationID, bool b_AllBranchData)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();

            SortedList Params = new SortedList();

            string qry = "";
            string Category = "";
            string Condition = "";
            string xCriteria = "";
            // if (b_AllBranchData)
            //     xCriteria = " N_FnYearID=@p2 and N_PurchaseType=0 and X_TransType=@p4 and B_YearEndProcess=0 and N_CompanyID=@p1 ";
            // else
            //     xCriteria = " N_FnYearID=@p2 and N_PurchaseType=0 and X_TransType=@p4 and B_YearEndProcess=0 and N_BranchID=@p3 and N_CompanyID=@p1 ";

            if (query != "" && query != null)
            {
                qry = " and (Description like @query or [Item Code] like @query ) ";
                Params.Add("@query", "%" + query + "%");
            }

            //xCriteria = "where N_CompanyID=" + nCompanyID + " and (B_IsIMEI=0 or B_IsIMEI is null)  and  ([Item Class]='Stock Item' OR [Item Class]='Assembly Item'  and N_LocationID=" + nLocationID + "";
            xCriteria = "where vw_InvItem_Search.N_CompanyID=" + nCompanyID + " and (vw_InvItem_Search.B_IsIMEI=0 or vw_InvItem_Search.B_IsIMEI is null)  and  (vw_InvItem_Search.[Item Class]='Stock Item' OR vw_InvItem_Search.[Item Class]='Assembly Item')  and Inv_StockMaster.N_LocationID=" + nLocationID + "";



            // string pageQry = "DECLARE @PageSize INT, @Page INT Select @PageSize=@PSize,@Page=@Offset;WITH PageNumbers AS(Select ROW_NUMBER() OVER(ORDER BY vw_InvItem_Search_cloud.N_ItemID) RowNo,";
            // string pageQryEnd = ") SELECT * FROM    PageNumbers WHERE   RowNo BETWEEN((@Page -1) *@PageSize + 1)  AND(@Page * @PageSize) order by N_ItemID DESC";
            string pageQry = "DECLARE @PageSize INT, @Page INT Select @PageSize=@PSize,@Page=@Offset;WITH PageNumbers AS(Select ROW_NUMBER() OVER(ORDER BY vw_InvItem_Search.N_ItemID) RowNo,";
            string pageQryEnd = ") SELECT * FROM    PageNumbers WHERE   RowNo BETWEEN((@Page -1) *@PageSize + 1)  AND(@Page * @PageSize) order by N_ItemID DESC";

            // string sqlComandText = " * from vw_InvItem_Search_cloud where N_CompanyID=@p1 and B_Inactive=@p2 and [Item Code]<> @p3 and N_ItemTypeID<>@p4 " + qry;

            string sqlComandText = "vw_InvItem_Search.*,dbo.SP_Cost(vw_InvItem_Search.N_ItemID,vw_InvItem_Search.N_CompanyID,'') As N_LPrice ,dbo.SP_SellingPrice(vw_InvItem_Search.N_ItemID,vw_InvItem_Search.N_CompanyID) As N_SPrice  From vw_InvItem_Search Where  N_CompanyID=" + nCompanyID + " and (B_IsIMEI=0 or B_IsIMEI is null)  and  ([Item Class]='Stock Item' OR [Item Class]='Assembly Item')" + qry;

            Params.Add("@p1", nCompanyID);
            Params.Add("@PSize", PageSize);
            Params.Add("@Offset", Page);



            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = pageQry + sqlComandText + pageQryEnd;
                    dt = dLayer.ExecuteDataTable(sql, Params, connection);

                    dt = myFunctions.AddNewColumnToDataTable(dt, "N_OpenStock", typeof(double), 0);
                    dt = myFunctions.AddNewColumnToDataTable(dt, "N_CurrentStock", typeof(double), 0);
                    dt = myFunctions.AddNewColumnToDataTable(dt, "N_StockID", typeof(int), 0);
                    dt = myFunctions.AddNewColumnToDataTable(dt, "N_LocationID", typeof(int), nLocationID);
                    dt = myFunctions.AddNewColumnToDataTable(dt, "X_BatchCode", typeof(string), null);
                    dt = myFunctions.AddNewColumnToDataTable(dt, "D_ExpiryDate", typeof(string), "");

                    foreach (DataRow dRow in dt.Rows)
                    {
                        DataTable stockTable = new DataTable();
                        string sqlStock = "SELECT isnull(N_OpenStock,0) as N_OpenStock,isnull(N_StockID,0) as N_StockID,isnull(N_LPrice,0) as N_LPrice,isnull(N_SPrice,0) as N_SPrice,isnull(N_CurrentStock,0) as N_CurrentStock,isnull(N_LocationID,0) as N_LocationID,X_BatchCode,D_ExpiryDate FROM Inv_StockMaster where N_CompanyID=" + nCompanyID + " and  N_ItemID= " + myFunctions.getIntVAL(dRow["N_ItemID"].ToString()) + " and N_LocationID=" + nLocationID + " and X_Type='Opening'";
                        stockTable = dLayer.ExecuteDataTable(sqlStock, Params, connection);
                        if (stockTable.Rows.Count == 1)
                        {
                            dRow["N_OpenStock"] = stockTable.Rows[0]["N_OpenStock"];
                            dRow["N_StockID"] = stockTable.Rows[0]["N_StockID"];
                            dRow["N_CurrentStock"] = stockTable.Rows[0]["N_CurrentStock"];
                            dRow["N_LPrice"] = stockTable.Rows[0]["N_LPrice"];
                            dRow["N_SPrice"] = stockTable.Rows[0]["N_SPrice"];
                            dRow["N_LocationID"] = stockTable.Rows[0]["N_LocationID"];
                            dRow["X_BatchCode"] = stockTable.Rows[0]["X_BatchCode"];
                            dRow["D_ExpiryDate"] = stockTable.Rows[0]["D_ExpiryDate"];


                        }

                    }

                }
                dt.AcceptChanges();
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Warning("No Results Found"));
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
                    DataTable DetailTable;
                    DataTable openingStock;
                    DataTable MasterTable;
                    DataTable StockTable;
                    int N_StockID = 0;
                    int N_OpeningID = 0;
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    SortedList Params = new SortedList();
                    DetailTable = ds.Tables["details"];
                    MasterTable = ds.Tables["master"];
                    openingStock = ds.Tables["openingStock"];
                    Params.Add("N_CompanyID", nCompanyID);
                    int nFnYearID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_FnYearID"].ToString());
                    int nBranchID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_BranchID"].ToString());
                    DetailTable.Columns.Remove("n_ItemUnitID");
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {
                        int StockID = myFunctions.getIntVAL(DetailTable.Rows[j]["n_StockID"].ToString());
                        if (StockID == 0)
                        {
                            DetailTable.Rows[j]["n_StockID"] = dLayer.ExecuteScalar("SELECT isnull(max(N_StockID),'0') + 1 FROM Inv_StockMaster", Params, connection, transaction).ToString();
                            StockID = myFunctions.getIntVAL(DetailTable.Rows[j]["n_StockID"].ToString());
                        }
                        dLayer.ExecuteNonQuery("Update Inv_ItemMaster SET N_Rate=" + myFunctions.getVAL(DetailTable.Rows[j]["n_SPrice"].ToString()) + " WHERE N_ItemID=" + myFunctions.getIntVAL(DetailTable.Rows[j]["n_ItemID"].ToString()) + " and N_CompanyID=" + nCompanyID + "", Params, connection, transaction);
                        openingStock.Rows[j]["N_TransID"] = StockID;

                    }
                    string stockMasterSql = "select * from Inv_StockMaster where  N_CompanyID=" + nCompanyID + "";
                    StockTable = dLayer.ExecuteDataTable(stockMasterSql, Params, connection,transaction);
                    if (StockTable.Rows.Count > 0)

                    {
                        foreach (DataRow stockItem in StockTable.Rows)
                        {
                            foreach (DataRow DetailItem in DetailTable.Rows)
                            {
                                if (stockItem["N_StockID"] == DetailItem["N_StockID"])
                                {
                                    if (stockItem["N_CurrentStock"] != stockItem["N_OpenStock"])
                                    {
                                        transaction.Rollback();
                                        return Ok(_api.Error(User, "Transaction already Processed for the Product "));
                                    }
                                }
                            }
                        }
                    }

                    DetailTable.AcceptChanges();
                    N_StockID = dLayer.SaveData("Inv_StockMaster", "N_StockID", "", "", DetailTable, connection, transaction);
                    if (N_StockID < 0)
                    {

                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to save!"));

                    }

                    N_OpeningID = dLayer.SaveData("Inv_OpeningStock", "N_TransID", "", "", openingStock, connection, transaction);

                    openingStock.AcceptChanges();
                    N_OpeningID = dLayer.SaveData("Inv_OpeningStock", "N_TransID", "", "", openingStock, connection, transaction);
                    if (N_OpeningID < 0)
                    {

                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to save!"));

                    }

                    SortedList PostingParam = new SortedList();


                    PostingParam.Add("@N_CompanyID", nCompanyID);
                    PostingParam.Add("@N_FnYearID", nFnYearID);
                    PostingParam.Add("@Mode", "IOB");
                    PostingParam.Add("@N_UserID", myFunctions.GetUserID(User));
                    PostingParam.Add("@N_PartyID", 0);
                    PostingParam.Add("@X_EntryFrom", "Opening Stock Entry");
                    PostingParam.Add("@N_BranchId", nBranchID);
                    try
                    {
                        dLayer.ExecuteNonQueryPro("SP_Acc_BeginingBalancePosting_Ins", PostingParam, connection, transaction);

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


        [HttpGet("stockDetails")]
        public ActionResult GetStockDetailsDetails(int nCompanyId, int nFnYearId, int nBranchId, int nLocationID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {

                    connection.Open();
                    DataSet dt = new DataSet();
                    SortedList QueryParamsList = new SortedList();
                    QueryParamsList.Add("@nCompanyID", nCompanyId);
                    QueryParamsList.Add("@nFnYearID", nFnYearId);
                    DataTable MasterTable = new DataTable();
                    DataTable DetailTable = new DataTable();
                    string stockQry = "";
                    string DetailSql = "";
                    stockQry = "select * from  vw_OpeningStockFill where N_CompanyID = " + nCompanyId + " and N_LocationID=" + nLocationID + " and  (N_ClassID <> 1 and N_ClassID <> 4) and (B_IsIMEI<>1 OR B_IsIMEI IS NULL)  and  N_OpenStock>=0  order by X_Itemcode,X_ItemName,X_Category";
                    DetailTable = dLayer.ExecuteDataTable(stockQry, QueryParamsList, connection);
                    DetailTable = myFunctions.AddNewColumnToDataTable(DetailTable, "N_Processed", typeof(int), 0);
                    foreach (DataRow row in DetailTable.Rows)
                    {
                        if (myFunctions.getIntVAL(row["N_OpenStock"].ToString()) != myFunctions.getIntVAL(row["N_CurrentStock"].ToString()))
                        {
                            row["N_Processed"] = 1;
                        }
                        else
                        {
                            row["N_Processed"] = 0;
                        }
                    }
                    DetailTable = _api.Format(DetailTable, "Details");
                    dt.Tables.Add(DetailTable);
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }
    }
}





























































