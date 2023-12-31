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
    [Route("crmcustomer")]
    [ApiController]
    public class CRM_Customer : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;

        public CRM_Customer(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }


        [HttpGet("list")]
        public ActionResult CustomerList(int nFnYearId,int nPage,int nSizeperpage, string xSearchkey, string xSortBy, int nCustomerID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            string sqlCommandCount = "";
            int nCompanyId=myFunctions.GetCompanyID(User);
            string UserPattern = myFunctions.GetUserPattern(User);
            int nUserID = myFunctions.GetUserID(User);
            string Pattern = "";
            if (UserPattern != "")
            {
                Pattern = " and Left(X_Pattern,Len(@p2))=@p2";
                Params.Add("@p2", UserPattern);
            }
            // else
            // {
            //     Pattern = " and N_CreatedUser=" + nUserID;

            // }
            int Count= (nPage - 1) * nSizeperpage;
            string sqlCommandText ="";

            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = " and (X_Customer like '%" + xSearchkey + "%'or X_CustomerCode like '%" + xSearchkey + "%' or X_Industry like '%" + xSearchkey + "%' or x_CustomerCategory like '%" + xSearchkey + "%'or n_AnnRevenue like '%" + xSearchkey + "%' or x_Employee like '%" + xSearchkey + "%'or x_City like '%" + xSearchkey + "%'or x_Phone like '%" + xSearchkey + "%'or x_SalesmanName like '%" + xSearchkey + "%' )";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_CustomerID desc";
            else
                xSortBy = " order by " + xSortBy;

            if(Count==0)
                sqlCommandText = "select * from vw_CRMCustomer where N_CompanyID=@p1" + Pattern + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select * from vw_CRMCustomer where N_CompanyID=@p1  " + Pattern + Searchkey + " and N_CustomerID not in (select top("+ Count +") N_CustomerID from vw_CRMCustomer where N_CompanyID=@p1 " + xSortBy + " ) " + xSortBy;


            Params.Add("@p1", nCompanyId);
            Params.Add("@p3", nFnYearId);
            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);
                    if (nCustomerID > 0)
                        sqlCommandCount = "select count(1) as N_Count from vw_ClientCRMCustomer where N_CompanyID=@p1 and N_CustID="+ nCustomerID + Pattern + Searchkey;
                    else
                        sqlCommandCount = "select count(1) as N_Count  from vw_CRMCustomer where N_CompanyID=@p1  " + Pattern;
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
                return Ok(api.Error(User,e));
            }
        }
        [HttpGet("listDetails")]
        public ActionResult CustomerListInner(int nFnyearId)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId=myFunctions.GetCompanyID(User);
           
            string sqlCommandText = "select  * from vw_CRMCustomer where N_CompanyID=@p1 ";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnyearId);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);
                    dt = api.Format(dt);
                    return Ok(api.Success(dt));

                }
                
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }


        [HttpGet("details")]
        public ActionResult CustomerListDetails(string xCustomerCode)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId=myFunctions.GetCompanyID(User);
  
            string sqlCommandText = "select * from vw_CRMCustomer where N_CompanyID=@p1 and X_CustomerCode=@p2";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", xCustomerCode);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);

                    int nCustomerID = myFunctions.getIntVAL(dt.Rows[0]["n_CustomerID"].ToString());
                    Params.Add("@nCustomerID", nCustomerID);
                    object customerCountSQ = dLayer.ExecuteScalar("select Isnull(Count(N_QuotationId),0) from Inv_SalesQuotation where N_CrmCompanyID=@nCustomerID and N_CompanyID=@p1", Params, connection);
                    if ( myFunctions.getIntVAL(customerCountSQ.ToString()) > 0 )
                    {
                        myFunctions.AddNewColumnToDataTable(dt, "b_CustomerCount", typeof(bool), true);
                    }   
                    
                }
                dt = api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(api.Warning("No Results Found"));
                }
                else
                {
                    return Ok(api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
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
                int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyId"].ToString());
                int nFnYearId = myFunctions.getIntVAL(MasterTable.Rows[0]["n_FnYearId"].ToString());
                int nCustomerID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CustomerID"].ToString());

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    // Auto Gen
                    string LeadCode = "";
                    var values = MasterTable.Rows[0]["x_CustomerCode"].ToString();
                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearId);
                        Params.Add("N_FormID", 1306);
                        LeadCode = dLayer.GetAutoNumber("CRM_Customer", "x_CustomerCode", Params, connection, transaction);
                        if (LeadCode == "") { transaction.Rollback();return Ok(api.Error(User,"Unable to generate Customer Code")); }
                        MasterTable.Rows[0]["x_CustomerCode"] = LeadCode;
                    }

                    nCustomerID = dLayer.SaveData("CRM_Customer", "n_CustomerID",  MasterTable, connection, transaction);
                    if (nCustomerID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User,"Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("Customer Created"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }
        }

      
        [HttpDelete("delete")]
        public ActionResult DeleteData(int nCustomerID)
        {

             int Results = 0;
            try
            {                        
                SortedList Params = new SortedList();
                SortedList QueryParams = new SortedList();                
                QueryParams.Add("@nFormID", 1305);
                QueryParams.Add("@nCustomerID", nCustomerID);



                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    SqlTransaction transaction = connection.BeginTransaction();

                    object NCustomerID = dLayer.ExecuteScalar("select N_CrmCompanyID from Inv_Customer where N_CrmCompanyID=@nCustomerID", QueryParams, connection, transaction);
                    NCustomerID = NCustomerID == null ? 0 : NCustomerID;
                     if (myFunctions.getIntVAL(NCustomerID.ToString()) > 0)
                  
                  
                    {
                     return Ok(api.Error(User, "Unable to delete Customer"));
                    }

                     object NSOCustomerID = dLayer.ExecuteScalar("select N_CrmCompanyID from Inv_SalesQuotation where N_CrmCompanyID=@nCustomerID", QueryParams, connection, transaction);
                     NSOCustomerID = NSOCustomerID == null ? 0 : NSOCustomerID;
                     if (myFunctions.getIntVAL(NSOCustomerID.ToString()) > 0)
                     {
                     return Ok(api.Error(User, "Unable to delete Customer"));
                    }
                    
                    else
                     {
                 Results = dLayer.DeleteData("CRM_Customer", "N_CustomerID", nCustomerID, "", connection, transaction);
                 transaction.Commit();
                     }
                  
                }
                if (Results > 0)
                {
                    Dictionary<string,string> res=new Dictionary<string, string>();
                    res.Add("N_CustomerID",nCustomerID.ToString());
                    return Ok(api.Success(res,"Customer deleted"));
                }
                else
                {
                    return Ok(api.Error(User,"Unable to delete Customer"));
                }

            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }



        }
    }
}