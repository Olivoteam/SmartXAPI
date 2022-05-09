using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using SmartxAPI.GeneralFunctions;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using System.Collections.Generic;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("invstatusmanager")]
    [ApiController]



    public class Inv_Dashboard : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;


        public Inv_Dashboard(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }

        [HttpGet]
        public ActionResult GetInventoryStatus(int? nCompanyID, int? nFnyearID, int nLocationID, string xType, bool bAllBranchData, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            int Count = (nPage - 1) * nSizeperpage;
            string Searchkey = "";
            string sqlCommandText = "", sqlCommandCount = "";

            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnyearID", nFnyearID);
            Params.Add("@nLocationID", nLocationID);
            Params.Add("@xType", xType);

            //      if (cmbItemSearchBy.SelectedIndex == 0)
            //     xType = "All";
            // else if (cmbItemSearchBy.SelectedIndex == 1)
            //     xType = "N_MinQty";
            // else if (cmbItemSearchBy.SelectedIndex == 2)
            //     xType = "N_ReOrderQty";
            // else
            //     xType = "NoStock";
            //     xType = "No Stock";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_ItemCode like'%" + xSearchkey + "%'or [Emp Name] like'%" + xSearchkey + "%'or X_VacType like'%" + xSearchkey + "%' or N_VacDays like'%" + xSearchkey + "%'or X_VacRemarks like'%" + xSearchkey + "%' or cast(D_VacDateTo as VarChar) like'%" + xSearchkey + "%' or cast(d_VacDateFrom as VarChar) like'%" + xSearchkey + "%' or x_CurrentStatus like'%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by X_ItemCode desc";
            else
                xSortBy = " order by " + xSortBy;

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    bool B_PartNo = Convert.ToBoolean(myFunctions.getIntVAL(myFunctions.ReturnSettings("82", "PartNo_InGrid", "N_Value", myFunctions.getIntVAL(nCompanyID.ToString()), dLayer, connection)));
                    object N_LocationCount = dLayer.ExecuteScalar("Select count(1) from inv_Location where  N_CompanyID=@nCompanyID", Params, connection);
                    string X_TableName = "", X_VisibleFieldList = "", X_Crieteria = "", X_OrderByField = "";
                    if (myFunctions.getIntVAL(N_LocationCount.ToString()) > 1)
                    {
                        // X_HideFieldList = "N_CompanyID,N_CategoryID,N_LocationID,N_CurrStock,N_MinQty,N_ReOrderQty";
                        X_OrderByField = "X_ItemCode ASC";
                        X_TableName = "vw_stockstatusbylocation";
                        if (B_PartNo)
                        {
                            X_VisibleFieldList = "X_PartNo,X_ItemCode,X_ItemName,X_Category,X_PreferredVendor,X_LocationName,X_Rack,N_CurrentStock,X_ItemUnit,SOQty,X_ItemManufacturer";
                        }
                        else
                        {
                            X_VisibleFieldList = "X_ItemCode,X_ItemName,X_Category,X_PreferredVendor,X_LocationName,X_Rack,N_CurrentStock,X_ItemUnit,SOQty";
                        }


                        if (bAllBranchData == true)
                        {
                            if (xType == "All")
                                X_Crieteria = " N_CompanyID=@nCompanyID";
                            else if (xType == "NoStock")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_CurrStock=0 ";
                            else if (xType == "MinimumQty")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_CurrStock <= N_MinQty";
                            else
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_CurrStock <= N_ReOrderQty";
                        }
                        else
                        {
                            if (xType == "All")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_LocationID=@nLocationID";
                            else if (xType == "NoStock")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_LocationID=@nLocationID and  N_CurrStock = 0 ";
                            else if (xType == "MinimumQty")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_LocationID=@nLocationID and N_CurrStock <= N_MinQty";
                            else
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_LocationID=@nLocationID and N_CurrStock <= N_ReOrderQty";
                        }
                    }
                    else
                    {
                        //X_HideFieldList = "N_CompanyID,N_CategoryID,N_LocationID,N_CurrStock,N_MinQty,N_ReOrderQty";
                        X_OrderByField = "X_ItemCode ASC";
                        X_TableName = "vw_stockstatusbylocation";

                        if (B_PartNo)
                        {
                            X_VisibleFieldList = "X_PartNo,X_ItemCode,X_ItemName,X_Category,X_PreferredVendor,X_Rack,N_CurrentStock,X_ItemUnit,SOQty,X_ItemManufacturer";
                        }
                        else
                        {
                            X_VisibleFieldList = "X_ItemCode,X_ItemName,X_Category,X_PreferredVendor,X_Rack,N_CurrentStock,X_ItemUnit,SOQty";
                        }

                        if (bAllBranchData == true)
                        {
                            if (xType == "All")
                                X_Crieteria = " N_CompanyID=@nCompanyID";
                            else if (xType == "NoStock")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_CurrStock = 0 ";
                            else if (xType == "MinimumQty")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_CurrStock <= N_MinQty";
                            else
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_CurrStock <= N_ReOrderQty";
                        }
                        else
                        {
                            if (xType == "All")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_LocationID=@nLocationID";
                            else if (xType == "NoStock")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_LocationID=@nLocationID and  N_CurrStock=0 ";
                            else if (xType == "N_MinQty")
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_LocationID=@nLocationID and N_CurrStock <= N_MinQty";
                            else
                                X_Crieteria = " N_CompanyID=@nCompanyID and N_LocationID=@nLocationID and N_CurrStock <= N_ReOrderQty";
                        }
                    }

                    if (Count == 0)
                        sqlCommandText = "Select top(" + nSizeperpage + ") " + X_VisibleFieldList + " from " + X_TableName + " where " + X_Crieteria + " " + Searchkey + " " + xSortBy + "";
                    else
                        sqlCommandText = "Select top(" + nSizeperpage + ") " + X_VisibleFieldList + " from " + X_TableName + " where " + X_Crieteria + " " + Searchkey + " and N_ItemID not in (select top(" + Count + ") N_ItemID from" + X_TableName + " where " + X_Crieteria + ") " + xSortBy + "";
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    sqlCommandCount = "select count(*) as N_Count From " + X_TableName + " where " + X_Crieteria + " " + Searchkey + " ";
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    dt = _api.Format(dt);
                    SortedList OutPut = new SortedList();
                    OutPut.Add("Details", _api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                    if (dt.Rows.Count == 0)
                    {
                        return Ok(_api.Notice("No Results Found"));
                    }
                    else
                    {
                        return Ok(_api.Success(dt));
                    }
                }

            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }

        [HttpGet("count")]
        public ActionResult GetCount(int nLocationID, bool bAllBranchData, int nBranchID)
        {
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);
            string sqlAll = "", sqlNoStock = "", sqlMinQty = "", sqlReOrder = "", criteria = "";
            string sqlTopSell = "", sqlInvValue = "";

            if (bAllBranchData)
                criteria = "N_CompanyID =" + nCompanyID;
            else
                criteria = "N_CompanyID =" + nCompanyID;
            // criteria="N_CompanyID ="+nCompanyID+" and N_LocationID="+nLocationID+" and N_BranchID="+nBranchID;

            sqlAll = "SELECT COUNT(*) as N_Count FROM vw_InvItem_Search_cloud WHERE " + criteria + " and B_Inactive=0 and [Item Code]<> '001' and N_ItemTypeID<>1";
            sqlNoStock = "SELECT COUNT(*) as N_Count FROM vw_stockstatusbylocation WHERE " + criteria + " and N_CurrStock = 0";
            sqlMinQty = "SELECT COUNT(*) as N_Count FROM vw_stockstatusbylocation WHERE " + criteria + " and N_CurrStock <=N_MinQty";
            sqlReOrder = "SELECT COUNT(*) as N_Count FROM vw_stockstatusbylocation WHERE " + criteria + " and N_CurrStock <=N_ReOrderQty";

            sqlTopSell = "select Top 5 * from vw_TopSellingItem where N_CompanyID=" + nCompanyID + " order by N_Count Desc";
            sqlInvValue = "SELECT vw_InvStock_Status.N_CompanyID, Inv_ItemCategory.X_CategoryCode, Inv_ItemCategory.X_Category,SUM(vw_InvStock_Status.N_Factor*vw_InvStock_Status.N_Cost*vw_InvStock_Status.N_Qty) AS N_Value "
                            + "FROM vw_InvStock_Status INNER JOIN Inv_ItemMaster ON vw_InvStock_Status.N_ItemID = Inv_ItemMaster.N_ItemID AND vw_InvStock_Status.N_CompanyID = Inv_ItemMaster.N_CompanyID INNER JOIN "
                            + "Inv_ItemCategory ON Inv_ItemMaster.N_CategoryID = Inv_ItemCategory.N_CategoryID AND Inv_ItemMaster.N_CategoryID = Inv_ItemCategory.N_CategoryID AND Inv_ItemMaster.N_CompanyID = Inv_ItemCategory.N_CompanyID "
                            + "WHERE vw_InvStock_Status.N_CompanyID=" + nCompanyID + " "
                            + "GROUP BY vw_InvStock_Status.N_CompanyID, Inv_ItemCategory.X_CategoryCode, Inv_ItemCategory.X_Category";

            SortedList Data = new SortedList();
            DataTable AllItem = new DataTable();
            DataTable NoStock = new DataTable();
            DataTable MinQty = new DataTable();
            DataTable ReOrder = new DataTable();
            DataTable TopSell = new DataTable();
            DataTable InvValue = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    AllItem = dLayer.ExecuteDataTable(sqlAll, Params, connection);
                    NoStock = dLayer.ExecuteDataTable(sqlNoStock, Params, connection);
                    MinQty = dLayer.ExecuteDataTable(sqlMinQty, Params, connection);
                    ReOrder = dLayer.ExecuteDataTable(sqlReOrder, Params, connection);
                    TopSell = dLayer.ExecuteDataTable(sqlTopSell, Params, connection);
                    InvValue = dLayer.ExecuteDataTable(sqlInvValue, Params, connection);
                }

                AllItem.AcceptChanges();
                NoStock.AcceptChanges();
                MinQty.AcceptChanges();
                ReOrder.AcceptChanges();
                TopSell.AcceptChanges();
                InvValue.AcceptChanges();

                if (AllItem.Rows.Count > 0) Data.Add("allItemCount", AllItem);
                if (NoStock.Rows.Count > 0) Data.Add("noStocCount", NoStock);
                if (MinQty.Rows.Count > 0) Data.Add("minQtyCount", MinQty);
                if (ReOrder.Rows.Count > 0) Data.Add("reOrderCount", ReOrder);

                if (TopSell.Rows.Count > 0) Data.Add("topSellItems", TopSell);
                if (InvValue.Rows.Count > 0) Data.Add("categoryWiseInvValue", InvValue);

                return Ok(_api.Success(Data));

            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }

        [HttpGet("expiryList")]
        public ActionResult ExpiryList(int nFnYearId,DateTime d_Date, int nPage, int nSizeperpage, string xSearchkey,int nLocationID, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            string sqlCommandCount = "";
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string Searchkey = "";
            Params.Add("@p1", nCompanyID);
            Params.Add("@p2", nFnYearId);
            Params.Add("@p3",nLocationID);
            if (xSearchkey != null && xSearchkey.Trim() != "")
                //Searchkey = "and ( X_ReferenceNo like '%" + xSearchkey + "%' or  D_Date like '%" + xSearchkey + "%'  ) ";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_ItemID desc";
            // xSortBy = " order by batch desc,D_TransDate desc";
            else
                xSortBy = " order by " + xSortBy;
         
                if (Count == 0)
                    sqlCommandText = "select * from Vw_ItemWiseLocation where N_CompanyID="+nCompanyID+" and D_ExpiryDate is not null and D_ExpiryDate <='"+d_Date+"' and  N_ItemID in (Select N_ItemID from Inv_ItemMasterWHLink where N_CompanyID="+nCompanyID+" and N_WarehouseID="+nLocationID+" ) and N_ItemID in (Select N_ItemID from Inv_StockMaster where N_CompanyID="+nCompanyID+" and N_LocationID="+nLocationID+" and D_ExpiryDate<='"+d_Date+"'  ) ";
                else
                    sqlCommandText = "select * from Vw_ItemWiseLocation where N_CompanyID="+nCompanyID+" and D_ExpiryDate is not null and  D_ExpiryDate <='"+d_Date+"' and  N_ItemID in (Select N_ItemID from Inv_ItemMasterWHLink where N_CompanyID="+nCompanyID+" and N_WarehouseID="+nLocationID+" ) and N_ItemID in (Select N_ItemID from Inv_StockMaster where N_CompanyID="+nCompanyID+" and N_LocationID="+nLocationID+" and D_ExpiryDate<='"+d_Date+"'  ) ";

         
            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText + xSortBy, Params, connection);

                    sqlCommandCount = "select count(*) as N_Count from Vw_ItemWiseLocation where N_CompanyID=@p1 and D_ExpiryDate is not null and D_ExpiryDate <='"+d_Date+"' and N_ItemID in (Select N_ItemID from Inv_ItemMasterWHLink where N_CompanyID=@p1 and N_WarehouseID=@p3 ) and N_ItemID in (Select N_ItemID from Inv_StockMaster where N_CompanyID=@p1 and N_LocationID=@p3 and D_ExpiryDate<='"+d_Date+" '  ) ";
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

    }
}