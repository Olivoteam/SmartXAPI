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
    [Route("projectunitFDT")]
    [ApiController]
    public class PrjFdt : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;

        private readonly int N_FormID =1287 ;


        public PrjFdt(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = 
            conf.GetConnectionString("SmartxConnection");
        }


        [HttpGet("details")]
        public ActionResult PrjFdtDetails(int nFDTID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId=myFunctions.GetCompanyID(User);
            string sqlCommandText = "select * from vw_FDT where N_CompanyID=@p1  and N_FDTID=@p3";
            Params.Add("@p1", nCompanyId);  
            Params.Add("@p3", nFDTID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);
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
                return BadRequest(api.Error(User,e));
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
                int nFDTid = myFunctions.getIntVAL(MasterTable.Rows[0]["N_FDTID"].ToString());

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    // Auto Gen
                    string FDTCode = "";
                    var values = MasterTable.Rows[0]["X_FDTCode"].ToString();
                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                         Params.Add("N_YearID", nFnYearId);
                        Params.Add("N_FormID", this.N_FormID);
                        FDTCode = dLayer.GetAutoNumber("prj_FDT", "X_FDTCode", Params, connection, transaction);
                        if (FDTCode == "") { transaction.Rollback();return Ok(api.Error(User,"Unable to generate project unit Code")); }
                        MasterTable.Rows[0]["X_FDTCode"] = FDTCode;
                    }
                    MasterTable.Columns.Remove("n_FnYearId");


                    nFDTid = dLayer.SaveData("prj_FDT", "N_FDTID", MasterTable, connection, transaction);
                    if (nFDTid <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User,"Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("Project Unit Created"));
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(api.Error(User,ex));
            }
        }





        [HttpGet("Projectslist") ]
        public ActionResult ProjectUnitList ()
        {    int nCompanyID=myFunctions.GetCompanyID(User);
  
            SortedList param = new SortedList(){{"@p1",nCompanyID}};
            
            DataTable dt=new DataTable();
            
            string sqlCommandText="select x_FDTName,n_FDTID from prj_FDT where N_CompanyID=@p1";
                
            try{
                    using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    dt=dLayer.ExecuteDataTable(sqlCommandText,param,connection);
                }
                    if(dt.Rows.Count==0)
                        {
                            return Ok(api.Notice("No Results Found"));
                        }else{
                            return Ok(api.Success(dt));
                        }
                
            }catch(Exception e){
                return Ok(api.Error(User,e));
            }   
        }
        
      
      [HttpGet("sor") ]
        public ActionResult sorList ()
        {    int nCompanyID=myFunctions.GetCompanyID(User);
  
            SortedList param = new SortedList(){{"@p1",nCompanyID}};
            
            DataTable dt=new DataTable();
            
            string sqlCommandText="select * from vw_ProjectSOR where N_CompanyID=@p1";
                
            try{
                    using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    dt=dLayer.ExecuteDataTable(sqlCommandText,param,connection);
                }
                    if(dt.Rows.Count==0)
                        {
                            return Ok(api.Notice("No Results Found"));
                        }else{
                            return Ok(api.Success(dt));
                        }
                
            }catch(Exception e){
                return Ok(api.Error(User,e));
            }   
        }
        
 [HttpGet("list")]
        public ActionResult PrjFdtList(int nPage,int nSizeperpage)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            string sqlCommandCount = "";
            int Count= (nPage - 1) * nSizeperpage;
            string sqlCommandText ="";
             
             if(Count==0)
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_ProjectSOR where N_CompanyID=@p1 ";
            else
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_ProjectSOR where N_CompanyID=@p1 and N_FDTID not in (select top("+ Count +") N_FDTID from vw_InvMainAssetCategory_Disp where N_CompanyID=@p1 )";
            Params.Add("@p1", nCompanyID);
            

            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);

                    sqlCommandCount = "select count(1) as N_Count  from vw_ProjectSOR where N_CompanyID=@p1 ";
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
                return BadRequest(api.Error(User,e));
            }
        }
      
        [HttpDelete("delete")]
        public ActionResult DeleteData(int nFDTid)
        {

             int Results = 0;
            try
            {                        
                SortedList Params = new SortedList();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    Results = dLayer.DeleteData("prj_FDT ", "N_FDTID", nFDTid, "", connection, transaction);
                    transaction.Commit();
                }
                if (Results > 0)
                {
                    Dictionary<string,string> res=new Dictionary<string, string>();
                    res.Add("N_FDTID",nFDTid.ToString());
                    return Ok(api.Success(res,"Project unit deleted"));
                }
                else
                {
                    return Ok(api.Error(User,"Unable to delete Project unit"));
                }

            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }



        }
    }
}

