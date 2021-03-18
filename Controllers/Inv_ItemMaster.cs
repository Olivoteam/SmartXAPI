using System.Collections.Generic;
using SmartxAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using SmartxAPI.GeneralFunctions;
using System.Data;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("products")]
    [ApiController]
    public class Inv_ItemMaster : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;

        public Inv_ItemMaster(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }

        //GET api/Projects/list
        [HttpGet("list")]
        public ActionResult GetAllItems(string query, int PageSize, int Page)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string qry = "";
            if (query != "" && query != null)
            {
                qry = " and (Description like @query or [Item Code] like @query) ";
                Params.Add("@query", "%" + query + "%");
            }

            string pageQry = "DECLARE @PageSize INT, @Page INT Select @PageSize=@PSize,@Page=@Offset;WITH PageNumbers AS(Select ROW_NUMBER() OVER(ORDER BY N_ItemID) RowNo,";
            string pageQryEnd = ") SELECT * FROM    PageNumbers WHERE   RowNo BETWEEN((@Page -1) *@PageSize + 1)  AND(@Page * @PageSize) order by N_ItemID DESC";

            string sqlComandText = " * from Vw_InvItem_SearchNew where N_CompanyID=@p1 and B_Inactive=@p2 and [Item Code]<> @p3 and N_ItemTypeID<>@p4 " + qry;
            Params.Add("@p1", nCompanyID);
            Params.Add("@p2", 0);
            Params.Add("@p3", "001");
            Params.Add("@p4", 1);
            Params.Add("@PSize", PageSize);
            Params.Add("@Offset", Page);



            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sql = pageQry + sqlComandText + pageQryEnd;
                    dt = dLayer.ExecuteDataTable(sql, Params, connection);
                }
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
                return Ok(_api.Error(e));
            }

        }

        [HttpGet("dashboardList")]
        public ActionResult GetDashboardList(int nFnYearId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (Description like '%" + xSearchkey + "%' or [Item Code] like '%" + xSearchkey + "%' or Category like '%" + xSearchkey + "%' or [Item Class] like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_ItemID desc,[Item Code] desc";
            else
            {
                switch (xSortBy.Split(" ")[0])
                {
                    case "itemClass":
                        xSortBy = "[Item Class] " + xSortBy.Split(" ")[1];
                        break;
                    case "itemCode":
                        xSortBy = "N_ItemID " + xSortBy.Split(" ")[1];
                        break;
                    default: break;
                }
                xSortBy = " order by " + xSortBy;

            }

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from Vw_InvItem_Search where N_CompanyID=@p1 and B_Inactive=@p2 and [Item Code]<> @p3 and N_ItemTypeID<>@p4 " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from Vw_InvItem_Search where N_CompanyID=@p1 and B_Inactive=@p2 and [Item Code]<> @p3 and N_ItemTypeID<>@p4 " + Searchkey + " and [Item Code] not in (select top(" + Count + ") [Item Code] from Vw_InvItem_Search where N_CompanyID=@p1 and B_Inactive=@p2 and [Item Code]<> @p3 and N_ItemTypeID<>@p4 " + Searchkey + xSortBy + " ) " + xSortBy;


            Params.Add("@p1", nCompanyID);
            Params.Add("@p2", 0);
            Params.Add("@p3", "001");
            Params.Add("@p4", 1);

            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    string sqlCommandCount = "select count(*) as N_Count  from Vw_InvItem_Search where N_CompanyID=@p1 and B_Inactive=@p2 and [Item Code]<> @p3 and N_ItemTypeID<>@p4 " + Searchkey;
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





        [HttpGet("details")]
        public ActionResult GetItemDetails(string xItemCode, int nLocationID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();

            int companyid = myFunctions.GetCompanyID(User);

            QueryParams.Add("@nCompanyID", companyid);
            QueryParams.Add("@xItemCode", xItemCode);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string _sqlQuery = "SELECT * from vw_InvItemMaster where X_ItemCode=@xItemCode and N_CompanyID=@nCompanyID";

                    dt = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);

                    if (dt.Rows.Count == 0)
                    {
                        return Ok(_api.Notice("No Results Found"));
                    }

                    QueryParams.Add("@nItemID", dt.Rows[0]["N_ItemID"].ToString());
                    QueryParams.Add("@nLocationID", nLocationID);
                    QueryParams.Add("@xStockUnit", dt.Rows[0]["X_StockUnit"].ToString());
                    double OpeningStock = 0;
                    object res = dLayer.ExecuteScalar("Select SUM(Inv_StockMaster.N_OpenStock) from vw_InvItemMaster Left Outer Join Inv_StockMaster On vw_InvItemMaster.N_ItemID=Inv_StockMaster.N_ItemID and Inv_StockMaster.X_Type = 'Opening' Where vw_InvItemMaster.N_ItemID=@nItemID and vw_InvItemMaster.N_CompanyID =@nCompanyID and N_LocationID=@nLocationID and (B_IsIMEI<>1 OR B_IsIMEI IS NULL)", QueryParams, connection);
                    if (res != null)
                        OpeningStock = myFunctions.getVAL(res.ToString());

                    double UnitQty = 0;
                    double Cost = 0;
                    res = dLayer.ExecuteScalar("Select N_Qty from Inv_ItemUnit Where N_CompanyID=@nCompanyID and N_ItemID =@nItemID and X_ItemUnit=@xStockUnit", QueryParams, connection);
                    if (res != null)
                        UnitQty = myFunctions.getVAL(res.ToString());

                    double BeginStock = OpeningStock / UnitQty;
                    res = dLayer.ExecuteScalar("Select N_LPrice from vw_InvItemMaster Left Outer Join Inv_StockMaster On vw_InvItemMaster.N_ItemID=Inv_StockMaster.N_ItemID and Inv_StockMaster.X_Type = 'Opening' Where vw_InvItemMaster.N_ItemID=@nItemID and vw_InvItemMaster.N_CompanyID =@nCompanyID and N_LocationID=@nLocationID and (B_IsIMEI<>1 OR B_IsIMEI IS NULL)", QueryParams, connection);
                    if (res != null)
                        Cost = myFunctions.getVAL(res.ToString()) * UnitQty;


                    dt = myFunctions.AddNewColumnToDataTable(dt, "N_OpeningStock", typeof(string), BeginStock.ToString(myCompanyID.DecimalPlaceString));

                    res = dLayer.ExecuteScalar("Select dbo.SP_StockByStockUnit(@nCompanyID,@nItemID,@nLocationID) As [Current Stock] from Inv_StockMaster where N_ItemId=@nItemID and N_CompanyID=@nCompanyID and N_LocationID=@nLocationID order by N_StockId desc", QueryParams, connection);
                    if (res != null)
                        dt = myFunctions.AddNewColumnToDataTable(dt, "N_CurrentStock", typeof(string), myFunctions.getVAL(res.ToString()).ToString(myCompanyID.DecimalPlaceString));
                    else
                        dt = myFunctions.AddNewColumnToDataTable(dt, "N_CurrentStock", typeof(string), "0.00");



                }
                dt.AcceptChanges();
                dt = _api.Format(dt);


                return Ok(_api.Success(dt));

            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }

        //Save....
        [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable, GeneralTable, StockUnit, SalesUnit, PurchaseUnit, AddUnit1, AddUnit2;
                MasterTable = ds.Tables["master"];
                GeneralTable = ds.Tables["general"];
                StockUnit = ds.Tables["stockUnit"];
                SalesUnit = ds.Tables["salesUnit"];
                PurchaseUnit = ds.Tables["purchaseUnit"];
                AddUnit1 = ds.Tables["addUnit1"];
                AddUnit2 = ds.Tables["addUnit2"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    // Auto Gen
                    string ItemCode = "";
                    ItemCode = MasterTable.Rows[0]["X_ItemCode"].ToString();
                    if (ItemCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", MasterTable.Rows[0]["N_CompanyId"].ToString());
                        Params.Add("N_YearID", GeneralTable.Rows[0]["N_FnYearId"].ToString());
                        Params.Add("N_FormID", 53);
                        ItemCode = dLayer.GetAutoNumber("Inv_ItemMaster", "X_ItemCode", Params, connection, transaction);
                        if (ItemCode == "") { transaction.Rollback(); return Ok(_api.Warning("Unable to generate product Code")); }
                        MasterTable.Rows[0]["X_ItemCode"] = ItemCode;
                    }

                    string image = MasterTable.Rows[0]["i_Image"].ToString();
                    Byte[] imageBitmap = new Byte[image.Length];
                    imageBitmap = Convert.FromBase64String(image);
                    MasterTable.Columns.Remove("i_Image");

                    string DupCriteria = "N_CompanyID=" + myFunctions.GetCompanyID(User) + " and X_ItemCode='" + ItemCode + "'";
                    int N_ItemID = dLayer.SaveData("Inv_ItemMaster", "N_ItemID", DupCriteria, "", MasterTable, connection, transaction);
                    if (N_ItemID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error("Unable to save"));
                    }

                    if (image.Length > 0)
                        dLayer.SaveImage("Inv_ItemMaster", "i_Image", imageBitmap, "N_ItemID", N_ItemID, connection, transaction);

                    foreach (DataRow var in StockUnit.Rows) var["n_ItemID"] = N_ItemID;
                    foreach (DataRow var in SalesUnit.Rows) var["n_ItemID"] = N_ItemID;
                    foreach (DataRow var in PurchaseUnit.Rows) var["n_ItemID"] = N_ItemID;
                    foreach (DataRow var in AddUnit1.Rows) var["n_ItemID"] = N_ItemID;
                    foreach (DataRow var in AddUnit2.Rows) var["n_ItemID"] = N_ItemID;

                    int BaseUnitID = dLayer.SaveData("Inv_ItemUnit", "N_ItemUnitID", StockUnit, connection, transaction);
                    dLayer.ExecuteNonQuery("update  Inv_ItemMaster set N_ItemUnitID=" + BaseUnitID + " ,N_StockUnitID =" + BaseUnitID + " where N_ItemID=" + N_ItemID + " and N_CompanyID=N_CompanyID", Params, connection, transaction);

                    foreach (DataRow var in SalesUnit.Rows) var["n_BaseUnitID"] = BaseUnitID;
                    foreach (DataRow var in PurchaseUnit.Rows) var["n_BaseUnitID"] = BaseUnitID;
                    foreach (DataRow var in AddUnit1.Rows) var["n_BaseUnitID"] = BaseUnitID;
                    foreach (DataRow var in AddUnit2.Rows) var["n_BaseUnitID"] = BaseUnitID;

                    int N_SalesUnitID = dLayer.SaveData("Inv_ItemUnit", "N_ItemUnitID", SalesUnit, connection, transaction);
                    int N_PurchaseUnitID = dLayer.SaveData("Inv_ItemUnit", "N_ItemUnitID", PurchaseUnit, connection, transaction);
                    int N_AddUnitID1 = dLayer.SaveData("Inv_ItemUnit", "N_ItemUnitID", AddUnit1, connection, transaction);
                    int N_AddUnitID2 = dLayer.SaveData("Inv_ItemUnit", "N_ItemUnitID", AddUnit2, connection, transaction);


                    dLayer.ExecuteNonQuery("update  Inv_ItemMaster set N_SalesUnitID=" + N_SalesUnitID + ",N_PurchaseUnitID=" + N_PurchaseUnitID + " where N_ItemID=" + N_ItemID + " and N_CompanyID=N_CompanyID", Params, connection, transaction);
                    if (BaseUnitID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error("Unable to save"));
                    }



                    transaction.Commit();
                }
                return Ok(_api.Success("Product Saved"));

            }
            catch (Exception ex)
            {
                return Ok(_api.Error(ex));
            }
        }



        //GET api/Projects/list
        [HttpGet("class")]
        public ActionResult GetItemClass()
        {
            DataTable dt = new DataTable();

            string sqlComandText = "select * from Inv_ItemClass order by N_Order ASC";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlComandText, connection);
                }
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Warning("no result found"));
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



        [HttpGet("dummy")]
        public ActionResult GetPurchaseInvoiceDummy(int? Id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    string sqlCommandText = "select * from Inv_ItemMaster where N_ItemID=@p1";
                    SortedList mParamList = new SortedList() { { "@p1", Id } };
                    DataTable masterTable = dLayer.ExecuteDataTable(sqlCommandText, mParamList, connection);
                    masterTable = _api.Format(masterTable, "master");

                    string sqlCommandText2 = "select * from Inv_ItemMaster where N_ItemID=@p1";
                    SortedList dParamList = new SortedList() { { "@p1", Id } };
                    DataTable detailTable = dLayer.ExecuteDataTable(sqlCommandText2, dParamList, connection);
                    detailTable = _api.Format(detailTable, "details");

                    if (detailTable.Rows.Count == 0) { return Ok(new { }); }
                    DataSet dataSet = new DataSet();
                    dataSet.Tables.Add(masterTable);
                    dataSet.Tables.Add(detailTable);

                    return Ok(dataSet);
                }

            }
            catch (Exception e)
            {
                return Ok(_api.Error(e));
            }
        }


        [HttpDelete("delete")]
        public ActionResult DeleteData(int nItemID, int nFnYearID)
        {
            int Results = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    SqlTransaction transaction = connection.BeginTransaction();

                    object N_Result = dLayer.ExecuteScalar("Select B_YearEndProcess from Acc_FnYear Where N_CompanyID= " + nCompanyID + " and N_FnYearID= " + nFnYearID, connection, transaction);
                    if (myFunctions.getIntVAL(myFunctions.getBoolVAL(N_Result.ToString())) == 1)
                    {
                        return Ok(_api.Error("Year Closed , Unable to delete product."));
                    }

                    dLayer.DeleteData("Inv_ItemDetails", "N_MainItemID", nItemID, "", connection, transaction);
                    Results = dLayer.DeleteData("Inv_ItemMaster", "N_ItemID", nItemID, "", connection, transaction);
                    if (Results > 0)
                    {

                        dLayer.ExecuteScalar("delete from  Inv_ItemUnit  Where N_ItemID=" + nItemID + " and N_CompanyID=" + nCompanyID, connection, transaction);
                        transaction.Commit();
                        return Ok(_api.Success("Product deleted"));
                    }
                    else
                    {
                        transaction.Rollback();

                        return Ok(_api.Error("Unable to delete product category"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error("Can't be delete,It has been used!"));
            }


        }



    }


}