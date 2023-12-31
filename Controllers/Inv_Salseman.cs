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
    [Route("salesman")]
    [ApiController]



    public class Inv_Salseman : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int FormID;


        public Inv_Salseman(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 290;
        }


        //List
        [HttpGet("list")]
        public ActionResult GetAllSalesExecutives(int? nCompanyID, int? nFnyearID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "select * from vw_InvSalesman where N_CompanyID=@p1 and N_FnYearID=@p2 order by N_SalesmanID DESC";
            Params.Add("@p1", nCompanyID);
            Params.Add("@p2", nFnyearID);

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


     
        //details
        [HttpGet("details")]
        public ActionResult GetAllSalesExecutivesDetails(int? n_SalesmanID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "select * from vw_InvSalesman where n_SalesmanID=@p3 and N_CompanyID=@p1";
            Params.Add("@p1", myFunctions.GetCompanyID(User));
            // Params.Add("@p2", nFnyearID);
            Params.Add("@p3", n_SalesmanID);

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
                    string ExecutiveCode = MasterTable.Rows[0]["x_SalesmanCode"].ToString();
                    string SalesmanName = MasterTable.Rows[0]["x_SalesmanName"].ToString();
                    string nCompanyID = MasterTable.Rows[0]["n_CompanyId"].ToString();
                    string nFnYearID = MasterTable.Rows[0]["n_FnYearId"].ToString();
                    int N_SalesmanID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_SalesmanID"].ToString());
                    if (ExecutiveCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", this.FormID);
                        Params.Add("N_BranchID", MasterTable.Rows[0]["n_BranchId"].ToString());
                        ExecutiveCode = dLayer.GetAutoNumber("inv_salesman", "X_SalesmanCode", Params, connection, transaction);
                        if (ExecutiveCode == "") { transaction.Rollback(); return Ok(_api.Error(User,"Unable to generate Sales Executive Code")); }
                        MasterTable.Rows[0]["X_SalesmanCode"] = ExecutiveCode;

                    }
                    else
                    {
                        dLayer.DeleteData("inv_salesman", "N_SalesmanID", N_SalesmanID, "", connection, transaction);
                    }

                    N_SalesmanID = dLayer.SaveData("inv_salesman", "N_SalesmanID", MasterTable, connection, transaction);
                    if (N_SalesmanID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User,"Unable to save"));
                    }
                    else
                    {

                        SortedList nParams = new SortedList();
                        nParams.Add("@p1", nCompanyID);
                        nParams.Add("@p2", nFnYearID);
                        string sqlCommandText = "select * from vw_InvSalesman where N_CompanyID=@p1 and N_FnYearID=@p2 ";
                        DataTable outputDt = dLayer.ExecuteDataTable(sqlCommandText, nParams, connection, transaction);
                        outputDt = _api.Format(outputDt, "NewSalesMan");
                        transaction.Commit();
                        return Ok(_api.Success(outputDt, "Salesman Saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

   [HttpDelete("delete")]
        public ActionResult DeleteData(int nSalesmanID)
        {
            int Results = 0;
             object Count =0;
            int nCompanyID = myFunctions.GetCompanyID(User);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                          object SalesCount = dLayer.ExecuteScalar("select count(N_SalesID) from Inv_Sales Where N_CompanyID=" + nCompanyID + " and  N_SalesmanID=" + nSalesmanID, connection);
                   object orderCount = dLayer.ExecuteScalar("select count(N_SalesOrderId) from Inv_SalesOrder Where N_CompanyID=" + nCompanyID + " and  N_SalesmanID=" + nSalesmanID, connection);
                   object quotationCount = dLayer.ExecuteScalar("select count(N_QuotationId) from Inv_SalesQuotation Where N_CompanyID=" + nCompanyID + " and  N_SalesmanID=" + nSalesmanID, connection);
                   object deliveryCount = dLayer.ExecuteScalar("select count(N_DeliveryNoteId) from Inv_DeliveryNote Where N_CompanyID=" + nCompanyID + " and  N_SalesmanID=" + nSalesmanID, connection);
                   object leadCount = dLayer.ExecuteScalar("select count(N_LeadID) from CRM_Leads Where N_CompanyID=" + nCompanyID + " and  N_SalesmanID=" + nSalesmanID, connection);
                   object oppCount = dLayer.ExecuteScalar("select count(N_OpportunityID) from CRM_Opportunity Where N_CompanyID=" + nCompanyID + " and  N_SalesmanID=" + nSalesmanID, connection);
                   object contactCount = dLayer.ExecuteScalar("select count(N_ContactID) from CRM_Contact Where N_CompanyID=" + nCompanyID + " and  N_SalesmanID=" + nSalesmanID, connection);
                   object crmcustCount = dLayer.ExecuteScalar("select count(N_CustomerID) from CRM_Customer Where N_CompanyID=" + nCompanyID + " and  N_SalesmanID=" + nSalesmanID, connection);
                   object receiptCount = dLayer.ExecuteScalar("select count(N_PayReceiptId) from Inv_PayReceipt Where N_CompanyID=" + nCompanyID + " and  N_SalesmanID=" + nSalesmanID, connection);
                  if( myFunctions.getIntVAL(SalesCount.ToString())>0||myFunctions.getIntVAL(orderCount.ToString())>0||myFunctions.getIntVAL(quotationCount.ToString())>0||myFunctions.getIntVAL(deliveryCount.ToString())>0||
                     myFunctions.getIntVAL(leadCount.ToString())>0||myFunctions.getIntVAL(oppCount.ToString())>0||myFunctions.getIntVAL(contactCount.ToString())>0||myFunctions.getIntVAL(crmcustCount.ToString())>0||myFunctions.getIntVAL(receiptCount.ToString())>0)
                   {
                      return Ok(_api.Error(User, "Can not Delete Sales Man"));
                   }
                   else{
                    Results = dLayer.DeleteData("inv_salesman", "N_SalesmanID", nSalesmanID, "", connection);

             
                    if (Results > 0)
                    {
                        return Ok( _api.Success("Sales Executive deleted"));
                    }
                    else
                    {
                        return Ok(_api.Error(User,"Unable to delete Sales Executive"));
                    }
                   }
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

        // [HttpDelete("delete")]
        // public ActionResult DeleteData(int nSalesmanID)
        // {
        //     int Results = 0;
        //     try
        //     {
        //         using (SqlConnection connection = new SqlConnection(connectionString))
        //         {
        //             connection.Open();
        //             Results = dLayer.DeleteData("inv_salesman", "N_SalesmanID", nSalesmanID, "", connection);
        //         }
        //         if (Results > 0)
        //         {
        //             return Ok(_api.Success("Sales Executive deleted"));
        //         }
        //         else
        //         {
        //             return Ok(_api.Error(User,"Unable to delete Sales Executive"));
        //         }

        //     }
        //     catch (Exception ex)
        //     {
        //         return Ok(_api.Error(User,ex));
        //     }


        // }
     }
}