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
using System.Text.RegularExpressions;

namespace SmartxAPI.Controllers

{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("materialDisptach")]
    [ApiController]
    public class Inv_MaterialDispatch : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly IMyAttachments myAttachments;
        private readonly string connectionString;
        private readonly int FormID;
        public Inv_MaterialDispatch(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf, IMyAttachments myAtt)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            myAttachments = myAtt;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 684;
        }

        [HttpGet("list")]
        public ActionResult GetMaterialDispatchList(int nCompanyId, int nFnYearId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_DispatchNo like '%" + xSearchkey + "%' or X_ProjectName like '%" + xSearchkey + "%' or D_DispatchDate like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by X_DispatchNo desc";
            else
            {
                switch (xSortBy.Split(" ")[0])
                {
                    case "X_DispatchNo":
                        xSortBy = "X_DispatchNo " + xSortBy.Split(" ")[1];
                        break;
                    default: break;
                }
                xSortBy = " order by " + xSortBy;
            }

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_MaterialDispatchDisp where N_CompanyID=@p1 and N_FnYearID=@p2 " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_MaterialDispatchDisp where N_CompanyID=@p1 and N_FnYearID=@p2 " + Searchkey + " and N_DispatchId not in (select top(" + Count + ") N_DispatchId from vw_MaterialDispatchDisp where N_CompanyID=@p1 and N_FnYearID=@p2 " + xSortBy + " ) " + xSortBy;
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);
            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(*) as N_Count  from vw_MaterialDispatchDisp where N_CompanyID=@p1 and N_FnYearID=@p2 " + Searchkey + "";
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
                return Ok(_api.Error(User, e));
            }
        }

        [HttpGet("details")]
        public ActionResult GetMaterialDispatchDetails(int nFnYearId,string xDispatchNo,int nLocationID, int nBranchId,bool B_AllBranchData)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            bool B_ProjectExists=true;
            try
            {
                using (SqlConnection Con = new SqlConnection(connectionString))
                {
                    Con.Open();


                    DataSet dsMaterailDispatch = new DataSet();
                    SortedList QueryParamsList = new SortedList();
                    QueryParamsList.Add("@nCompanyID", nCompanyId);
                    QueryParamsList.Add("@nFnYearID", nFnYearId);
                    QueryParamsList.Add("@nBranchId", nBranchId);
                    QueryParamsList.Add("@xDispatchNo", xDispatchNo);
                    QueryParamsList.Add("@nLocationID", nLocationID);

                    string Mastersql = "";

                    if (B_AllBranchData)
                        Mastersql= "Select * From vw_MaterialDispatchDisp Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and X_DispatchNo=@xDispatchNo";
                    else
                        Mastersql= "Select * From vw_MaterialDispatchDisp Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and X_DispatchNo=@xDispatchNo and N_BranchId=@nBranchId";

                    DataTable MasterTable = dLayer.ExecuteDataTable(Mastersql, QueryParamsList, Con);
                    if (MasterTable.Rows.Count == 0) { return Ok(_api.Warning("No data found")); }
                    MasterTable = _api.Format(MasterTable, "Master");
                    int N_DispatchID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_DispatchId"].ToString());
                    QueryParamsList.Add("@N_DispatchID", N_DispatchID);

                    string DetailSql = "";
                  
                    if (B_ProjectExists)
                        DetailSql = "Select *,dbo.SP_BatchStock(vw_MaterialDispatchDetailDisp.N_ItemID,vw_MaterialDispatchDetailDisp.N_LocationID,'',vw_MaterialDispatchDetailDisp.N_ProjectID) as N_stock  from vw_MaterialDispatchDetailDisp  where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_DispatchId=@N_DispatchID";
                    else
                        DetailSql = "Select *,dbo.SP_BatchStock(vw_MaterialDispatchDetailDisp.N_ItemID,@nLocationID,'') as N_stock  from vw_MaterialDispatchDetailDisp  where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and N_DispatchId=@N_DispatchID";

                    DataTable DetailTable = dLayer.ExecuteDataTable(DetailSql, QueryParamsList, Con);

                    DetailTable = _api.Format(DetailTable, "Details");
                    dsMaterailDispatch.Tables.Add(MasterTable);
                    dsMaterailDispatch.Tables.Add(DetailTable);
                    return Ok(_api.Success(dsMaterailDispatch));
                  
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }

        //Save....
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
                    string values = "";
                    MasterTable = ds.Tables["master"];
                    DetailTable = ds.Tables["details"];
                    DataRow MasterRow = MasterTable.Rows[0];
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    bool bDeptEnabled=false;
                    int nDispatchID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_DispatchID"].ToString());
                    int nFnYearID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_FnYearID"].ToString());
                    int N_RSID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_RSID"].ToString());
                    int nSaveDraft = myFunctions.getIntVAL(MasterTable.Rows[0]["B_IsSaveDraft"].ToString());
                    int N_UserID = myFunctions.getIntVAL(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                    string X_DispatchNo = MasterTable.Rows[0]["X_DispatchNo"].ToString();
                    if (nDispatchID > 0)
                    {
                        SortedList DeleteParams = new SortedList(){
                                    {"N_CompanyID",nCompanyID},
                                    {"N_UserID",N_UserID},
                                    {"X_TransType","MATERIAL DISPATCH"},
                                    {"X_SystemName","WebRequest"},
                                    {"N_VoucherID",nDispatchID}};

                        dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_SaleAccounts", DeleteParams, connection, transaction);
                    }
                    values = MasterRow["X_DispatchNo"].ToString();

                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", MasterTable.Rows[0]["n_CompanyId"].ToString());
                        Params.Add("N_YearID", MasterTable.Rows[0]["n_FnYearId"].ToString());
                        Params.Add("N_FormID", this.FormID);
                        //Params.Add("N_BranchID", MasterTable.Rows[0]["n_BranchId"].ToString());
                        X_DispatchNo = dLayer.GetAutoNumber("Inv_MaterialDispatch", "X_DispatchNo", Params, connection, transaction);
                        if (X_DispatchNo == "") { transaction.Rollback(); return Ok(_api.Error(User, "Unable to generate Return Number")); }
                        MasterTable.Rows[0]["X_DispatchNo"] = X_DispatchNo;
                    }

                    nDispatchID = dLayer.SaveData("Inv_MaterialDispatch", "N_DispatchID", MasterTable, connection, transaction);
                    if (nDispatchID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable To Save"));
                    }
                    for (int i = 0; i < DetailTable.Rows.Count; i++)
                    {
                        DetailTable.Rows[i]["N_DispatchID"] = nDispatchID;
                    }
                    int N_DispatchDetailsID = dLayer.SaveData("Inv_MaterialDispatchDetails", "N_DispatchDetailsID", DetailTable, connection, transaction);
                    if (N_DispatchDetailsID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable To Save"));
                    }
                    if(nSaveDraft==0)
                        dLayer.ExecuteScalar("update Inv_PRS set N_Processed=1 where N_PRSID=" + N_RSID + " and N_CompanyID=" + nCompanyID+ " and N_FnYearID=" + nFnYearID + " and N_TransTypeID=8", connection, transaction);

                   
                    SortedList UpdateStockParam = new SortedList();
                    UpdateStockParam.Add("N_CompanyID", nCompanyID);
                    UpdateStockParam.Add("N_DispatchId", nDispatchID);
                    UpdateStockParam.Add("N_UserID", N_UserID);

                    if (!bDeptEnabled)
                        dLayer.ExecuteNonQueryPro("SP_Inv_MaterialDispatch", UpdateStockParam, connection, transaction);
                    else
                        dLayer.ExecuteNonQueryPro("SP_Inv_MaterialDispatch_Department", UpdateStockParam, connection, transaction);

                    SortedList PostParam = new SortedList();
                    PostParam.Add("N_CompanyID", nCompanyID);
                    PostParam.Add("X_InventoryMode", nDispatchID);
                    PostParam.Add("N_UserID", N_UserID);

                    if (!bDeptEnabled)
                        dLayer.ExecuteNonQueryPro("SP_Acc_Inventory_Sales_Posting", PostParam, connection, transaction);

                    transaction.Commit();
                    return Ok(_api.Success("Saved"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }
        }

        //Delete....
        [HttpDelete("delete")]
        public ActionResult DeleteData(int nDispatchID, int N_RSID, int nCompanyID, int nFnYearID, int nBranchID)
        {
            int Results = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    var nUserID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",nCompanyID},
                                {"N_UserID",nUserID},
                                {"X_TransType","MATERIAL DISPATCH"},
                                {"X_SystemName","WebRequest"},
                                {"N_VoucherID",nDispatchID}};

                    Results = dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_SaleAccounts", DeleteParams, connection, transaction);
                    if (Results <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to delete Material Dispatch."));
                    }
                    else
                    {
                        if(N_RSID>0)
                            dLayer.ExecuteScalar("update Inv_PRS set N_Processed=0 where N_PRSID=" + N_RSID + " and N_CompanyID=" + nCompanyID+ " and N_FnYearID=" + nFnYearID, connection, transaction);
                    }
                    transaction.Commit();
                    return Ok(_api.Success("Material Dispatch deleted"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }


        }

    }
}