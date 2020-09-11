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
using System.Linq;
using System.Security.Claims;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("travelorderrequest")]
    [ApiController]



    public class Ess_TravelOrderRequest : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int FormID;


        public Ess_TravelOrderRequest(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 1235;
        }


        //List
        [HttpGet("list")]
        public ActionResult GetTravelOrderRequest(int? nCompanyID, string xReqType)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();

            int userid = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier).Value);
            int companyid = int.Parse(User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Sid).Value);
            string companyname = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.StreetAddress).Value;
            string username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;

            QueryParams.Add("@nCompanyID", companyid);
            QueryParams.Add("@nUserID", userid);
            string sqlCommandText = "";

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    object objEmpID = dLayer.ExecuteScalar("Select N_EmpID From Sec_User where N_UserID=@nUserID and N_CompanyID=@nCompanyID", QueryParams, connection);
                    if (objEmpID != null)
                    {
                        QueryParams.Add("@nEmpID", myFunctions.getIntVAL(objEmpID.ToString()));
                        QueryParams.Add("@xStatus", xReqType);
                        if (xReqType.ToLower() == "all")
                            sqlCommandText = "select * from vw_PayLoanApprovals where N_CompanyID=@nCompanyID order by D_LoanPeriodTo Desc";
                        else

                            sqlCommandText = "select * from vw_PayLoanApprovals where N_CompanyID=@nCompanyID and X_Status like '%@xStatus%'  order by D_LoanPeriodTo Desc ";

                        dt = dLayer.ExecuteDataTable(sqlCommandText, QueryParams, connection);
                    }


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
                return BadRequest(_api.Error(e));
            }
        }
       
        //Save....
        [HttpPost("save")]
        public ActionResult SaveTORequest([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                MasterTable = ds.Tables["master"];
                SortedList Params = new SortedList();
                DataRow MasterRow = MasterTable.Rows[0];

                var x_RequestCode = MasterRow["x_RequestCode"].ToString();
                int nRequestID = myFunctions.getIntVAL(MasterRow["n_RequestID"].ToString());
                int nCompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyId"].ToString());
                int nFnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearId"].ToString());
                int nEmpID = myFunctions.getIntVAL(MasterRow["n_EmpID"].ToString());

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction(); ;
                    if (x_RequestCode == "@Auto")
                    {
                        Params.Add("@nCompanyID", nCompanyID);
                        object objReqCode = dLayer.ExecuteScalar("Select max(isnull(N_RequestID,0))+1 as N_RequestID from Pay_EmpBussinessTripRequest where N_CompanyID=@nCompanyID",Params,connection,transaction);
                            if(objReqCode.ToString()==""|| objReqCode.ToString()==null){x_RequestCode="1";}else{
                            x_RequestCode = objReqCode.ToString();
                            }
                            MasterTable.Rows[0]["x_RequestCode"] = x_RequestCode;
                    }
                    else
                    {
                        dLayer.DeleteData("Pay_EmpBussinessTripRequest", "n_RequestID", nRequestID, "", connection, transaction);
                    }
                    MasterTable = myFunctions.AddNewColumnToDataTable(MasterTable,"N_RequestType",typeof(int),this.FormID);
                    MasterTable.Columns.Remove("n_RequestID");
                    MasterTable.AcceptChanges();


                    nRequestID = dLayer.SaveData("Pay_EmpBussinessTripRequest", "n_RequestID", nRequestID, MasterTable, connection, transaction);
                    if (nRequestID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error("Unable to save"));
                    }
                    else
                    {
                         transaction.Commit();
                    }
                    Dictionary<string,string> res=new Dictionary<string, string>();
                    res.Add("x_RequestCode",x_RequestCode.ToString());
                    return Ok(_api.Success(res,"Travel Order request saved"));
                }
            }
            catch (Exception ex)
            {
                return BadRequest(_api.Error(ex));
            }
        }


          [HttpDelete()]
        public ActionResult DeleteData(int nRequestID)
        {
            int Results = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                           Results = dLayer.DeleteData("Pay_EmpBussinessTripRequest", "n_RequestID", nRequestID, "", connection, transaction);
                    if (Results <= 0)
                    {
                        transaction.Rollback();
                        return Ok( _api.Error( "Unable to delete Travel Order request"));
                    }
                        transaction.Commit();
                        return Ok( _api.Success("Travel Order request Deleted Successfully"));
                   
                }
            }
            catch (Exception ex)
            {
                return BadRequest(_api.Error(ex));
            }
        }



    }
}