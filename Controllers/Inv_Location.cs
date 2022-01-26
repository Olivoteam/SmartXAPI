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
    [Route("location")]
    [ApiController]
    public class Inv_Location : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        public Inv_Location(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }
        [HttpGet("list")]
        public ActionResult GetLocationDetails(int? nCompanyId, string prs, bool bLocationRequired, bool bAllBranchData, int nBranchID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "";
            if (prs == null || prs == "")
                sqlCommandText = "select * from vw_InvLocation_Disp where N_CompanyID=@p1 order by N_LocationID DESC";
            else
            {
                if (!bLocationRequired)
                {
                    if (bAllBranchData == true)
                        sqlCommandText = "select [Location Name] as x_LocationName,* from vw_InvLocation_Disp where N_MainLocationID =0 and N_CompanyID=" + nCompanyId;

                    else
                        sqlCommandText = "select [Location Name] as x_LocationName,* from vw_InvLocation_Disp where  N_MainLocationID =0 and N_CompanyID=" + nCompanyId + " and  N_BranchID=" + nBranchID;

                }
                else
                {
                    sqlCommandText = "select [Location Name] as x_LocationName,* from vw_InvLocation_Disp where  isnull(N_MainLocationID,0) =0 and N_CompanyID=" + nCompanyId + " and  N_BranchID=" + nBranchID;
                }
            }

            Params.Add("@p1", nCompanyId);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Warning("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(_api.Format(dt)));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }


        [HttpGet("listdetails")]
        public ActionResult GetLocationDetails(int? nCompanyId, int? nLocationId)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "select * from vw_InvLocation where N_CompanyID=@p1 and N_LocationID=@p2 order by N_LocationID DESC";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nLocationId);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Warning("No Results Found"));
                }
                else
                {
                    return Ok(_api.Success(_api.Format(dt)));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }


        [HttpPost("change")]
        public ActionResult ChangeData([FromBody] DataSet ds)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable MasterTable;
                    MasterTable = ds.Tables["master"];
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyID"].ToString());
                    int nLocationID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_LocationID"].ToString());
                    Params.Add("@nCompanyID", nCompanyID);
                    Params.Add("@nLocationID", nLocationID);

                    dLayer.ExecuteNonQuery("update Inv_Location set B_IsCurrent=0 where N_CompanyID=@nCompanyID", Params, connection);
                    dLayer.ExecuteNonQuery("update Inv_Location set B_IsCurrent=1 where N_LocationID=@nLocationID and N_CompanyID=@nCompanyID", Params, connection);

                    return Ok(_api.Success("Location Changed"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
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
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    SortedList Params = new SortedList();
                    SortedList ValidateParams = new SortedList();
                    // Auto Gen
                    string LocationCode = "";
                    //Limit Validation
                    ValidateParams.Add("@N_CompanyID", MasterTable.Rows[0]["n_CompanyId"].ToString());
                    object LocationCount = dLayer.ExecuteScalar("select count(N_LocationID)  from Inv_Location where N_CompanyID=@N_CompanyID", ValidateParams, connection, transaction);
                    object limit = dLayer.ExecuteScalar("select N_LocationLimit from Acc_Company where N_CompanyID=@N_CompanyID", ValidateParams, connection, transaction);
                    bool b_TransferProducts = false;
                    int n_LocationFromID = 0;
                    if (LocationCount != null && limit != null)
                    {
                        if (myFunctions.getIntVAL(LocationCount.ToString()) >= myFunctions.getIntVAL(limit.ToString()))
                        {
                            transaction.Rollback();
                            return Ok(_api.Error(User, "Location Limit exceeded!!!"));
                        }
                    }
                    if (MasterTable.Columns.Contains("b_TransferProducts"))
                    {
                        b_TransferProducts = myFunctions.getBoolVAL(MasterTable.Rows[0]["b_TransferProducts"].ToString());
                        MasterTable.Columns.Remove("b_TransferProducts");
                    }

                    if (MasterTable.Columns.Contains("n_LocationFromID"))
                    {
                        n_LocationFromID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_LocationFromID"].ToString());
                        MasterTable.Columns.Remove("n_LocationFromID");
                    }


                    var values = MasterTable.Rows[0]["X_LocationCode"].ToString();
                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", MasterTable.Rows[0]["n_CompanyId"].ToString());
                        Params.Add("N_YearID", MasterTable.Rows[0]["n_FnYearId"].ToString());
                        Params.Add("N_FormID", 450);
                        Params.Add("N_BranchID", MasterTable.Rows[0]["n_BranchId"].ToString());
                        LocationCode = dLayer.GetAutoNumber("Inv_Location", "X_LocationCode", Params, connection, transaction);
                        if (LocationCode == "") { transaction.Rollback(); return Ok(_api.Error(User, "Unable to generate Location Code")); }
                        MasterTable.Rows[0]["X_LocationCode"] = LocationCode;
                    }




                    MasterTable.Columns.Remove("n_FnYearId");
                    MasterTable.Columns.Remove("b_isSubLocation");
                    int N_LocationID = dLayer.SaveData("Inv_Location", "N_LocationID", MasterTable, connection, transaction);
                    if (N_LocationID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Warning("Unable to save"));
                    }
                    else
                    {
                        if (b_TransferProducts)
                        {
                            dLayer.ExecuteNonQuery("insert into Inv_ItemMasterWHLink  select ROW_NUMBER()over (Order by N_companyId)+ISNULL((Select MAX(N_RowID) from Inv_ItemMasterWHLink),0) ,N_CompanyID," + N_LocationID + ",N_ItemID,D_Entrydate from Inv_ItemMaster where  N_CompanyID=" + myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyId"].ToString()) + " ", Params, connection,transaction);
                        }
                        transaction.Commit();
                        return GetLocationDetails(int.Parse(MasterTable.Rows[0]["n_CompanyId"].ToString()), N_LocationID);
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }
        }

        [HttpDelete("delete")]
        public ActionResult DeleteData(int nLocationId)
        {
            int Results = 0;
            SortedList Params = new SortedList();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Params.Add("@nLocationId", nLocationId);
                    object count = dLayer.ExecuteScalar("select count(*) as N_Count from vw_Inv_Location_Disp where N_LocationID=@nLocationId and N_CompanyID=N_CompanyID", Params, connection);
                    int N_Count = myFunctions.getIntVAL(count.ToString());
                    if (N_Count <= 0)
                    {
                        Results = dLayer.DeleteData("Inv_Location", "N_LocationID", nLocationId, "", connection);
                    }
                }
                if (Results > 0)
                {
                    return Ok(_api.Success("Location deleted"));
                }
                else
                {
                    return Ok(_api.Error(User, "Unable to delete Location"));
                }

            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }


        }
    }
}