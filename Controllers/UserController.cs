using SmartxAPI.Data;
using SmartxAPI.Dtos.Login;
using Microsoft.AspNetCore.Mvc;
using System;
using SmartxAPI.GeneralFunctions;
using System.Data;
using System.Collections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SmartxAPI.Controllers
{

    [Route("user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ISec_UserRepo _repository;
        private readonly IApiFunctions _api;

        private readonly IMyFunctions myFunctions;
        private readonly IDataAccessLayer dLayer;
        private readonly string connectionString;


        public UserController(ISec_UserRepo repository, IApiFunctions api, IMyFunctions myFun, IDataAccessLayer dl, IConfiguration conf)
        {
            _repository = repository;
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }
        [HttpPost("login")]
        public ActionResult Authenticate([FromBody] Sec_AuthenticateDto model)
        {
            try
            {
                string ipAddress = "";
                if (Request.Headers.ContainsKey("X-Forwarded-For"))
                    ipAddress = Request.Headers["X-Forwarded-For"];
                else
                    ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                //var password = myFunctions.EncryptString(model.Password);
                var password = model.Password;
                var user = _repository.Authenticate(model.CompanyName, model.Username, password, ipAddress);

                if (user == null) { return StatusCode(403, _api.Response(403, "Username or password is incorrect")); }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(403, _api.ErrorResponse(ex));
            }
        }

        //GET api/User/list?....
        [HttpGet("list")]
        public ActionResult GetUserList(int? nCompanyId, bool bAllBranchesData, string userid, string qry)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            string criteria = "";
            
            string sqlCommandText = "Sp_UserList";
            Params.Add("N_CompanyID", nCompanyId);
            Params.Add("N_UserId", userid);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTablePro(sqlCommandText, Params, connection);
                }
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                    return StatusCode(200, new { StatusCode = 200, Message = "No Results Found" });
                else
                {
                    dt.Columns.Remove("X_Password");
                    dt.AcceptChanges();
                    return Ok(dt);
                }

            }
            catch (Exception e)
            {
                return StatusCode(403, _api.ErrorResponse(e));
            }
        }
    }
}