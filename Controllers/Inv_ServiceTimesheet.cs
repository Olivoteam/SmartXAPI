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
        private readonly int N_FormID = 1145;
        public InvServiceTimesheet(IDataAccessLayer dl,IMyFunctions myFun, IApiFunctions apiFun, IConfiguration conf)
        {
            dLayer = dl;
            api = apiFun;
            _api = api;
            myFunctions=myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }

        [HttpGet("dashboardList")]
        public ActionResult DashboardList(int? nCompanyId, int nFnYearID,int nFormID, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
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
                    Searchkey = "and (X_ServiceSheetCode like '%" + xSearchkey + "%' or X_OrderNo like '%" + xSearchkey + "%' or X_ProjectName like '%" + xSearchkey + "%' or X_CustomerName like '%" + xSearchkey + "%' or D_Invoicedate like '%" + xSearchkey + "%')";

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
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_ServiceSheetMaster where N_CompanyID=@nCompanyId   " + Searchkey + " " + xSortBy;
                else
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_ServiceSheetMaster where N_CompanyID=@nCompanyId  " + Searchkey + " and N_ServiceSheetID not in (select top(" + Count + ") N_ServiceSheetID from vw_Inv_ServiceSheetMaster where N_CompanyID=@nCompanyId " + xSortBy + " ) " + " " + xSortBy;
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
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_VendorServiceSheetMaster where N_CompanyID=@nCompanyId   " + Searchkey + " " + xSortBy;
                else
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_VendorServiceSheetMaster where N_CompanyID=@nCompanyId  " + Searchkey + " and N_ServiceSheetID not in (select top(" + Count + ") N_ServiceSheetID from vw_Inv_VendorServiceSheetMaster where N_CompanyID=@nCompanyId " + xSortBy + " ) " + " " + xSortBy;
            }
            Params.Add("@nCompanyId", nCompanyID);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    SortedList OutPut = new SortedList();

                    if(nFormID==1145)
                        sqlCommandCount = "select count(*) as N_Count  from vw_Inv_ServiceSheetMaster where N_CompanyID=@nCompanyId " + Searchkey + "";
                    else
                        sqlCommandCount = "select count(*) as N_Count  from vw_Inv_VendorServiceSheetMaster where N_CompanyID=@nCompanyId " + Searchkey + "";

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

                    int N_ServiceSheetID = myFunctions.getIntVAL(MasterRow["N_ServiceSheetID"].ToString());
                    int N_FnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearID"].ToString());
                    int N_CompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyID"].ToString());
                    int N_FormID = myFunctions.getIntVAL(MasterRow["N_FormID"].ToString());
                    string X_ServiceSheetCode = MasterRow["X_ServiceSheetCode"].ToString();

                    if(N_FormID==1145)
                    {
                        if (X_ServiceSheetCode == "@Auto")
                        {
                            Params.Add("N_CompanyID", N_CompanyID);
                            Params.Add("N_YearID", N_FnYearID);
                            Params.Add("N_FormID", N_FormID);
                            X_ServiceSheetCode = dLayer.GetAutoNumber("Inv_ServiceSheetMaster", "X_ServiceSheetCode", Params, connection, transaction);
                            if (X_ServiceSheetCode == "")
                            {
                                transaction.Rollback();
                                return Ok("Unable to generate Customer Service Timesheet");
                            }
                            MasterTable.Rows[0]["X_ServiceSheetCode"] = X_ServiceSheetCode;
                        }

                        N_ServiceSheetID = dLayer.SaveData("Inv_ServiceSheetMaster", "N_ServiceSheetID", "", "", MasterTable, connection, transaction);
                        if (N_ServiceSheetID <= 0)
                        {
                            transaction.Rollback();
                            return Ok("Unable to save Customer Service Timesheet!");
                        }
                        for (int j = 0; j < DetailTable.Rows.Count; j++)
                        {
                            DetailTable.Rows[j]["N_ServiceSheetID"] = N_ServiceSheetID;
                        }
                        int N_ServiceSheetDetailsID = dLayer.SaveData("Inv_ServiceSheetDetails", "N_ServiceSheetDetailsID", DetailTable, connection, transaction);
                        if (N_ServiceSheetDetailsID <= 0)
                        {
                            transaction.Rollback();
                            return Ok("Unable to save Customer Service Timesheet!");
                        }
                    }
                    else
                    {
                        if (X_ServiceSheetCode == "@Auto")
                        {
                            Params.Add("N_CompanyID", N_CompanyID);
                            Params.Add("N_YearID", N_FnYearID);
                            Params.Add("N_FormID", N_FormID);
                            X_ServiceSheetCode =  dLayer.GetAutoNumber("Inv_VendorServiceSheet", "X_ServiceSheetCode", Params, connection, transaction);
                            if (X_ServiceSheetCode == "")
                            {
                                transaction.Rollback();
                                return Ok("Unable to generate Vendor Service Timesheet");
                            }
                            MasterTable.Rows[0]["X_ServiceSheetCode"] = X_ServiceSheetCode;
                        }

                        N_ServiceSheetID = dLayer.SaveData("Inv_VendorServiceSheet", "N_ServiceSheetID", "", "", MasterTable, connection, transaction);
                        if (N_ServiceSheetID <= 0)
                        {
                            transaction.Rollback();
                            return Ok("Unable to save Vendor Service Timesheet!");
                        }
                        for (int j = 0; j < DetailTable.Rows.Count; j++)
                        {
                            DetailTable.Rows[j]["N_ServiceSheetID"] = N_ServiceSheetID;
                        }
                        int N_ServiceSheetDetailsID = dLayer.SaveData("Inv_VendorServiceSheetDetails", "N_ServiceSheetDetailsID", DetailTable, connection, transaction);
                        if (N_ServiceSheetDetailsID <= 0)
                        {
                            transaction.Rollback();
                            return Ok("Unable to save Vendor Service Timesheet!");
                        }
                    }

                    transaction.Commit();
                    SortedList Result = new SortedList();
                    Result.Add("N_ServiceSheetID", N_ServiceSheetID);
                    Result.Add("X_ServiceSheetCode", X_ServiceSheetCode);

                    return Ok(_api.Success(Result, "Service Timesheet saved successfully!"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

         [HttpGet("details")]
        public ActionResult EmployeeEvaluation(string xEvalCode)
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
                    Params.Add("@xEvalCode", xEvalCode);
                    Mastersql = "select * from vw_PayEvaluation_Details where N_CompanyId=@nCompanyID and X_EvalCode=@xEvalCode  ";
                   
                    MasterTable = dLayer.ExecuteDataTable(Mastersql, Params, connection);
                    if (MasterTable.Rows.Count == 0) { return Ok(_api.Warning("No data found")); }
                    int EvaID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_EvalID"].ToString());
                    Params.Add("@nEvalID", EvaID);

                    MasterTable = _api.Format(MasterTable, "Master");
                    DetailSql = "select * from vw_PayEvaluation_Details where N_CompanyId=@nCompanyID and N_EvalID=@nEvalID ";
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
            
        [HttpGet("List")]
        public ActionResult EmployeeEvaluationList()
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nComapnyID", nCompanyID);
            SortedList OutPut = new SortedList();
            string sqlCommandText = "select N_CompanyID,N_EvalID,X_EvalCode,X_Description from vw_PayEmpEvauation_List where N_CompanyID=@nComapnyID";
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
                return Ok(_api.Error(User,e));
            }
        }
        

        [HttpDelete("delete")]
        public ActionResult DeleteData(int nEvalID, int nCompanyID, int nFnYearID)
        {
            int Results = 0;
            try
            {
                SortedList QueryParams = new SortedList();
                QueryParams.Add("@nCompanyID", nCompanyID);
                QueryParams.Add("@nFnYearID", nFnYearID);
                QueryParams.Add("@nEvalID", nEvalID);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    Results = dLayer.DeleteData("Pay_EmpEvaluation", "N_EvalID", nEvalID, "", connection);


                    if (Results > 0)
                    {
                        dLayer.DeleteData("Pay_EmpEvaluationDetails", "N_EvalID", nEvalID, "", connection);
                        return Ok(_api.Success("Employee Evaluation deleted"));
                    }
                    else
                    {
                        return Ok(_api.Error(User,"Unable to delete"));
                    }

                }

            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }


        }


           }
}
    