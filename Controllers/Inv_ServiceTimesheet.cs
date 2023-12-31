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
     [Route("serviceTimesheet")]
     [ApiController]
    public class InvServiceTimesheet : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions api;
        private readonly string connectionString;
        private readonly IMyFunctions myFunctions;
        private readonly IApiFunctions _api;
        // private readonly int N_FormID = 1145;
        public InvServiceTimesheet(IDataAccessLayer dl,IMyFunctions myFun, IApiFunctions apiFun, IConfiguration conf)
        {
            dLayer = dl;
            api = apiFun;
            _api = api;
            myFunctions=myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }

        [HttpGet("dashboardList")]
        public ActionResult DashboardList(int nFnYearID,int nFormID, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

            if(nFormID==1145)
            {
                if (xSearchkey != null && xSearchkey.Trim() != "")
                    Searchkey = "and (X_ServiceSheetCode like '%" + xSearchkey + "%' or X_OrderNo like '%" + xSearchkey + "%' or X_ProjectName like '%" + xSearchkey + "%' or X_CustomerName like '%" + xSearchkey + "%' or D_Entrydate like '%" + xSearchkey + "%')";

                if (xSortBy == null || xSortBy.Trim() == "")
                    xSortBy = " order by X_ServiceSheetCode desc";
                else
                {
                    switch (xSortBy.Split(" ")[0])
                    {
                        case "X_ServiceSheetCode":
                            xSortBy = "X_ServiceSheetCode " + xSortBy.Split(" ")[1];
                            break;
                        default: break;
                    }
                    xSortBy = " order by " + xSortBy;
                }

                if (Count == 0)
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_ServiceTimesheet where N_CompanyID=@nCompanyId and N_FormID=@nFormID " + Searchkey + " " + xSortBy;
                else
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_ServiceSheetMaster where N_CompanyID=@nCompanyId and N_FormID=@nFormID " + Searchkey + " and N_ServiceSheetID not in (select top(" + Count + ") N_ServiceSheetID from vw_Inv_ServiceSheetMaster where N_CompanyID=@nCompanyId and N_FormID=@nFormID " + xSortBy + " ) " + " " + xSortBy;
            }
            else
            {
                if (xSearchkey != null && xSearchkey.Trim() != "")
                    Searchkey = "and (X_ServiceSheetCode like '%" + xSearchkey + "%' or X_POrderNo like '%" + xSearchkey + "%' or X_ProjectName like '%" + xSearchkey + "%' or X_VendorName like '%" + xSearchkey + "%' or D_Invoicedate like '%" + xSearchkey + "%')";

                if (xSortBy == null || xSortBy.Trim() == "")
                    xSortBy = " order by X_ServiceSheetCode desc";
                else
                {
                    switch (xSortBy.Split(" ")[0])
                    {
                        case "X_ServiceSheetCode":
                            xSortBy = "X_ServiceSheetCode " + xSortBy.Split(" ")[1];
                            break;
                        default: break;
                    }
                    xSortBy = " order by " + xSortBy;
                }

                if (Count == 0)
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_ServiceTimesheet where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_FormID=@nFormID " + Searchkey + " " + xSortBy;
                else
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_ServiceTimesheet where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_FormID=@nFormID " + Searchkey + " and N_ServiceSheetID not in (select top(" + Count + ") N_ServiceSheetID from vw_Inv_ServiceTimesheet where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_FormID=@nFormID " + xSortBy + " ) " + " " + xSortBy;
            }
            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnYearID);
            Params.Add("@nFormID", nFormID);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    SortedList OutPut = new SortedList();

                    // if(nFormID==1145)
                        sqlCommandCount = "select count(1) as N_Count  from vw_Inv_ServiceTimesheet where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_FormID=@nFormID " + Searchkey + "";
                    // else
                    //     sqlCommandCount = "select count(1) as N_Count  from vw_Inv_VendorServiceSheetMaster where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_FormID=@nFormID " + Searchkey + "";

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
                    DataTable ItemTable;
                    DataTable DetailTable;
                    MasterTable = ds.Tables["master"];
                    ItemTable = ds.Tables["items"];
                    DetailTable = ds.Tables["details"];
                    SortedList Params = new SortedList();

                    int nServiceSheetID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_ServiceSheetID"].ToString());
                    int nFnYearID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_FnYearID"].ToString());
                    int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyID"].ToString());
                    int nFormID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_FormID"].ToString());
                    string xServiceSheetCode = MasterTable.Rows[0]["x_ServiceSheetCode"].ToString();
                    int nServiceSheetItemID = 0;

                    if (nServiceSheetID > 0)
                    {
                        object serviceSheetID = dLayer.ExecuteScalar("select isNull(N_ServiceSheetID,0) from Inv_Sales where N_CompanyId="+ nCompanyID +" and N_FormID=1601 and N_ServiceSheetID="+nServiceSheetID, Params, connection, transaction);
                        if (serviceSheetID == null) serviceSheetID = 0;
                        if (myFunctions.getIntVAL(serviceSheetID.ToString()) > 0)
                        {
                            transaction.Rollback();
                            return Ok(_api.Error(User,"Unable to save,Invoice Processed"));
                        }

                        dLayer.DeleteData("Inv_ServiceTimesheet", "N_ServiceSheetID", nServiceSheetID, "N_CompanyID =" + nCompanyID, connection, transaction);
                        dLayer.DeleteData("Inv_ServiceTimesheetItems", "N_ServiceSheetID", nServiceSheetID, "N_CompanyID =" + nCompanyID, connection, transaction);
                        dLayer.DeleteData("Inv_ServiceTimesheetDetails", "N_ServiceSheetID", nServiceSheetID, "N_CompanyID =" + nCompanyID, connection, transaction);
                    }

                    if (xServiceSheetCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", nFormID);
                        xServiceSheetCode = dLayer.GetAutoNumber("Inv_ServiceTimesheet", "X_ServiceSheetCode", Params, connection, transaction);
                        if (xServiceSheetCode == "")
                        {
                            transaction.Rollback();
                            return Ok("Unable to generate Service Timesheet");
                        }
                        MasterTable.Rows[0]["X_ServiceSheetCode"] = xServiceSheetCode;
                    }

                    nServiceSheetID = dLayer.SaveData("Inv_ServiceTimesheet", "N_ServiceSheetID", "", "", MasterTable, connection, transaction);
                    if (nServiceSheetID <= 0)
                    {
                        transaction.Rollback();
                        return Ok("Unable to save Service Timesheet!");
                    }

                    for (int j = 0; j < ItemTable.Rows.Count; j++)
                    {
                        ItemTable.Rows[j]["N_ServiceSheetID"] = nServiceSheetID;

                        nServiceSheetItemID = dLayer.SaveDataWithIndex("Inv_ServiceTimesheetItems", "N_ServiceSheetItemID", "", "", j, ItemTable, connection, transaction);
                        if (nServiceSheetItemID <= 0)
                        {
                            transaction.Rollback();
                            return Ok(_api.Error(User, "Unable to save"));
                        }

                        for (int i = 0; i < DetailTable.Rows.Count; i++)
                        {
                            if (nFormID == 1145) {
                                if (DetailTable.Rows[i]["N_TransDetailID"].ToString() == ItemTable.Rows[j]["N_DeliverNoteDetailsID"].ToString())
                                {
                                    DetailTable.Rows[i]["N_ServiceSheetID"] = nServiceSheetID;
                                    DetailTable.Rows[i]["N_ServiceSheetItemID"] = nServiceSheetItemID;
                                }
                            } else {
                                if (DetailTable.Rows[i]["N_TransDetailID"].ToString() == ItemTable.Rows[j]["N_MRNDetailID"].ToString())
                                {
                                    DetailTable.Rows[i]["N_ServiceSheetID"] = nServiceSheetID;
                                    DetailTable.Rows[i]["N_ServiceSheetItemID"] = nServiceSheetItemID;
                                }
                            };
                        }
                    }

                    if (DetailTable.Rows.Count > 0)
                    {
                        int nServiceSheetDetailsID = dLayer.SaveData("Inv_ServiceTimesheetDetails", "N_ServiceSheetDetailsID", DetailTable, connection, transaction);
                        if (nServiceSheetDetailsID <= 0)
                        {
                            transaction.Rollback();
                            return Ok("Unable to save Service Timesheet!");
                        }
                    }
                   
                    transaction.Commit();
                    SortedList Result = new SortedList();
                    Result.Add("N_ServiceSheetID", nServiceSheetID);
                    Result.Add("X_ServiceSheetCode", xServiceSheetCode);

                     if (nFormID == 1145)
                        return Ok(_api.Success(Result, "Customer Service Timesheet Saved Successfully!"));
                    else 
                        return Ok(_api.Success(Result, "Vendor Service Timesheet Saved Successfully!"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

        [HttpGet("details")]
        public ActionResult ServiceSheetDetails(int nFormID,string xServiceSheetCode, int nLocationID, string xType)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataSet dt = new DataSet();
                    SortedList Params = new SortedList();
                    SortedList ProcParams = new SortedList();
                    DataTable MasterTable = new DataTable();
                    DataTable ItemTable = new DataTable();
                    DataTable DetailTable = new DataTable();
                    DataTable ProcTable = new DataTable();
                    SortedList DetailParams = new SortedList();
                 

                    string Mastersql = "";
                    string Itemsql = "";
                    string DetailSql = "";

                    

                    Params.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    Params.Add("@xServiceSheetCode", xServiceSheetCode);
                    Mastersql = "select * from vw_Inv_ServiceTimesheet where N_CompanyID=@nCompanyID and X_ServiceSheetCode=@xServiceSheetCode";
                   
                    MasterTable = dLayer.ExecuteDataTable(Mastersql, Params, connection);
                    if (MasterTable.Rows.Count == 0) { return Ok(_api.Warning("No data found")); }
                    int nServiceSheetID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_ServiceSheetID"].ToString());
                    Params.Add("@nServiceSheetID", nServiceSheetID);

                    MasterTable = _api.Format(MasterTable, "Master");

                    Itemsql = "select * from vw_Inv_ServiceTimesheetItems where N_CompanyID=@nCompanyID and N_ServiceSheetID=@nServiceSheetID";
                    ItemTable = dLayer.ExecuteDataTable(Itemsql, Params, connection);
                    ItemTable = _api.Format(ItemTable, "Items");

                    ProcParams.Add("@N_TransID", myFunctions.getIntVAL(MasterTable.Rows[0]["N_SOID"].ToString()));
                    ProcParams.Add("@N_CompanyId", myFunctions.GetCompanyID(User));
                    ProcParams.Add("@N_FnyearID", myFunctions.getIntVAL(MasterTable.Rows[0]["N_FnYearID"].ToString()));
                    ProcParams.Add("@N_BranchID", myFunctions.getIntVAL(MasterTable.Rows[0]["N_BranchID"].ToString()));
                                                 
                    ///ProcParams.Add("@D_DateFrom", DateTime.ParseExact(MasterTable.Rows[0]["D_DateFrom"].ToString(), "yyyy-MM-dd hh:mm:ss ",System.Globalization.CultureInfo.InvariantCulture));
                   // ProcParams.Add("@D_DateTo",  DateTime.ParseExact(MasterTable.Rows[0]["D_DateTo"].ToString(), "yyyy-MM-dd hh:mm:ss",System.Globalization.CultureInfo.InvariantCulture));

                                                     
                    ProcParams.Add("@D_DateFrom", MasterTable.Rows[0]["D_DateFrom"].ToString());
                    ProcParams.Add("@D_DateTo", MasterTable.Rows[0]["D_DateTo"].ToString());

                    ProcParams.Add("@N_LocationID", nLocationID);
                    ProcParams.Add("@N_UserID", myFunctions.GetUserID(User));
                    ProcParams.Add("@X_Type", xType);
                    ProcParams.Add("@N_FormID", myFunctions.getIntVAL(MasterTable.Rows[0]["N_FormID"].ToString()));
                    ProcTable = dLayer.ExecuteDataTablePro("SP_InvItemWiseDateList", ProcParams, connection);

                    DetailSql = "select * from vw_Inv_ServiceTimesheetDetails where N_CompanyId=@nCompanyID and N_ServiceSheetID=@nServiceSheetID";
                    DetailTable = dLayer.ExecuteDataTable(DetailSql, Params, connection);

                    foreach (DataRow Avar in ProcTable.Rows)
                    {
                        foreach (DataRow Kvar in DetailTable.Rows)
                        {
                            // if (myFunctions.getIntVAL(Avar["N_TransDetailsID"].ToString()) == myFunctions.getIntVAL(Kvar["N_TransDetailID"].ToString()) &&
                            //     myFunctions.getIntVAL(Avar["N_ItemID"].ToString()) == myFunctions.getIntVAL(Kvar["N_ItemID"].ToString()) &&
                            //     Avar["DateValue"].ToString() == Kvar["D_Date"].ToString())
                            // {
                            //     Kvar["N_Hour"] = Avar["N_Hours"];
                            //     Kvar["X_Remarks"] = Avar["X_Remarks"];
                            // }
                        }
                    }

                    Object RSCount = "";
                    if (nFormID==1145)
                        RSCount=dLayer.ExecuteScalar("select count(*) from Inv_Sales where N_ServiceSheetID="+nServiceSheetID+" and N_CompanyID="+ myFunctions.GetCompanyID(User), Params, connection);
                    else
                        RSCount=dLayer.ExecuteScalar("select count(*) from Inv_Purchase where N_ServiceSheetID="+nServiceSheetID+" and N_CompanyID="+ myFunctions.GetCompanyID(User), Params, connection);
                    
                    int nRSCount = myFunctions.getIntVAL(RSCount.ToString());
                    if (nRSCount > 0)
                    {
                        if (MasterTable.Rows.Count > 0)
                        {
                            MasterTable.Columns.Add("b_RSProcessed");
                            MasterTable.Rows[0]["b_RSProcessed"]=true;
                        }
                    }    
                    DetailTable = _api.Format(DetailTable, "Details");

                    dt.Tables.Add(MasterTable);
                    dt.Tables.Add(ItemTable);
                    dt.Tables.Add(DetailTable);
                    return Ok(_api.Success(dt));
                }

            }
            catch (Exception e)
            {
                return Ok(_api.Error(User,e));
            }
        }

        [HttpDelete("delete")]
        public ActionResult DeleteData(int nServiceSheetID, int nFnYearID, int nFormID)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            int Results = 0;
            try
            {
                SortedList QueryParams = new SortedList();
                QueryParams.Add("@nCompanyID", nCompanyID);
                QueryParams.Add("@nFnYearID", nFnYearID);
                QueryParams.Add("@nServiceSheetID", nServiceSheetID);
                QueryParams.Add("@nFormID", nFormID);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    object serviceSheetID = dLayer.ExecuteScalar("select isNull(N_ServiceSheetID,0) from Inv_Sales where N_CompanyId=@nCompanyID and N_FormID=@nFormID and N_ServiceSheetID="+nServiceSheetID, QueryParams, connection);
                    if (serviceSheetID == null)
                        serviceSheetID = 0;

                    if (myFunctions.getIntVAL(serviceSheetID.ToString()) != 0)
                    {
                        return Ok(_api.Error(User,"Unable to delete,Invoice Processed"));
                    } else {
                        Results = dLayer.DeleteData("Inv_ServiceTimesheet", "N_ServiceSheetID", nServiceSheetID, "N_CompanyID =" + nCompanyID + " and N_FnYearID=" + nFnYearID, connection);

                        if (Results > 0)
                        {
                            dLayer.DeleteData("Inv_ServiceTimesheetItems", "N_ServiceSheetID", nServiceSheetID, "N_CompanyID =" + nCompanyID, connection);
                            dLayer.DeleteData("Inv_ServiceTimesheetDetails", "N_ServiceSheetID", nServiceSheetID, "N_CompanyID =" + nCompanyID, connection);
                        
                            if(nFormID==1145) {
                                return Ok(_api.Success("Customer Service Timesheet Deleted"));
                            }
                            else {
                                return Ok(_api.Success("Vendor Service Timesheet Deleted"));
                            }
                            
                        }
                        else
                        {
                            return Ok(_api.Error(User,"Unable to delete"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

        [HttpGet("orderitemdetails")]
        public ActionResult GetOrderItemDetails(int nFnYearID, int nFormID, int nBranchID, int nLocationID, DateTime dDateFrom, DateTime dDateTo, int nSOID, string xType, bool enableDayWise)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataSet dt = new DataSet();
                    SortedList Params = new SortedList();
                    DataTable ItemTable = new DataTable();
                    DataTable DetailTable = new DataTable();
                    string Itemsql = "";

                    Params.Add("@N_TransID", nSOID);
                    Params.Add("@N_CompanyId", myFunctions.GetCompanyID(User));
                    Params.Add("@N_FnyearID", nFnYearID);
                    Params.Add("@N_BranchID", nBranchID);
                    Params.Add("@D_DateFrom", dDateFrom);
                    Params.Add("@D_DateTo", dDateTo);
                    Params.Add("@N_LocationID", nLocationID);
                    Params.Add("@N_UserID", myFunctions.GetUserID(User));
                    Params.Add("@X_Type", xType);
                    Params.Add("@N_FormID", nFormID);
                    
                    if (nFormID == 1145)
                        Itemsql = "select * from vw_SOItemsForTimesheet where N_CompanyID=@N_CompanyId and N_SalesOrderId=@N_TransID and ((D_DeliveryDate<=@D_DateFrom) or (D_DeliveryDate>=@D_DateFrom AND D_DeliveryDate<=@D_DateTo)) AND ISNULL(D_ReturnDate,@D_DateFrom)>=@D_DateFrom "+
                        "GROUP BY N_CompanyID, N_ItemID, N_MainItemID, X_ItemCode, X_ItemName,N_DeliverNoteID,X_ReceiptNo,D_DeliveryDate,N_SalesOrderId,X_OrderNo,D_OrderDate,N_CustomerID,X_CustomerCode,X_CustomerName,N_ProjectID,X_ProjectName,N_DeliveryNoteRtnID,X_ReturnNo,D_ReturnDate,N_DeliverNoteDetailsID,"+
		                "N_SOdetailsID,N_RentalUnitID,X_RentalUnit,N_Duration,N_Price,X_FreeDescription,N_Qty,N_DeliveryNoteRtnDtlsID";
                    else {
                        if (xType == "SO")
                            Itemsql = "select * from vw_POItemsForTimesheet where N_CompanyID=@N_CompanyId and N_SOId=@N_TransID and ((D_MRNDate<=@D_DateFrom) or (D_MRNDate>=@D_DateFrom AND D_MRNDate<=@D_DateTo)) AND ISNULL(D_ReturnDate,@D_DateFrom)>=@D_DateFrom";
                        else
                        {
                            if (enableDayWise)
                                Itemsql = "select * from vw_POItemsForTimesheetDayWise where N_CompanyID=@N_CompanyId and N_POrderID=@N_TransID and ((D_MRNDate<=@D_DateFrom) or (D_MRNDate>=@D_DateFrom AND D_MRNDate<=@D_DateTo)) AND ISNULL(D_ReturnDate,@D_DateFrom)>=@D_DateFrom";
                            else
                                Itemsql = "select * from vw_POItemsForTimesheet where N_CompanyID=@N_CompanyId and N_POrderID=@N_TransID and ((D_MRNDate<=@D_DateFrom) or (D_MRNDate>=@D_DateFrom AND D_MRNDate<=@D_DateTo)) AND ISNULL(D_ReturnDate,@D_DateFrom)>=@D_DateFrom";
                        }
                    }

                    ItemTable = dLayer.ExecuteDataTable(Itemsql, Params, connection);
                    ItemTable = _api.Format(ItemTable, "Items");

                    DetailTable = dLayer.ExecuteDataTablePro("SP_InvItemWiseDateList", Params, connection);

                    DetailTable = _api.Format(DetailTable, "Details");
                    dt.Tables.Add(ItemTable);
                    dt.Tables.Add(DetailTable);
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User,e));
            }
        }

        [HttpGet("multirentalPO")]
        public ActionResult GetMultiRentalPOList(int nFnYearID, int nFormID, int nVendorID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            Params.Add("@N_CompanyID", myFunctions.GetCompanyID(User));
            Params.Add("@N_FnyearID", nFnYearID);
            Params.Add("@N_FormID", nFormID);
            Params.Add("@N_VendorID", nVendorID);


            string sqlCommandText = "";
            sqlCommandText = "select * from vw_Inv_ServiceTimesheet where N_CompanyID=@N_CompanyID and N_FormID=@N_FormID and N_VendorID=@N_VendorID and N_ServiceSheetID not in (select isNull(N_ServiceSheetID, 0) from Inv_PurchaseDetails where N_CompanyID=@N_CompanyID) order by x_ServiceSheetCode desc";

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
                return Ok(_api.Error(User, e));
            }

        }

        [HttpGet("multiJobOrder")]
        public ActionResult GetMultiJobOrderList(int nFnYearID, int nFormID, int nCustomerID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            Params.Add("@N_CompanyID", myFunctions.GetCompanyID(User));
            Params.Add("@N_FnyearID", nFnYearID);
            Params.Add("@N_FormID", nFormID);
            Params.Add("@N_CustomerID", nCustomerID);


            string sqlCommandText = "";
            sqlCommandText = "select * from vw_Inv_ServiceTimesheet where N_CompanyID=@N_CompanyID and N_FormID=@N_FormID and N_CustomerID=@N_CustomerID and N_ServiceSheetID not in (select isNull(N_ServiceSheetID, 0) from Inv_SalesDetails where N_CompanyID=@N_CompanyID) order by x_ServiceSheetCode desc";

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
                return Ok(_api.Error(User, e));
            }

        }

        [HttpGet("combinedVendorList")]
        public ActionResult GetCombinedVendorList(int nProjectID, int nFormID, DateTime dDateFrom, DateTime dDateTo)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "";
            Params.Add("@N_CompanyID", myFunctions.GetCompanyID(User));
            Params.Add("@N_ProjectID", nProjectID);
            Params.Add("@N_FormID", nFormID);
            Params.Add("@D_DateFrom", dDateFrom);
            Params.Add("@D_DateTo", dDateTo);

            if (nProjectID > 0)
                sqlCommandText = "select N_CompanyID, N_VendorID, X_VendorName, X_VendorName_Ar, X_VendorCode, N_CurrencyID, N_ExchangeRate from vw_PurchaseOrderReceive where N_CompanyID=@N_CompanyID and N_ProjectID=@N_ProjectID and ((D_ReceiveDate<=@D_DateFrom) or (D_ReceiveDate>=@D_DateFrom AND D_ReceiveDate<=@D_DateTo)) AND ISNULL(D_ReceiveReturnDate,@D_DateFrom)>=@D_DateFrom and N_POrderID NOT IN (select N_POID from Inv_ServiceTimesheet where N_FormID=@N_FormID and N_CompanyID=@N_CompanyID and D_DateFrom=@D_DateFrom and D_DateTo=@D_DateTo) GROUP BY N_CompanyID, N_VendorID, X_VendorName, X_VendorName_Ar, X_VendorCode, N_CurrencyID, N_ExchangeRate";
            else
                sqlCommandText = "select N_CompanyID, N_VendorID, X_VendorName, X_VendorName_Ar, X_VendorCode, N_CurrencyID, N_ExchangeRate from vw_PurchaseOrderReceive where N_CompanyID=@N_CompanyID and ((D_ReceiveDate<=@D_DateFrom) or (D_ReceiveDate>=@D_DateFrom AND D_ReceiveDate<=@D_DateTo)) AND ISNULL(D_ReceiveReturnDate,@D_DateFrom)>=@D_DateFrom and N_POrderID NOT IN (select N_POID from Inv_ServiceTimesheet where N_FormID=@N_FormID and N_CompanyID=@N_CompanyID and D_DateFrom=@D_DateFrom and D_DateTo=@D_DateTo) GROUP BY N_CompanyID, N_VendorID, X_VendorName, X_VendorName_Ar, X_VendorCode, N_CurrencyID, N_ExchangeRate";

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
                return Ok(_api.Error(User, e));
            }
        }

        [HttpGet("combinedProjectList")]
        public ActionResult GetCombinedProjectList(int nPartyID, int nFormID, DateTime dDateFrom, DateTime dDateTo)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "";
            Params.Add("@N_CompanyID", myFunctions.GetCompanyID(User));
            Params.Add("@N_PartyID", nPartyID);
            Params.Add("@N_FormID", nFormID);
            Params.Add("@D_DateFrom", dDateFrom);
            Params.Add("@D_DateTo", dDateTo);
            
            if (nFormID == 1147)
            {
                if (nPartyID > 0)
                    sqlCommandText = "select N_CompanyID, N_ProjectID, X_ProjectName, X_ProjectCode from vw_PurchaseOrderReceive where N_CompanyID=@N_CompanyID and ISNULL(N_ProjectID, 0)>0 and N_VendorID=@N_PartyID and ((D_ReceiveDate<=@D_DateFrom) or (D_ReceiveDate>=@D_DateFrom AND D_ReceiveDate<=@D_DateTo)) AND ISNULL(D_ReceiveReturnDate,@D_DateFrom)>=@D_DateFrom and N_POrderID NOT IN (select N_POID from Inv_ServiceTimesheet where N_FormID=@N_FormID and N_CompanyID=@N_CompanyID and D_DateFrom=@D_DateFrom and D_DateTo=@D_DateTo) GROUP BY N_CompanyID, N_ProjectID, X_ProjectName, X_ProjectCode";
                else
                    sqlCommandText = "select N_CompanyID, N_ProjectID, X_ProjectName, X_ProjectCode from vw_PurchaseOrderReceive where N_CompanyID=@N_CompanyID and ISNULL(N_ProjectID, 0)>0 and ((D_ReceiveDate<=@D_DateFrom) or (D_ReceiveDate>=@D_DateFrom AND D_ReceiveDate<=@D_DateTo)) AND ISNULL(D_ReceiveReturnDate,@D_DateFrom)>=@D_DateFrom and N_POrderID NOT IN (select N_POID from Inv_ServiceTimesheet where N_FormID=@N_FormID and N_CompanyID=@N_CompanyID and D_DateFrom=@D_DateFrom and D_DateTo=@D_DateTo) GROUP BY N_CompanyID, N_ProjectID, X_ProjectName, X_ProjectCode";
            }
            else
            {
                if (nPartyID > 0)
                    sqlCommandText = "select N_CompanyID, N_ProjectID, X_ProjectName, X_ProjectCode from vw_SalesorderDelivery where N_CompanyID=@N_CompanyID and ISNULL(N_ProjectID, 0)>0 and N_CustomerID=@N_PartyID and ((D_DeliveryDate<=@D_DateFrom) or (D_DeliveryDate>=@D_DateFrom AND D_DeliveryDate<=@D_DateTo)) AND ISNULL(D_DeliveryReturnDate,@D_DateFrom)>=@D_DateFrom and N_SalesOrderId NOT IN (select N_SOID from Inv_ServiceTimesheet where N_FormID=@N_FormID and N_CompanyID=@N_CompanyID and D_DateFrom=@D_DateFrom and D_DateTo=@D_DateTo) GROUP BY N_CompanyID, N_ProjectID, X_ProjectName, X_ProjectCode";
                else
                    sqlCommandText = "select N_CompanyID, N_ProjectID, X_ProjectName, X_ProjectCode from vw_SalesorderDelivery where N_CompanyID=@N_CompanyID and ISNULL(N_ProjectID, 0)>0 and ((D_DeliveryDate<=@D_DateFrom) or (D_DeliveryDate>=@D_DateFrom AND D_DeliveryDate<=@D_DateTo)) AND ISNULL(D_DeliveryReturnDate,@D_DateFrom)>=@D_DateFrom and N_SalesOrderId NOT IN (select N_SOID from Inv_ServiceTimesheet where N_FormID=@N_FormID and N_CompanyID=@N_CompanyID and D_DateFrom=@D_DateFrom and D_DateTo=@D_DateTo) GROUP BY N_CompanyID, N_ProjectID, X_ProjectName, X_ProjectCode";
            }

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
                return Ok(_api.Error(User, e));
            }
        }

        [HttpGet("combinedCustomerList")]
        public ActionResult GetCombinedCustomerList(int nProjectID, int nFormID, DateTime dDateFrom, DateTime dDateTo)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "";
            Params.Add("@N_CompanyID", myFunctions.GetCompanyID(User));
            Params.Add("@N_ProjectID", nProjectID);
            Params.Add("@N_FormID", nFormID);
            Params.Add("@D_DateFrom", dDateFrom);
            Params.Add("@D_DateTo", dDateTo);

            if (nProjectID > 0)
                sqlCommandText = "select N_CompanyID, N_CustomerID, X_CustomerName, X_CustomerName_Ar, X_CustomerCode, N_CurrencyID, N_ExchangeRate from vw_SalesorderDelivery where N_CompanyID=@N_CompanyID and N_ProjectID=@N_ProjectID and ((D_DeliveryDate<=@D_DateFrom) or (D_DeliveryDate>=@D_DateFrom AND D_DeliveryDate<=@D_DateTo)) AND ISNULL(D_DeliveryReturnDate,@D_DateFrom)>=@D_DateFrom and N_SalesOrderId NOT IN (select N_SOID from Inv_ServiceTimesheet where N_FormID=@N_FormID and N_CompanyID=@N_CompanyID and D_DateFrom=@D_DateFrom and D_DateTo=@D_DateTo) GROUP BY N_CompanyID, N_CustomerID, X_CustomerName, X_CustomerName_Ar, X_CustomerCode, N_CurrencyID, N_ExchangeRate";
            else
                sqlCommandText = "select N_CompanyID, N_CustomerID, X_CustomerName, X_CustomerName_Ar, X_CustomerCode, N_CurrencyID, N_ExchangeRate from vw_SalesorderDelivery where N_CompanyID=@N_CompanyID and ((D_DeliveryDate<=@D_DateFrom) or (D_DeliveryDate>=@D_DateFrom AND D_DeliveryDate<=@D_DateTo)) AND ISNULL(D_DeliveryReturnDate,@D_DateFrom)>=@D_DateFrom and N_SalesOrderId NOT IN (select N_SOID from Inv_ServiceTimesheet where N_FormID=@N_FormID and N_CompanyID=@N_CompanyID and D_DateFrom=@D_DateFrom and D_DateTo=@D_DateTo) GROUP BY N_CompanyID, N_CustomerID, X_CustomerName, X_CustomerName_Ar, X_CustomerCode, N_CurrencyID, N_ExchangeRate";

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
                return Ok(_api.Error(User, e));
            }
        }
    }
}
    