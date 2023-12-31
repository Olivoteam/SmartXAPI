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
    [Route("approvalcodes")]
    [ApiController]
    public class Gen_Approvalcodes : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly int FormID;
        private readonly IMyFunctions myFunctions;

        public Gen_Approvalcodes(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 1056;
        }
        private readonly string connectionString;


        [HttpGet("usercategorylist")]
        public ActionResult GetUser(int nCompanyId)
        {
            DataTable dt = new DataTable();
            //test
            // int abc=0;
            SortedList Params = new SortedList();
            //int nCompanyId=myFunctions.GetCompanyID(User);
            string sqlCommandText = "select * from Sec_UserCategory where N_CompanyID=@p1 and x_Usercategory IS NOT NULL";
            Params.Add("@p1", nCompanyId);


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    connection.Open();
                }
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else { return Ok(_api.Success(dt)); }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User,e));
            }
        }

        [HttpGet("userlist")]
        public ActionResult GetUserlist(int nCompanyId)//,int nCategoryID,int nAnyUserUsed)
        {
            DataTable dt = new DataTable(); 
            SortedList Params = new SortedList();

            string sqlCommandText = "";
            
            // if(nAnyUserUsed==0)
            // {
            //     if(nCategoryID!=0)
            //         sqlCommandText ="select * from vw_UserList_levelSettings where N_CompanyID=@p1 and N_UserCategoryID in (@p2,-11,-22) and B_Active=1 and N_UserCategoryID<>1";
            //     else
            //         sqlCommandText ="select * from vw_UserList_levelSettings where N_CompanyID=@p1 and N_UserCategoryID >= -24 and B_Active=1 and N_UserCategoryID<>1";
            // }
            // else
            // {
            //     if(nCategoryID!=0)
            //         sqlCommandText ="select * from vw_UserList_levelSettings where N_CompanyID=@p1 and (N_UserCategoryID=@p2 OR N_UserCategoryID<= -22) and B_Active=1 and N_UserCategoryID<>1";
            //     else
             //       sqlCommandText ="select * from vw_UserList_levelSettings where N_CompanyID=@p1 and N_UserCategoryID <>-11 and B_Active=1 and N_UserCategoryID<>1";
            //}

             sqlCommandText ="select * from vw_UserList_levelSettings where N_CompanyID=@p1 and B_Active=1 and N_UserCategoryID<>1";

            Params.Add("@p1", nCompanyId);
            // Params.Add("@p2", nCategoryID);


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    connection.Open();
                }
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else { return Ok(_api.Success(dt)); }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User,e));
            }
        }


        [HttpGet("actionlist")]
        public ActionResult GetActionList()
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            string sqlCommandText = "select * from gen_defaults where n_DefaultId=33 order by N_Sort asc";
            //Params.Add("@p1", nDefaultId);
            //Params.Add("@p1", nTypeId);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    connection.Open();
                }
                dt = _api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Results Found"));
                }
                else { return Ok(_api.Success(dt)); }
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
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    DataTable MasterTable;
                    DataTable DetailTable;
                    string DocNo = "";
                    MasterTable = ds.Tables["master"];
                    DetailTable = ds.Tables["details"];
                    DataRow MasterRow = MasterTable.Rows[0];
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyID"].ToString());
                    int nApprovalID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_ApprovalID"].ToString());
                    int nFnYearID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_FnYearID"].ToString());
                    string X_ApprovalCode = MasterTable.Rows[0]["X_ApprovalCode"].ToString();
                    MasterTable.Columns.Remove("N_FnYearID");

                    if (nApprovalID > 0)
                    {
                         dLayer.DeleteData("Gen_ApprovalCodesDetails", "N_ApprovalID", nApprovalID, "", connection,transaction);
                         dLayer.DeleteData("Gen_ApprovalCodes", "N_ApprovalID", nApprovalID, "", connection,transaction);
                    }
                   
                    DocNo = MasterRow["X_ApprovalCode"].ToString();
                    
                    if (X_ApprovalCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_FormID", this.FormID);
                        Params.Add("N_YearID", nFnYearID);

                        while (true)
                        {
                            DocNo = dLayer.ExecuteScalarPro("SP_AutoNumberGenerate", Params, connection, transaction).ToString();
                            object N_Result = dLayer.ExecuteScalar("Select 1 from Gen_ApprovalCodes Where X_ApprovalCode ='" + DocNo + "' and N_CompanyID= " + nCompanyID, connection, transaction);
                            if (N_Result == null)
                                break;
                        }
                        X_ApprovalCode = DocNo;

                        //TEST
                        if (X_ApprovalCode == "") { transaction.Rollback(); return Ok(_api.Error(User,"Unable to generate Approval Code")); }
                        MasterTable.Rows[0]["X_ApprovalCode"] = X_ApprovalCode;

                    }

                    nApprovalID = dLayer.SaveData("Gen_ApprovalCodes", "N_ApprovalID", MasterTable, connection, transaction);
                    if (nApprovalID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User,"Unable To Save"));
                    }

                    for (int i = 0; i < DetailTable.Rows.Count; i++)
                    {
                        DetailTable.Rows[i]["N_ApprovalID"] = nApprovalID;
                    }
                    int N_ApprovalDetailsID = dLayer.SaveData("Gen_ApprovalCodesDetails", "N_ApprovalDetailsID", DetailTable, connection, transaction);
                    if (N_ApprovalDetailsID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User,"Unable To Save"));
                    }
                    transaction.Commit();
                    SortedList Result = new SortedList();
                    Result.Add("N_ApprovalID", nApprovalID);
                    Result.Add("N_ApprovalDetailsID", N_ApprovalDetailsID);

                    return Ok(_api.Success(Result, "Saved"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

        [HttpGet("details")]
        public ActionResult GenApprovalCode(string xApprovalCode,int nApproovalID)
        {


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataSet dt = new DataSet();
                    SortedList Params = new SortedList();
                    DataTable MasterTable = new DataTable();
                    DataTable DetailTable = new DataTable();
                    DataTable DataTable = new DataTable();

                    string Mastersql = "";
                    string DetailSql = "";

                    Params.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    Params.Add("@xApprovalCode", xApprovalCode);
                    Mastersql = "select * from Gen_ApprovalCodes where N_CompanyId=@nCompanyID and X_ApprovalCode=@xApprovalCode  ";

                    MasterTable = dLayer.ExecuteDataTable(Mastersql, Params, connection);
                    if (MasterTable.Rows.Count == 0) { return Ok(_api.Warning("No data found")); }
                    int ApproovalID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_ApprovalID"].ToString());
                    Params.Add("@nApproovalID", ApproovalID);

                    MasterTable = _api.Format(MasterTable, "Master");
                    DetailSql = "select * from vw_ApprovalCodeDetails where N_CompanyId=@nCompanyID and N_ApprovalID=@nApproovalID order by N_level asc";
                    DetailTable = dLayer.ExecuteDataTable(DetailSql, Params, connection);
                    DetailTable = _api.Format(DetailTable, "Details");
                    dt.Tables.Add(MasterTable);
                    dt.Tables.Add(DetailTable);
                    return Ok(_api.Success(dt));


                }

            }
            catch (Exception e)
            {
                return Ok(_api.Error(User,e));
            }
        }
        [HttpGet("dashboardlist")]
        public ActionResult GenApprovalList(int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_ApprovalDescription like '%"+ xSearchkey +"%' or X_ApprovalCode like '%"+ xSearchkey +"%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_ApprovalID desc";
            else
                xSortBy = " order by " + xSortBy;


            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from Gen_ApprovalCodes where N_CompanyID=@p1 " + Searchkey +" "+ xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from Gen_ApprovalCodes where N_CompanyID=@p1 " + Searchkey +" and  N_ApprovalID not in (select top(" + Count + ") N_ApprovalID from Gen_ApprovalCodes  where N_CompanyID=@p1 " +Searchkey + xSortBy + ") "  + Searchkey +" "+ xSortBy;

            Params.Add("@p1", nCompanyId);
            // Params.Add("@nFnYearId", nFnYearId);
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(1) as N_Count  from Gen_ApprovalCodes where N_CompanyID=@p1 ";
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
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

        [HttpDelete("delete")]
        public ActionResult DeleteData(int nApprovalID)
        {
            int Results = 0;
             int nCompanyID=myFunctions.GetCompanyID(User);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    
                    connection.Open();
                     SqlTransaction transaction = connection.BeginTransaction();
                     if ( nApprovalID > 0)
                    {
                        object appCount = dLayer.ExecuteScalar("select count(1) From Sec_ApprovalSettings_EmployeeDetails where N_ApprovalID =" + nApprovalID + " and N_CompanyID =" + nCompanyID, connection, transaction);
                        appCount = appCount == null ? 0 : appCount;
                        if (myFunctions.getIntVAL(appCount.ToString()) > 0){
                            return Ok(_api.Error(User, "Already In Use !!"));
                        }
                    }
                    if ( nApprovalID > 0)
                    {
                        object appCount = dLayer.ExecuteScalar("select count(1) From Sec_ApprovalSettings_General where N_ApprovalID =" + nApprovalID + " and N_CompanyID =" + nCompanyID, connection, transaction);
                        appCount = appCount == null ? 0 : appCount;
                        if (myFunctions.getIntVAL(appCount.ToString()) > 0){
                            return Ok(_api.Error(User, "Already In Use !!"));
                        }
                    }
                    Results = dLayer.DeleteData("Gen_ApprovalCodes", "N_ApprovalID", nApprovalID,"N_CompanyID =" + nCompanyID , connection,transaction);
                    if (Results > 0)
                    {                      
                        dLayer.DeleteData("Gen_ApprovalCodesDetails", "N_ApprovalID", nApprovalID, "N_CompanyID =" + nCompanyID , connection,transaction);
                        transaction.Commit();
                        return Ok(_api.Success("Approval Code deleted"));
                    }
                    else
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User,"Unable to delete"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

    }
}
