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
    [Route("employee")]
    [ApiController]
    public class Pay_EmployeeMaster : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int FormID;


        public Pay_EmployeeMaster(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 0;
        }

        [HttpGet("list")]
        public ActionResult GetEmployeeList(int? nCompanyID,int nFnYearID, bool bAllBranchData,int nBranchID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            Params.Add("@nCompanyID",nCompanyID);
            Params.Add("@nFnYearID",nFnYearID);
            Params.Add("@bAllBranchData",bAllBranchData);
            Params.Add("@nBranchID",nBranchID);
            string sqlCommandText = "";
            if (bAllBranchData == true)
                sqlCommandText="Select N_CompanyID,N_EmpID,N_BranchID,N_FnYearID,[Employee Code],Name,X_Position,X_Department,X_BranchName from vw_PayEmployee_Disp Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID";
            else
                sqlCommandText = "Select N_CompanyID,N_EmpID,N_BranchID,N_FnYearID,[Employee Code],Name,X_Position,X_Department,X_BranchName from vw_PayEmployee_Disp Where N_CompanyID=@nCompanyID and N_FnYearID=@nFnYearID and (N_BranchID=0 or N_BranchID=@nBranchID)";

            

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params , connection);
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
                return Ok(_api.Error(e));
            }
        }
        }
    }