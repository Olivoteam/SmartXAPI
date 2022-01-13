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
    [Route("dataupload")]
    [ApiController]
    public class Gen_DataUpload : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        public Gen_DataUpload(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }
        //Save....
        [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                DataTable Mastertable = new DataTable();
                int nCompanyID = myFunctions.GetCompanyID(User);
                int nFnYearId = 1;
                int nMasterID = 0;
                string xTableName = "";
                SortedList Params = new SortedList();
                Params.Add("N_CompanyID", nCompanyID);
                Params.Add("N_FnYearID", nFnYearId);
                int N_UserID= myFunctions.GetUserID(User);

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    foreach (DataTable dt in ds.Tables)
                    {
                    if (dt.Columns.Contains("notes"))
                    dt.Columns.Remove("notes");
                        Params.Add("X_Type", dt.TableName);
                        Mastertable = ds.Tables[dt.TableName];
                        foreach (DataColumn col in Mastertable.Columns)
                        {
                            col.ColumnName = col.ColumnName.Replace(" ", "_");
                            col.ColumnName = col.ColumnName.Replace("*", "");
                            col.ColumnName = col.ColumnName.Replace("/", "_");
                        }
                        Mastertable.Columns.Add("Pkey_Code");

                        if (dt.TableName == "Customer List" || dt.TableName == "Customers" || dt.TableName == "Customer")
                            xTableName = "Mig_Customers";
                        if (dt.TableName == "Vendor List"  || dt.TableName == "Vendors" || dt.TableName == "Vendor")
                            xTableName = "Mig_Vendors";
                        if (dt.TableName == "Lead List")
                        {
                            xTableName = "Mig_Leads";
                            Mastertable.Columns.Add("N_UserID");
                            foreach (DataRow dtRow in Mastertable.Rows)
                            {
                                dtRow["N_UserID"] = N_UserID;
                            }
                        }
                        if (dt.TableName == "Chart of Accounts")
                            xTableName = "Mig_Accounts";
                        if (dt.TableName == "Products Stock")
                            xTableName = "Mig_Stock";
                        if (dt.TableName == "Employee List" || dt.TableName == "Employees")
                            xTableName = "Mig_Employee";
                        if (dt.TableName == "products stock")
                            xTableName = "Mig_Stock";
                        if (dt.TableName == "FixedAssets List")
                            xTableName = "_Mig_AssetList";
                        if (dt.TableName == "Salary History")
                            xTableName = "Mig_EmployeeSalaryHistory";
                        if (dt.TableName == "Employee Salary")
                            xTableName = "Mig_EmployeeSalary";
                        if (dt.TableName == "Leave History")
                            xTableName = "Mig_EmployeeLeaveHistory";




                        if (dt.TableName == "Product List" || dt.TableName == "Products")
                        {
                            xTableName = "Mig_Items";
                            Mastertable.Columns.Add("N_CompanyID");
                            foreach (DataRow dtRow in Mastertable.Rows)
                            {
                                dtRow["N_CompanyID"] = nCompanyID;
                            }
                        }


                        if (dt.TableName == "Category")
                        {
                            xTableName = "Mig_POSCategory";
                            Mastertable.Columns.Add("N_CompanyID");
                            foreach (DataRow dtRow in Mastertable.Rows)
                            {
                                dtRow["N_CompanyID"] = nCompanyID;
                            }
                        }

                        if (dt.TableName == "Package Items")
                        {
                            xTableName = "Mig_PackageItem";
                            Mastertable.Columns.Add("N_CompanyID");
                            foreach (DataRow dtRow in Mastertable.Rows)
                            {
                                dtRow["N_CompanyID"] = nCompanyID;
                            }
                        }


                        if (dt.TableName == "Warranty Items")
                        {
                            xTableName = "Mig_WarrantyItem";
                            Mastertable.Columns.Add("N_CompanyID");
                            foreach (DataRow dtRow in Mastertable.Rows)
                            {
                                dtRow["N_CompanyID"] = nCompanyID;
                            }
                        }

                        if (Mastertable.Rows.Count > 0)
                        {

                            dLayer.ExecuteNonQuery("delete from " + xTableName, Params, connection, transaction);
                            nMasterID = dLayer.SaveData(xTableName, "PKey_Code", Mastertable, connection, transaction);
                            dLayer.ExecuteNonQueryPro("SP_SetupData", Params, connection, transaction);
                            if (nMasterID <= 0)
                            {
                                transaction.Rollback();
                                return Ok(_api.Error(User, dt.TableName + " Uploaded Error"));
                            }
                            Mastertable.Clear();
                            Params.Remove("X_Type");
                            transaction.Commit();
                            return Ok(_api.Success(dt.TableName + " Uploaded"));
                        }
                    }
                    if (Mastertable.Rows.Count > 0)
                    {
                        transaction.Commit();
                        return Ok(_api.Success("Uploaded Completed"));
                    }
                    else
                    {
                        return Ok(_api.Error(User, "Uploaded Error"));
                    }
                }

            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }
        }

        [HttpGet("dataList")]
        public ActionResult GetDepartmentList(string parent)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable dt = new DataTable();
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    Params.Add("@nCompanyID", nCompanyID);
                    string sqlCommandText = "select * from VW_TableCount where  N_CompanyID= " + nCompanyID + "";




                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    dt = _api.Format(dt);
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