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
    // [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("API")]
    [ApiController]
    public class Api_Common : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly ISec_UserRepo _repository;

        public Api_Common(ISec_UserRepo repository, IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            _repository = repository;
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString =
            conf.GetConnectionString("SmartxConnection");
        }
        
        [HttpPost("customer")]
        public ActionResult SaveCustomer([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                DataTable dt;
                MasterTable = ds.Tables["master"];
                int nCustomerID = 0;
                string Auth = "";
                if (Request.Headers.ContainsKey("Authorization"))
                    Auth = Request.Headers["Authorization"];
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "pkey_code", typeof(int), 0);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    dt = dLayer.ExecuteDataTable("select * from sec_user where  X_Token='" + Auth + "'", Params, connection, transaction);
                    if (dt.Rows.Count > 0)
                    {
                        MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "N_CompanyID", typeof(int), dt.Rows[0]["N_CompanyID"]);
                        object N_FnyearID = dLayer.ExecuteScalar("select MAX(N_FnyearID) from Acc_Fnyear where N_CompanyID=" + dt.Rows[0]["N_CompanyID"], connection, transaction);
                        Params.Add("N_CompanyID", dt.Rows[0]["N_CompanyID"]);
                        Params.Add("N_FnyearID", N_FnyearID);
                        Params.Add("X_Type", "customer");
                        Params.Add("N_BranchID", dt.Rows[0]["N_BranchID"]);
                        Params.Add("N_LocationID", dt.Rows[0]["N_LocationID"]);
                        

                        dLayer.ExecuteNonQuery("delete from mig_customers", Params, connection, transaction);
                        nCustomerID = dLayer.SaveData("mig_customers", "pkey_code", MasterTable, connection, transaction);
                        dLayer.ExecuteNonQueryPro("SP_SetupData_cloud", Params, connection, transaction);
                        dLayer.ExecuteNonQuery("Update sec_user Set X_Token= '' where N_UserID = " + dt.Rows[0]["N_UserID"], Params, connection, transaction);

                    }
                    else
                        return Ok(api.Error(User, "Invalid Token"));



                    if (nCustomerID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User, "Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("Customer Saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User, ex));
            }
        }
        [HttpPost("vendor")]
        public ActionResult SaveVendor([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                DataTable dt;
                MasterTable = ds.Tables["master"];
                int nVendorID = 0;
                string Auth = "";
                if (Request.Headers.ContainsKey("Authorization"))
                    Auth = Request.Headers["Authorization"];
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "pkey_code", typeof(int), 0);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    dt = dLayer.ExecuteDataTable("select * from sec_user where  X_Token='" + Auth + "'", Params, connection, transaction);
                    if (dt.Rows.Count > 0)
                    {
                        MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "N_CompanyID", typeof(int), dt.Rows[0]["N_CompanyID"]);
                        object N_FnyearID = dLayer.ExecuteScalar("select MAX(N_FnyearID) from Acc_Fnyear where N_CompanyID=" + dt.Rows[0]["N_CompanyID"], connection, transaction);
                        Params.Add("N_CompanyID", dt.Rows[0]["N_CompanyID"]);
                        Params.Add("N_FnyearID", N_FnyearID);
                        Params.Add("X_Type", "vendor");
                        Params.Add("N_BranchID", dt.Rows[0]["N_BranchID"]);
                        Params.Add("N_LocationID", dt.Rows[0]["N_LocationID"]);
                        

                        dLayer.ExecuteNonQuery("delete from Mig_Vendors", Params, connection, transaction);
                        nVendorID = dLayer.SaveData("Mig_Vendors", "pkey_code", MasterTable, connection, transaction);
                        dLayer.ExecuteNonQueryPro("SP_SetupData_cloud", Params, connection, transaction);
                        dLayer.ExecuteNonQuery("Update sec_user Set X_Token= '' where N_UserID = " + dt.Rows[0]["N_UserID"], Params, connection, transaction);

                    }
                    else
                        return Ok(api.Error(User, "Invalid Token"));



                    if (nVendorID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User, "Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("Vendor Saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User, ex));
            }
        }
        [HttpPost("FTSales")]
        public ActionResult SaveFTSales([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                DataTable dt;
                MasterTable = ds.Tables["master"];
                int nSalesID = 0;
                string Auth = "";
                if (Request.Headers.ContainsKey("Authorization"))
                    Auth = Request.Headers["Authorization"];
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "pkey_code", typeof(int), 0);
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "unit", typeof(string), "Nos");
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "Item_Code", typeof(string), "001");
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "Item_Name", typeof(string), "Default Item");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    dt = dLayer.ExecuteDataTable("select * from sec_user where  X_Token='" + Auth + "'", Params, connection, transaction);
                    if (dt.Rows.Count > 0)
                    {
                        MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "N_CompanyID", typeof(int), dt.Rows[0]["N_CompanyID"]);
                        object N_FnyearID = dLayer.ExecuteScalar("select MAX(N_FnyearID) from Acc_Fnyear where N_CompanyID=" + dt.Rows[0]["N_CompanyID"], connection, transaction);
                        Params.Add("N_CompanyID", dt.Rows[0]["N_CompanyID"]);
                        Params.Add("N_FnyearID", N_FnyearID);
                        Params.Add("N_BranchID", dt.Rows[0]["N_BranchID"]);
                        Params.Add("N_LocationID", dt.Rows[0]["N_LocationID"]);
                        Params.Add("N_UserID", dt.Rows[0]["N_UserID"]);
                        

                        dLayer.ExecuteNonQuery("delete from Mig_SalesInvoice", Params, connection, transaction);
                        nSalesID = dLayer.SaveData("Mig_SalesInvoice", "pkey_code", MasterTable, connection, transaction);
                        dLayer.ExecuteNonQueryPro("SP_FTSalesInvoiceImport", Params, connection, transaction);
                        dLayer.ExecuteNonQuery("Update sec_user Set X_Token= '' where N_UserID = " + dt.Rows[0]["N_UserID"], Params, connection, transaction);

                    }
                    else
                        return Ok(api.Error(User, "Invalid Token"));



                    if (nSalesID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User, "Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("FTSales Saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                 return Ok(api.Error(User, ex));
            }
        }
        [HttpPost("cusomerreceipt")]
        public ActionResult Savecusomerreceipt([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                DataTable dt;
                MasterTable = ds.Tables["master"];
                int nSalesID = 0;
                string Auth = "";
                if (Request.Headers.ContainsKey("Authorization"))
                    Auth = Request.Headers["Authorization"];
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "pkey_code", typeof(int), 0);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    dt = dLayer.ExecuteDataTable("select * from sec_user where  X_Token='" + Auth + "'", Params, connection, transaction);
                    if (dt.Rows.Count > 0)
                    {
                        MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "N_CompanyID", typeof(int), dt.Rows[0]["N_CompanyID"]);
                        object N_FnyearID = dLayer.ExecuteScalar("select MAX(N_FnyearID) from Acc_Fnyear where N_CompanyID=" + dt.Rows[0]["N_CompanyID"], connection, transaction);
                        Params.Add("N_CompanyID", dt.Rows[0]["N_CompanyID"]);
                        Params.Add("N_FnyearID", N_FnyearID);
                        Params.Add("X_Type", "customer payment");
                        Params.Add("N_BranchID", dt.Rows[0]["N_BranchID"]);
                        Params.Add("N_LocationID", dt.Rows[0]["N_LocationID"]);
                        

                        dLayer.ExecuteNonQuery("delete from Mig_CustomerPayment", Params, connection, transaction);
                        nSalesID = dLayer.SaveData("Mig_CustomerPayment", "pkey_code", MasterTable, connection, transaction);
                        dLayer.ExecuteNonQueryPro("SP_SetupData_cloud", Params, connection, transaction);
                        dLayer.ExecuteNonQuery("Update sec_user Set X_Token= '' where N_UserID = " + dt.Rows[0]["N_UserID"], Params, connection, transaction);

                    }
                    else
                        return Ok(api.Error(User, "Invalid Token"));



                    if (nSalesID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User, "Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("Sales Receipt Saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                 return Ok(api.Error(User, ex));
            }
        }
        [HttpPost("FTPurchase")]
        public ActionResult SaveFTPurchase([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                DataTable dt;
                MasterTable = ds.Tables["master"];
                int nPurchaseID = 0;
                string Auth = "";
                if (Request.Headers.ContainsKey("Authorization"))
                    Auth = Request.Headers["Authorization"];
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "pkey_code", typeof(int), 0);
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "unit", typeof(string), "Nos");
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "Item_Code", typeof(string), "001");
                MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "Item_Name", typeof(string), "Default Item");
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    dt = dLayer.ExecuteDataTable("select * from sec_user where  X_Token='" + Auth + "'", Params, connection, transaction);
                    if (dt.Rows.Count > 0)
                    {
                        MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable, "N_CompanyID", typeof(int), dt.Rows[0]["N_CompanyID"]);
                        object N_FnyearID = dLayer.ExecuteScalar("select MAX(N_FnyearID) from Acc_Fnyear where N_CompanyID=" + dt.Rows[0]["N_CompanyID"], connection, transaction);
                        Params.Add("N_CompanyID", dt.Rows[0]["N_CompanyID"]);
                        Params.Add("N_FnyearID", N_FnyearID);
                        Params.Add("X_Type", "FTpurchase invoice");
                        Params.Add("N_BranchID", dt.Rows[0]["N_BranchID"]);
                        Params.Add("N_LocationID", dt.Rows[0]["N_LocationID"]);
                        

                        dLayer.ExecuteNonQuery("delete from Mig_PurchaseInvoice", Params, connection, transaction);
                        nPurchaseID = dLayer.SaveData("Mig_PurchaseInvoice", "pkey_code", MasterTable, connection, transaction);
                        dLayer.ExecuteNonQueryPro("SP_SetupData_cloud", Params, connection, transaction);
                       // dLayer.ExecuteNonQuery("Update sec_user Set X_Token= '' where N_UserID = " + dt.Rows[0]["N_UserID"], Params, connection, transaction);

                    }
                    else
                        return Ok(api.Error(User, "Invalid Token"));



                    if (nPurchaseID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User, "Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("FTPurchase Saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                 return Ok(api.Error(User, ex));
            }
        }
         [HttpGet("schClasslist")]
        public ActionResult schClasslist(int nCompanyID)
        {
            try
            {
                DataTable MasterTable;
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    MasterTable = dLayer.ExecuteDataTable("select * from Sch_Class where N_CompanyID="+nCompanyID, Params, connection,transaction);
                    if(MasterTable.Rows.Count<=0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User, "No Data Found"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(MasterTable);
                    }
                }
            }
            catch (Exception ex)
            {
                 return Ok(api.Error(User, ex));
            }
        }
         [HttpPost("schRegistration")]
        public ActionResult schRegistration([FromBody] DataSet ds)
        {
           try
            {
                DataTable MasterTable = new DataTable();
                DataRow row = MasterTable.NewRow();
                MasterTable.Rows.Add(row);
                int N_RegID = 0;
                string Stud = "";
                DataTable Details = ds.Tables["master"];

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();

                    string xencrypt=Details.Rows[0]["encrypt"].ToString();
                    xencrypt=xencrypt.Replace("xx", "");
                    int N_CompanyID=myFunctions.getIntVAL(xencrypt.ToString());
                    object N_FnyearID = dLayer.ExecuteScalar("select MAX(N_FnyearID) from Acc_Fnyear where N_CompanyID=" + N_CompanyID, connection, transaction);
                     Params.Add("N_CompanyID", N_CompanyID);
                     Params.Add("N_YearID", N_FnyearID);
                     Params.Add("N_FormID", 1517);
                     Stud = dLayer.GetAutoNumber("Sch_Registration", "X_RegNo", Params, connection, transaction);
                    if (Stud == "") { transaction.Rollback(); return Ok(api.Warning("Unable to Generate Registration Number")); }


                    Details = myFunctions.AddNewColumnToDataTable(Details, "N_CompanyId", typeof(int), N_CompanyID);
                    Details = myFunctions.AddNewColumnToDataTable(Details, "N_AcYearID", typeof(int), myFunctions.getIntVAL(N_FnyearID.ToString()));
                    Details = myFunctions.AddNewColumnToDataTable(Details, "N_RegID", typeof(int), 0);
                    Details = myFunctions.AddNewColumnToDataTable(Details, "X_RegNo", typeof(int), Stud);
                    Details.Columns.Remove("encrypt");


                    
                    if (Details.Rows.Count > 0)
                        N_RegID = dLayer.SaveData("Sch_Registration", "N_RegID", Details, connection, transaction);
                    else
                        return Ok(api.Error("Unable to save"));



                    if (N_RegID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error("Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("Registration saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error("Unable to save"));
            }
        }
        
    }
}

