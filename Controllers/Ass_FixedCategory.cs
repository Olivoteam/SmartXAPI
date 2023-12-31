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
    [Route("fixedassetcategory")]
    [ApiController]
    public class Ass_FixedCategory : ControllerBase
    {
        private readonly IApiFunctions api;
        
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int N_FormID = 128;

        public Ass_FixedCategory(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }


        [HttpGet("list")]
        public ActionResult FixedAssetList(int nFnYearId,int nPage,int nSizeperpage,string xSortBy,int nFormID,string xSearchkey,bool showAll)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            string sqlCommandCount = "";
            int Count= (nPage - 1) * nSizeperpage;
            string sqlCommandText ="";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearId);
            string Searchkey = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = " and (code like '%" + xSearchkey + "%' or category like '%" + xSearchkey + "%' or account like '%" + xSearchkey + "%')";
             
              if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_CategoryID desc";
            else
                xSortBy = " order by " + xSortBy;

             if(nFormID==1766){
             if(Count==0)
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_InvAssetCategory_Disp where N_CompanyID=@p1 and N_FnYearID=@p2 and N_FormID=1766"+ Searchkey + " "+xSortBy;
            else
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_InvAssetCategory_Disp where N_CompanyID=@p1 and N_FormID=1766 and N_CategoryID not in (select top("+ Count +") N_CategoryID fromvw_InvAssetCategory_Disp  where N_CompanyID=@p1 )" + xSortBy;
             }   

            else if (showAll==true){
                 if(Count==0)
                sqlCommandText = "select * from vw_InvAssetCategory_Disp where N_CompanyID=@p1 and N_FnYearID=@p2 and ISNULL(N_FormID,0)=0"+ xSortBy;
            else
                sqlCommandText = "select * from vw_InvAssetCategory_Disp where N_CompanyID=@p1 and ISNULL(N_FormID,0)=0 and N_CategoryID not in (select top("+ Count +") N_CategoryID fromvw_InvAssetCategory_Disp  where N_CompanyID=@p1 )" + xSortBy; 
             }

             else{
             if(Count==0)
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_InvAssetCategory_Disp where N_CompanyID=@p1 and N_FnYearID=@p2 and ISNULL(N_FormID,0)=0"+ Searchkey + " "+xSortBy;
            else
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_InvAssetCategory_Disp where N_CompanyID=@p1 and ISNULL(N_FormID,0)=0 and N_CategoryID not in (select top("+ Count +") N_CategoryID fromvw_InvAssetCategory_Disp  where N_CompanyID=@p1 )" + Searchkey + " "+xSortBy;
             }

            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);

                    sqlCommandCount = "select count(1) as N_Count  from vw_InvAssetCategory_Disp where N_CompanyID=@p1 ";
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



//  [HttpGet("details")]
//         public ActionResult FixedAssetListDetails(string xFixedAssetNo , int nLangID)
//         {
//             DataTable dt = new DataTable();
//             SortedList Params = new SortedList();
//             int nCompanyId=myFunctions.GetCompanyID(User);
//             int N_Flag = 0;
//             if(nLangID ==2)
//             {
//                 N_Flag= 1;
//             }
//           string sqlCommandText = "SP_Inv_AssetItemcategory_Disp  " +CompanyID + "," + FnYearID + "," + N_Flag + ""
//            // string sqlCommandText = "select * from vw_InvAssetCategory_Disp where N_CompanyID=@p1 and Code=@p3";
//             Params.Add("@p1", nCompanyId);
//               Params.Add("@p2", nCompanyId);
//             Params.Add("@p3",xFixedAssetNo );
//             try
//             {
//                 using (SqlConnection connection = new SqlConnection(connectionString))
//                 {
//                     connection.Open();
//                     dt = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);
//                 }
//                 dt = api.Format(dt);
//                 if (dt.Rows.Count == 0)
//                 {
//                     return Ok(api.Warning("No Results Found"));
//                 }
//                 else
//                 {
//                     return Ok(api.Success(dt));
//                 }
//             }
//             catch (Exception e)
//             {
//                 return BadRequest(api.Error(User,e));
//             }
//         }



 [HttpGet("details")]
        public ActionResult FixedAssetListDetails(int nCategoryID, int nFnYearID, int? nLangID, int nFormID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            int N_Flag = 0;
            string sqlCommandText ="";
            if (nLangID == 2)
            {
                N_Flag = 1;
            }
            if(nFormID==1766){
             sqlCommandText = "select * from Vw_SchBookCategory where N_CompanyID= " + myFunctions.GetCompanyID(User) +"";
             
            }
            else{
            sqlCommandText = "SP_Inv_AssetItemcategory_Disp  " + myFunctions.GetCompanyID(User) + "," + nFnYearID + "," + N_Flag + "";
            }
  
            Params.Add("@p1", nCompanyID);
            Params.Add("@p2", nFnYearID);
            Params.Add("@p3", nCategoryID);
            try
            {

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    DataTable output = new DataTable();
                     DataRow[] dr=dt.Select("N_CategoryID = " + nCategoryID + " and  N_CompanyID = " + nCompanyID + " and N_FnYearID=" + nFnYearID + "");
                    output = dr.CopyToDataTable();  
                    output = api.Format(output);
                    if (output.Rows.Count == 0)
                        return Ok(api.Warning("No Results Found"));
                    else
                        return Ok(api.Success(output));
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
                int nCategoryID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_CategoryID"].ToString());

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    // Auto Gen
                    string CategoryCode = "";
                    var values = MasterTable.Rows[0]["X_CategoryCode"].ToString();
                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearId);
                        Params.Add("N_FormID", this.N_FormID);
                        CategoryCode = dLayer.GetAutoNumber("Ass_AssetCategory", "X_CategoryCode", Params, connection, transaction);
                        if (CategoryCode == "") { transaction.Rollback();return Ok(api.Error(User,"Unable to generate Category Code")); }
                        MasterTable.Rows[0]["X_CategoryCode"] = CategoryCode;
                    }
                   if (MasterTable.Columns.Contains("x_DeprCalculation"))
                    {
                        MasterTable.Columns.Remove("x_DeprCalculation");
                      
                    }
                    if (MasterTable.Columns.Contains("x_TypeName"))
                    {
                        MasterTable.Columns.Remove("x_TypeName");
                      
                    }

                    string  DupCriteria = "X_CategoryCode='" + values + "' and N_CompanyID=" + nCompanyID;
                    string  X_Criteria = "N_CompanyID=" + nCompanyID + " and N_FnYearID=" + nFnYearId;
                    nCategoryID = dLayer.SaveData("Ass_AssetCategory", "N_CategoryID",DupCriteria,X_Criteria,MasterTable, connection, transaction);
                    
                    
                    if (nCategoryID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User,"Unable to save"));
                    }
                    else
                    {
                        transaction.Commit();
                        return Ok(api.Success("Fixed Asset Created"));
                    }
                }
            }
            catch (Exception ex)
            {
                return BadRequest(api.Error(User,ex));
            }
        }

      
        [HttpDelete("delete")]
        public ActionResult DeleteData(int nCategoryID)
        {

             int Results = 0;
            try
            {                        
                SortedList Params = new SortedList();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                     if ( nCategoryID > 0)
                    {
                        object catCount = dLayer.ExecuteScalar("select count(1) From Ass_PurchaseDetails where n_CategoryID =" + nCategoryID + " and N_CompanyID =" + myFunctions.GetCompanyID(User), connection, transaction);
                        catCount = catCount == null ? 0 : catCount;
                        if (myFunctions.getIntVAL(catCount.ToString()) > 0){
                            return Ok(api.Error(User, "Already In Use !!"));
                        }

                        object categoryCount = dLayer.ExecuteScalar("select count(1) From Ass_AssetMaster where n_CategoryID =" + nCategoryID + " and N_CompanyID =" + myFunctions.GetCompanyID(User), connection, transaction);
                        categoryCount = categoryCount == null ? 0 : categoryCount;
                        if (myFunctions.getIntVAL(categoryCount.ToString()) > 0){
                            return Ok(api.Error(User, "Already In Use !!"));
                        }
                    }


                    Results = dLayer.DeleteData("Ass_AssetCategory", "N_CategoryID", nCategoryID, "", connection, transaction);
                    transaction.Commit();
                }
                if (Results > 0)
                {
                    Dictionary<string,string> res=new Dictionary<string, string>();
                    res.Add("N_CategoryID",nCategoryID.ToString());
                    return Ok(api.Success(res,"Fixed Asset deleted"));
                }
                else
                {
                    return Ok(api.Error(User,"Unable to delete Fixed Asset"));
                }

            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }



        }
    }
}