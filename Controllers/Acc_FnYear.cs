using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SmartxAPI.GeneralFunctions;
using System;
using System.Data;
using System.Collections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SmartxAPI.Controllers

{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("fnYear")]
    [ApiController]
    public class Acc_FnYear : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;

        public Acc_FnYear(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }
        [AllowAnonymous]
        [HttpGet("list")]
        public ActionResult GetCountryList(int nCompanyId,bool nProcessed)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            string sqlCommandText=" ";
            Params.Add("@nCompanyID",myFunctions.GetCompanyID(User));
          if (nProcessed == true)
            {
              sqlCommandText = "Select * from Acc_FnYear Where Acc_FnYear.B_TransferProcess=1 and Acc_FnYear.N_CompanyID=@nCompanyId";
            }
         else
           {
            sqlCommandText = "Select * from Acc_FnYear Where Acc_FnYear.N_CompanyID=@nCompanyID";
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
                    return Ok(_api.Success(dt));
                }

            }
            catch (Exception e)
            {
                return Ok( _api.Error(User,e));
            }
        }


    }
}