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
    [Route("invOnlineStockAllocation")]
    [ApiController]

    public class Inv_OnlineStockAllocation : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int FormID;
         private readonly IMyAttachments myAttachments;


        public Inv_OnlineStockAllocation(IDataAccessLayer dl, IApiFunctions api, IMyFunctions myFun, IConfiguration conf,IMyAttachments myAtt)
        {
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 1563;
             myAttachments = myAtt;

        }


          [HttpGet("list")]
        public ActionResult GetInvOnlineStockAllocation(int? nCompanyId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable dt = new DataTable();
                    DataTable CountTable = new DataTable();
                    SortedList Params = new SortedList();
                    DataSet dataSet = new DataSet();
                    string sqlCommandText = "";
                    string sqlCommandCount = "";
                    string Searchkey = "";

                    int nUserID = myFunctions.GetUserID(User);
                      if (xSearchkey != null && xSearchkey.Trim() != "")
                      Searchkey = "and (X_StoreCode like'%" + xSearchkey + "%'or X_StoreName like'%" + xSearchkey + "%')";

                       if (xSortBy == null || xSortBy.Trim() == "")
                        xSortBy = " order by N_StoreID desc";
                       else
                        xSortBy = " order by " + xSortBy;

                    
                    int Count = (nPage - 1) * nSizeperpage;
                    if (Count == 0)
                        sqlCommandText = "select top(" + nSizeperpage + ") [X_StoreCode] AS X_StoreCode,* from Inv_OnlineStore where N_CompanyID=@p1" + Searchkey + " " + xSortBy;
                    else
                        sqlCommandText = "select top(" + nSizeperpage + ") [X_StoreCode] AS X_StoreCode,* from Inv_OnlineStore where N_CompanyID=@p1" + Searchkey + " and N_StoreID not in (select top(" + Count + ") N_StoreID from Inv_OnlineStore where N_CompanyID=@p1" + xSortBy + " ) " + xSortBy;

                    // sqlCommandText = "select * from Inv_MRNDetails where N_CompanyID=@p1";
                    Params.Add("@p1", nCompanyId);
                  //  Params.Add("@p2", nFnYearId);
                    SortedList OutPut = new SortedList();


                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(*) as N_Count from Inv_OnlineStore where N_CompanyID=@p1 " + Searchkey + "";
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
                return Ok(_api.Error(User, e));
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
                    DataTable DetailsToImport;
                    DataTable Generaltable;
                    DataTable MappingRule = new DataTable();
                    MasterTable = ds.Tables["master"];
                    DetailTable = ds.Tables["details"];
                    Generaltable = ds.Tables["general"];
                    DataRow MasterRow = MasterTable.Rows[0];
                    //DataRow DetailRow = DetailTable.Rows[0];
                    SortedList Params = new SortedList();
                     DetailsToImport = ds.Tables["detailsImport"];
                     bool B_isImport = false;

                    if (ds.Tables.Contains("detailsImport") && DetailsToImport.Rows.Count > 0)
                    B_isImport = true;
                    int nStoreID = myFunctions.getIntVAL(MasterRow["N_StoreID"].ToString());
                    // int nStoreDetailID = myFunctions.getIntVAL(DetailRow["N_StoreDetailID"].ToString());
                    int nFnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearID"].ToString());
                    int nCompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyID"].ToString());
                    string xStoreCode = MasterRow["x_StoreCode"].ToString();
                    bool isRuleBasedImport = false;

                    if (Generaltable.Columns.Contains("N_RuleID")) //Checking if it's rule bases import
                    isRuleBasedImport = true;
                    string x_StoreCode = "";
                    if (xStoreCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", this.FormID);
                        x_StoreCode = dLayer.GetAutoNumber("Inv_OnlineStore", "x_StoreCode", Params, connection, transaction);
                        if (x_StoreCode == "")
                        {
                            transaction.Rollback();
                            return Ok("Unable to generate bin transfer Code");
                        }
                        MasterTable.Rows[0]["x_StoreCode"] = x_StoreCode;
                    }
                    
                    else
                    {
                          if (!isRuleBasedImport)
                        {
                         dLayer.DeleteData("Inv_OnlineStore", "N_StoreID", nStoreID, "", connection,transaction);
                         dLayer.DeleteData("Inv_OnlineStoreDetail", "N_StoreID", nStoreID, "", connection,transaction);
                        }
                    }
                       
                     if (isRuleBasedImport)
                        {
                            int RuleID = myFunctions.getIntVAL(Generaltable.Rows[0]["n_RuleID"].ToString());
                            SortedList ruleParams = new SortedList();
                            ruleParams.Add("@nCompanyID", nCompanyID);
                            ruleParams.Add("@nRuleID", RuleID);
                            MappingRule = dLayer.ExecuteDataTable("select X_FieldName,X_ColumnRefName from Gen_ImportRuleDetails where N_CompanyID=@nCompanyID and N_RuleID=@nRuleID", ruleParams, connection, transaction);
                      
                    MasterTable.Columns.Remove("x_EntryFrom");
                    MasterTable.Columns.Remove("x_TransType"); }
                     MasterTable.Columns.Remove("n_FnYearID");
                    int n_StoreID = dLayer.SaveData("Inv_OnlineStore", "N_StoreID", "", "", MasterTable, connection, transaction);
                    if (n_StoreID <= 0)
                    {
                        transaction.Rollback();
                        return Ok("Unable to save online stock ");
                    }

                    if (B_isImport)
                        {
                            // foreach (DataColumn col in DetailsToImport.Columns)
                            // {
                            //     col.ColumnName = col.ColumnName.Replace(" ", "_");
                            //     col.ColumnName = col.ColumnName.Replace("*", "");
                            //     col.ColumnName = col.ColumnName.Replace("/", "_");
                            // }

                          
                         
                              // Mapping Rule Configuration
                       
                        if (DetailsToImport.Rows.Count > 0)
                        {

                          string FieldList = "";
                            string FieldValuesArray = "";
                            string IDFieldName = "PKey_Code";
                            int rowCount = 0;
                            int totalCount = 0;
                            DetailsToImport.Columns.Add("N_CompanyID");
                            DetailsToImport.Columns.Add("Pkey_Code");
                            DetailsToImport.Columns.Add("N_StoreID");
                            foreach (DataRow dtRow in DetailsToImport.Rows)
                            {
                                
                                dtRow["N_CompanyID"] = nCompanyID;
                                dtRow["N_StoreID"] = nStoreID;
                                dtRow["Pkey_Code"] = 0;


                            }
                                 for (int j = 0; j < DetailsToImport.Rows.Count; j++)
                            {
                                rowCount = rowCount + 1;
                                string FieldValues = "";
                                for (int k = 0; k < DetailsToImport.Columns.Count; k++)
                                {
                                    var values = "";
                                    if (DetailsToImport.Columns[k].ColumnName.ToString().ToLower() == IDFieldName.ToLower())
                                    {
                                        values = (j + 1).ToString();
                                    }
                                    else
                                    {
                                        if (DetailsToImport.Rows[j][k] == DBNull.Value)
                                        {
                                            values = "";

                                        }
                                        else
                                        {
                                            values = DetailsToImport.Rows[j][k].ToString();
                                        }
                                        values = values.Replace("|", " ");
                                    }


                                    if (isRuleBasedImport) // In Case of Mapping Rule
                                    {
                                        DataRow[] RuleRow = MappingRule.Select("X_ColumnRefName = '" + DetailsToImport.Columns[k].ColumnName.ToString() + "'");
                                        if (RuleRow.Length > 0)
                                        {
                                            FieldValues = FieldValues + "|" + values;
                                            if (j == 0)
                                                FieldList = FieldList + "," + RuleRow[0]["X_FieldName"].ToString().Replace(" ", "_").Replace("*", "").Replace("/", "_");
                                        }
                                        else if (DetailsToImport.Columns[k].ColumnName.ToString() == "N_CompanyID" || DetailsToImport.Columns[k].ColumnName.ToString() == "PKey_Code" || DetailsToImport.Columns[k].ColumnName.ToString() == "N_StoreID")//System Defined fields exception
                                        {
                                            FieldValues = FieldValues + "|" + values;
                                            if (j == 0)
                                                FieldList = FieldList + "," + DetailsToImport.Columns[k].ColumnName.ToString();
                                        }
                                    }
                                    else
                                    {
                                        FieldValues = FieldValues + "|" + values;
                                        if (j == 0)
                                            FieldList = FieldList + "," + DetailsToImport.Columns[k].ColumnName.ToString();
                                    }

                                }

                                 if (j == 0)
                                {
                                    FieldList = FieldList.Substring(1);
                                }

                                FieldValues = ValidateString(FieldValues.Substring(1));

                                FieldValuesArray = FieldValuesArray + ",(" + FieldValues + ")";
                                if (rowCount == 1000 || DetailsToImport.Rows.Count == (totalCount + rowCount))
                                {
                                    totalCount = totalCount + rowCount;
                                    FieldValuesArray = FieldValuesArray.Substring(1);
                                    string inserStript = "insert into Mig_OnlineStore (" + FieldList + ") values" + FieldValuesArray;
                                    dLayer.ExecuteNonQuery(inserStript, connection, transaction);
                                    FieldValuesArray = "";
                                    rowCount = 0;
                                }
                            }
                        }
                           

                            //dLayer.SaveData("Mig_OnlineStore", "Pkey_Code", "", "", DetailsToImport, connection, transaction);

                            SortedList ProParam = new SortedList();
                            ProParam.Add("N_CompanyID", nCompanyID);
                            ProParam.Add("n_PkeyID", n_StoreID);
                         
                            ProParam.Add("X_Type", "Online Stock Allocation");
                            DetailTable = dLayer.ExecuteDataTablePro("SP_ScreenDataImport", ProParam, connection,transaction);



                        }

                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {
                        DetailTable.Rows[j]["n_StoreID"] = n_StoreID;
                    }
                     if(DetailTable.Rows.Count>0)
                     {
                     int n_StoreDetailID = dLayer.SaveData("Inv_OnlineStoreDetail", "N_StoreDetailID", DetailTable, connection, transaction);
                     if (n_StoreDetailID <= 0)
                     {
                        transaction.Rollback();
                        return Ok("Unable to save online stock");
                     }


                     }
                   




                    transaction.Commit();
                    SortedList Result = new SortedList();
                    Result.Add("n_StoreID", n_StoreID);
                    Result.Add("x_StoreCode", x_StoreCode);
                    // Result.Add("n_StoreDetailID", n_StoreDetailID);

                    return Ok(_api.Success(Result, "Online Stock Allocation Created"));
                }
            }
            
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

     public string ValidateString(string InputString)
        {
            string OutputString = InputString.Replace("'", "''");
            OutputString = OutputString.Replace("|", "','");
            OutputString = "'" + OutputString + "'";
            return OutputString;
        }
//    [HttpGet("exportData")]
//         public ActionResult GetExportDetails(string nStoreID,int nCompanyID, int nRuleID)
//         {
//             DataTable Master = new DataTable();
//             DataTable Detail = new DataTable();
//             DataSet ds = new DataSet();
//             SortedList Params = new SortedList();
//             SortedList QueryParams = new SortedList();
//             DataTable Attachments = new DataTable();
//             int companyid = myFunctions.GetCompanyID(User);

//             QueryParams.Add("@nCompanyID", nCompanyID);

           
//             string Condition = "";
//             string _sqlQuery = "";
//             try
//             {
//                 using (SqlConnection connection = new SqlConnection(connectionString))
//                 {
//                     connection.Open();



//                      if (isRuleBasedExport)
//                         {
                          
//                             SortedList ruleParams = new SortedList();
//                             ruleParams.Add("@nCompanyID", nCompanyID);
//                             ruleParams.Add("@nRuleID", RuleID);
//                             MappingRule = dLayer.ExecuteDataTable("select X_FieldName,X_ColumnRefName from Gen_ImportRuleDetails where N_CompanyID=@nCompanyID and N_RuleID=@nRuleID", ruleParams, connection, transaction);
//                         }

//                             //string FieldList = "";
//                             // string FieldValuesArray = "";
//                             // string IDFieldName = "PKey_Code";
//                             // int rowCount = 0;
//                             // int totalCount = 0;
//                                         //DataRow[] RuleRow = MappingRule.Select("X_ColumnRefName = '" + DetailsToImport.Columns[k].ColumnName.ToString() + "'");
//                                         // if (RuleRow.Length > 0)
//                                         // {
//                                         //     FieldValues = FieldValues + "|" + values;
//                                         //     if (j == 0)
//                                         //         FieldList = FieldList + "," + RuleRow[0]["X_FieldName"].ToString().Replace(" ", "_").Replace("*", "").Replace("/", "_");
//                                         // }
//                                         // else if (DetailsToImport.Columns[k].ColumnName.ToString() == "N_CompanyID" || DetailsToImport.Columns[k].ColumnName.ToString() == "PKey_Code" || DetailsToImport.Columns[k].ColumnName.ToString() == "N_StoreID")//System Defined fields exception
//                                         // {
//                                         //     FieldValues = FieldValues + "|" + values;
//                                         //     if (j == 0)
//                                         //         FieldList = FieldList + "," + DetailsToImport.Columns[k].ColumnName.ToString();
//                                         // }
                                    
//                                     // else
//                                     // {
//                                     //     FieldValues = FieldValues + "|" + values;
//                                     //     if (j == 0)
//                                     //         FieldList = FieldList + "," + DetailsToImport.Columns[k].ColumnName.ToString();
//                                     // }

                     
//                          _sqlQuery = "Select * from vw_Inv_OnlineStoreDetail Where N_CompanyID=@nCompanyID and N_StoreID=@nStoreID";
                        
//                         Detail = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);

//                         Detail = _api.Format(Detail, "details");
//                         if (Detail.Rows.Count == 0)
//                         {
//                             return Ok(_api.Notice("No Results Found"));
//                         }
//                         ds.Tables.Add(Detail);


//                         return Ok(_api.Success(ds));
//                     }


//                 }


//             }
//             catch (Exception e)
//             {
//                 return Ok(_api.Error(User, e));
//             }
//         }

         [HttpGet("details")]
        public ActionResult GetDetails(string xStoreCode,int nCompanyID, int nBranchID, bool bShowAllBranchData)
        {
            DataTable Master = new DataTable();
            DataTable Detail = new DataTable();
            DataSet ds = new DataSet();
            SortedList Params = new SortedList();
            SortedList QueryParams = new SortedList();
            DataTable Attachments = new DataTable();
            int companyid = myFunctions.GetCompanyID(User);

            QueryParams.Add("@nCompanyID", nCompanyID);

            QueryParams.Add("@nBranchID", nBranchID);
           // QueryParams.Add("@nFnYearID", nFnYearID);
            string Condition = "";
            string _sqlQuery = "";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();


                    if (xStoreCode != "" && xStoreCode != null)
                    {
                        QueryParams.Add("@xStoreCode", xStoreCode);
                        _sqlQuery = "Select * from Inv_OnlineStore Where n_Companyid=@nCompanyID and X_StoreCode =@xStoreCode";
                    }
                    else
                    {
                        QueryParams.Add("@xStoreCode", xStoreCode);
                        if (bShowAllBranchData == true)
                            Condition = " n_Companyid=@nCompanyID and X_StoreCode =@xStoreCode";
                        else
                            Condition = " n_Companyid=@nCompanyID and X_StoreCode =@xStoreCode and N_BranchID=@nBranchID";

                        _sqlQuery = "Select * from Inv_OnlineStore Where " + Condition + "";
                    }


                    Master = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);
                    

                    Master = _api.Format(Master, "master");

                    if (Master.Rows.Count == 0)
                    {
                        return Ok(_api.Notice("No Results Found"));
                    }
                    else
                    {

                        ds.Tables.Add(Master);
                        if (xStoreCode != null)
                        {
                            QueryParams.Add("@nStoreID", Master.Rows[0]["N_StoreID"].ToString());

                            _sqlQuery = "Select * from vw_Inv_OnlineStoreDetail Where N_CompanyID=@nCompanyID and N_StoreID=@nStoreID";

                        }
                        else
                        {
                            QueryParams.Add("@N_StoreID", Master.Rows[0]["N_StoreID"].ToString());

                            _sqlQuery = "Select * from vw_Inv_OnlineStoreDetail Where N_CompanyID=@nCompanyID and N_StoreID=@nStoreID";
                        }

                        Detail = dLayer.ExecuteDataTable(_sqlQuery, QueryParams, connection);

                        Detail = _api.Format(Detail, "details");
                        if (Detail.Rows.Count == 0)
                        {
                            return Ok(_api.Notice("No Results Found"));
                        }
                        ds.Tables.Add(Detail);


                        return Ok(_api.Success(ds));
                    }


                }


            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }

          [HttpDelete("delete")]
        public ActionResult DeleteData(int nStoreID, int nCompanyID, int nFnYearID)
        {
            int Results = 0;
            try
            {
                SortedList QueryParams = new SortedList();
                QueryParams.Add("@nCompanyID", nCompanyID);
                QueryParams.Add("@nFnYearID", nFnYearID);
                QueryParams.Add("@nStoreID", nStoreID);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    Results = dLayer.DeleteData("Inv_OnlineStore", "N_StoreID", nStoreID, "", connection);

                    if (Results > 0)
                    {
                        dLayer.DeleteData("Inv_OnlineStoreDetail", "N_StoreID", nStoreID, "", connection);
                        return Ok(_api.Success("Online Stock deleted"));
                    }
                    else
                    {
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