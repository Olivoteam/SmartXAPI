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
     [Route("whpick")]
     [ApiController]
    public class WhPickList : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions api;
        private readonly string connectionString;
        private readonly IMyFunctions myFunctions;
        private readonly IApiFunctions _api;
        private readonly int nFormID = 1411;
        public WhPickList(IDataAccessLayer dl,IMyFunctions myFun, IApiFunctions apiFun, IConfiguration conf)
        {
            dLayer = dl;
            api = apiFun;
            _api = api;
            myFunctions=myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }

        [HttpGet("list")]
        public ActionResult GetDashboardList(int nPage, int nSizeperpage, string xSearchkey, string xSortBy, int nFnYearID)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_PickListCode like '%" + xSearchkey + "%' or X_CustomerName like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_PickListID desc";
            else
            {
                xSortBy = " order by " + xSortBy;
            }

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_WhPickListMaster where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_WhPickListMaster where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + Searchkey + " and N_PickListID not in (select top(" + Count + ") N_PickListID from vw_WhPickListMaster where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + Searchkey + xSortBy + " ) " + xSortBy;

            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnYearID);

            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    string sqlCommandCount = "select count(*) as N_Count  from vw_WhPickListMaster where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID " + Searchkey + "";
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("Details", _api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                    if (dt.Rows.Count == 0)
                    {
                        return Ok(_api.Success(OutPut));
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
        
        [HttpPost("Save")]
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
                    MasterTable = ds.Tables["master"];
                    DetailTable = ds.Tables["details"];
                    DataRow MasterRow = MasterTable.Rows[0];
                    SortedList Params = new SortedList();

                    int nPickListID = myFunctions.getIntVAL(MasterRow["n_PickListID"].ToString());
                    int nFnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearID"].ToString());
                    int nCompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyID"].ToString());
                    string xPickListCode = MasterRow["x_PickListCode"].ToString();

                    string x_PickListCode = "";
                    if (xPickListCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", nFormID);
                        x_PickListCode = dLayer.GetAutoNumber("Wh_PickList", "X_PickListCode", Params, connection, transaction);
                        if (x_PickListCode == "")
                        {
                            transaction.Rollback();
                            return Ok("Unable to generate Picklist Code");
                        }
                        MasterTable.Rows[0]["x_PickListCode"] = x_PickListCode;
                    }

                    int n_PickListID = dLayer.SaveData("Wh_PickList", "N_PickListID", "", "", MasterTable, connection, transaction);
                    if (n_PickListID <= 0)
                    {
                        transaction.Rollback();
                        return Ok("Unable to save Warehouse Picklist");
                    }
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {
                        DetailTable.Rows[j]["n_PickListID"] = n_PickListID;
                    }
                    int n_PickListDetailsID = dLayer.SaveData("Wh_PickListDetails", "N_PickListDetailsID", DetailTable, connection, transaction);
                    if (n_PickListDetailsID <= 0)
                    {
                        transaction.Rollback();
                        return Ok("Unable to save Warehouse Picklist");
                    }

                    transaction.Commit();
                    SortedList Result = new SortedList();
                    Result.Add("n_PickListID", n_PickListID);
                    Result.Add("x_PickListCode", x_PickListCode);
                    Result.Add("n_PickListDetailsID", n_PickListDetailsID);

                    return Ok(_api.Success(Result, "Warehouse Picklist Created"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

        [HttpGet("details")]
        public ActionResult EmployeeEvaluation(string xPickListCode)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataSet dt = new DataSet();
                    SortedList Params = new SortedList();
                    DataTable MasterTable = new DataTable();
                    DataTable DetailTable = new DataTable();
                    DataTable DataTable = new DataTable();
                    string Mastersql = "";
                    string DetailSql = "";

                    Params.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    Params.Add("@xPickListCode", xPickListCode);
                    Mastersql = "select * from vw_WhPickListMaster where N_CompanyID=@nCompanyID and X_PickListCode=@xPickListCode ";
                   
                    MasterTable = dLayer.ExecuteDataTable(Mastersql, Params, connection);
                    if (MasterTable.Rows.Count == 0) { return Ok(_api.Warning("No data found")); }
                    int nPickListID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_PickListID"].ToString());
                    Params.Add("@nPickListID", nPickListID);

                    MasterTable = _api.Format(MasterTable, "Master");
                    DetailSql = "select * from vw_WhPickListDetails where N_CompanyID=@nCompanyID and N_PickListID=@nPickListID ";
                    DetailTable = dLayer.ExecuteDataTable(DetailSql, Params, connection);
                    DetailTable = _api.Format(DetailTable, "Details");
                    dt.Tables.Add(MasterTable);
                    dt.Tables.Add(DetailTable);
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User,e));
            }
        }

        // [HttpDelete("delete")]
        // public ActionResult DeleteData(int nEvalID, int nCompanyID, int nFnYearID)
        // {
        //     int Results = 0;
        //     try
        //     {
        //         SortedList QueryParams = new SortedList();
        //         QueryParams.Add("@nCompanyID", nCompanyID);
        //         QueryParams.Add("@nFnYearID", nFnYearID);
        //         QueryParams.Add("@nEvalID", nEvalID);
        //         using (SqlConnection connection = new SqlConnection(connectionString))
        //         {
        //             connection.Open();

        //             Results = dLayer.DeleteData("Pay_EmpEvaluation", "N_EvalID", nEvalID, "", connection);


        //             if (Results > 0)
        //             {
        //                 dLayer.DeleteData("Pay_EmpEvaluationDetails", "N_EvalID", nEvalID, "", connection);
        //                 return Ok(_api.Success("Employee Evaluation deleted"));
        //             }
        //             else
        //             {
        //                 return Ok(_api.Error(User,"Unable to delete"));
        //             }

        //         }

        //     }
        //     catch (Exception ex)
        //     {
        //         return Ok(_api.Error(User,ex));
        //     }


        // }


           }
}
    