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
    [Route("disciplinaryAction")]
    [ApiController]
    public class Pay_DisciplinaryAction : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int N_FormID = 985;


        public Pay_DisciplinaryAction(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }

        [HttpGet("list")]
        public ActionResult GetAllDisciplinaryAction(int nFnYearId,int nComapanyId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_ActionCode like '%" + xSearchkey + "%' or X_EmpName like '%" + xSearchkey + "%' or X_TypeName like '%" + xSearchkey + "%' or cast(D_Date as VarChar) like '%" + xSearchkey + "%'or X_Reason like '%" + xSearchkey + "%' or X_Investigation like '%" + xSearchkey + "%' or N_Penalty like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by X_ActionCode desc";
            else
            
             xSortBy = " order by " + xSortBy;
            


            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from Vw_Pay_displinaryAction where N_CompanyID=@p1 " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from Vw_Pay_displinaryAction where N_CompanyID=@p1 " + Searchkey + " and N_ActionID not in (select top(" + Count + ") N_ActionID from Vw_Pay_displinaryAction where N_CompanyID=@p1 and N_FnYearID=@p2" + xSearchkey + xSortBy + " ) " + xSortBy;

            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);
           
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount="select count(1) as N_Count from Vw_Pay_displinaryAction where N_CompanyId=@p1 and N_FnYearID=@p2 "+ Searchkey +" ";
                    DataTable Summary = dLayer.ExecuteDataTable(sqlCommandCount, Params, connection);
                    string TotalCount = "0";
                   
                    if (Summary.Rows.Count > 0)
                    {
                        DataRow drow = Summary.Rows[0];
                        TotalCount = drow["N_Count"].ToString();
                      
                    }
                    OutPut.Add("Details", _api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                   
                    if (dt.Rows.Count == 0)
                    {
                        return Ok(_api.Warning("No Results Found"));
                    }
                    else
                    {
                        return Ok(_api.Success(OutPut));
                    }
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User,e));
            }
        }
       


          [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                MasterTable = ds.Tables["master"];
                int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyId"].ToString());
                int nFnYearId = myFunctions.getIntVAL(MasterTable.Rows[0]["n_FnYearId"].ToString());
                int nActionId = myFunctions.getIntVAL(MasterTable.Rows[0]["n_ActionId"].ToString());
                 string xButtonAction="";
 
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    // Auto Gen
                    string ActionCode = "";
                    var values = MasterTable.Rows[0]["X_ActionCode"].ToString();
                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearId);
                        Params.Add("N_FormID", this.N_FormID);
                        
                        ActionCode = dLayer.GetAutoNumber("Pay_disciplinaryAction", "X_ActionCode", Params, connection, transaction);
                         xButtonAction="Insert"; 
                        if (ActionCode == "") { return Ok(_api.Error(User,"Unable to generate Action Code")); }
                        MasterTable.Rows[0]["X_ActionCode"] = ActionCode;
                        

                    }else {
                         xButtonAction="Update"; 
                    }
                     ActionCode = MasterTable.Rows[0]["X_ActionCode"].ToString();

            
                       

                

                   nActionId = dLayer.SaveData("Pay_disciplinaryAction", "n_ActionId", MasterTable, connection, transaction);
                             //Activity Log
                string ipAddress = "";
                if (  Request.Headers.ContainsKey("X-Forwarded-For"))
                    ipAddress = Request.Headers["X-Forwarded-For"];
                else
                    ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                       myFunctions.LogScreenActivitys(nFnYearId,nActionId,ActionCode,985,xButtonAction,ipAddress,"",User,dLayer,connection,transaction);
                       


                   if (nActionId <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User,"Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(_api.Success("Disciplinary Action  Information Saved"));
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(_api.Error(User,ex));
            }
        }

     [HttpGet("details")]
        public ActionResult GetDetails(int xActionCode,int nFnYearId)
        {
            DataTable dt=new DataTable();
            SortedList Params=new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            string sqlCommandText="select * from Vw_Pay_displinaryAction where N_CompanyID=@nCompanyID and X_ActionCode=@xActionCode and N_FnYearID=@p2";
            Params.Add("@nCompanyID",nCompanyID);
            Params.Add("@xActionCode",xActionCode);
            Params.Add("@p2", nFnYearId);

            try{
                using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        dt=dLayer.ExecuteDataTable(sqlCommandText,Params,connection); 
                    }
                    if(dt.Rows.Count==0)
                        {
                            return Ok(_api.Notice("No Results Found" ));
                        }else{
                            return Ok(_api.Success(dt));
                        }
            }catch(Exception e){
                return Ok(_api.Error(User,e));
            }
        }

          [HttpDelete("delete")]
        public ActionResult DeleteData(int nActionID,int nFnYearID,int nCompanyID)
        {
            int Results = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList ParamList = new SortedList();
                    DataTable TransData = new DataTable();
                    ParamList.Add("@nTransID", nActionID);
                    ParamList.Add("@nFnYearID", nFnYearID);
                    ParamList.Add("@nCompanyID", nCompanyID);
                    string xButtonAction="Delete";
                    string X_ActionCode="";
                    string Sql = "select N_ActionId,X_ActionCode from Pay_disciplinaryAction where N_ActionId=@nTransID and N_CompanyID=@nCompanyID ";
                   TransData = dLayer.ExecuteDataTable(Sql, ParamList, connection,transaction);
                    
                      if (TransData.Rows.Count == 0)
                    {
                        return Ok(_api.Error(User, "Transaction not Found"));
                    }
                    DataRow TransRow = TransData.Rows[0];
                               //  Activity Log
                string ipAddress = "";
                if (  Request.Headers.ContainsKey("X-Forwarded-For"))
                    ipAddress = Request.Headers["X-Forwarded-For"];
                else
                    ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                       myFunctions.LogScreenActivitys(myFunctions.getIntVAL( nFnYearID.ToString()),nActionID,TransRow["X_ActionCode"].ToString(),985,xButtonAction,ipAddress,"",User,dLayer,connection,transaction);
             

              
                    Results = dLayer.DeleteData("Pay_DisciplinaryAction", "n_ActionID", nActionID, "", connection,transaction);
                     transaction.Commit();
          
                  
                    if (Results > 0)
                    {
                        return Ok( _api.Success("Deleted Successfully"));
                    }
                    else
                    {
                        return Ok(_api.Error(User,"Unable to delete "));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

        
        [HttpGet("Datalist")]
        public ActionResult GetDisciplinarylist(int nEmpID,int nPage,int nSizeperpage)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            int Count = (nPage - 1) * nSizeperpage;
            int nCompanyID=myFunctions.GetCompanyID(User);
            Params.Add("@nCompanyID",nCompanyID);
            Params.Add("@nEmpID",nEmpID);

          

            string sqlCommandText="Select N_CompanyID,X_ActionCode,X_Reason,X_Investigation,X_TypeName,N_Penalty,N_EmpID,x_WarningDecision from Vw_Pay_displinaryAction Where N_CompanyID=@nCompanyID and N_EmpID=@nEmpID";
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
                return Ok(_api.Error(User,e));
            }
        }



  [HttpGet("Tablelist")]
        public ActionResult GetAllDisciplinary(int nEmpId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_ActionCode like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by X_ActionCode desc";
            else
            
             xSortBy = " order by " + xSortBy;
            


            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from Vw_Pay_displinaryAction where N_CompanyID=@p1 and N_EmpID=@p2 " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from Vw_Pay_displinaryAction where N_CompanyID=@p1 " + Searchkey + " and N_ActionID not in (select top(" + Count + ") N_ActionID from Vw_Pay_displinaryAction where N_CompanyID=@p1 and N_EmpID=@p2" + xSearchkey + xSortBy + " ) " + xSortBy;

            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nEmpId);
           
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount="select count(1) as N_Count from Vw_Pay_displinaryAction where N_CompanyId=@p1 and N_EmpID=@p2 "+ Searchkey +" ";
                    DataTable Summary = dLayer.ExecuteDataTable(sqlCommandCount, Params, connection);
                    string TotalCount = "0";
                   
                    if (Summary.Rows.Count > 0)
                    {
                        DataRow drow = Summary.Rows[0];
                        TotalCount = drow["N_Count"].ToString();
                      
                    }
                    OutPut.Add("Details", _api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                   
                    if (dt.Rows.Count == 0)
                    {
                        return Ok(_api.Warning("No Results Found"));
                    }
                    else
                    {
                        return Ok(_api.Success(OutPut));
                    }
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User,e));
            }
        }
       



      
    }
}