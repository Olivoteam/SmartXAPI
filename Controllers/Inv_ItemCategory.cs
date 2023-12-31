using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System;
using SmartxAPI.GeneralFunctions;
using System.Data;
using System.Collections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("itemcategory")]
    [ApiController]
    public class Inv_ItemCategory : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;



        public Inv_ItemCategory(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }


        //GET api/productcategory/list?....
        [HttpGet("list")]
        public ActionResult GetItemCategory()
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);

            string sqlCommandText = "select Inv_ItemCategory.N_CompanyID,Inv_ItemCategory.N_CategoryID, Inv_ItemCategory.X_Category, isnull(Inv_ItemCategory.X_Category_Ar,Inv_ItemCategory.X_Category) as X_Category_Ar,Inv_ItemCategory.X_CategoryCode,Inv_ItemCategory.N_ParentCategoryID, Inv_ItemCategory_1.X_Category as X_ParentCategory from Inv_ItemCategory LEFT OUTER JOIN Inv_ItemCategory AS Inv_ItemCategory_1 ON Inv_ItemCategory.N_CompanyID = Inv_ItemCategory_1.N_CompanyID AND Inv_ItemCategory.N_ParentCategoryID = Inv_ItemCategory_1.N_CategoryID where Inv_ItemCategory.N_CompanyID=@p1";
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
                return Ok(_api.Error(User, e));
            }
        }

        [HttpGet("details")]
        public ActionResult GetItemCategoryDetails(int nFnYearID, int nCategoryId)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "Select TOP 1 *,Code as X_CategoryCode from vw_InvItemCategory Where N_CompanyID=@p1 and (N_FnYearID =@p3 or N_FnYearID is null ) and n_CategoryID=@p2 Order By N_CategoryID";

            Params.Add("@p1", myFunctions.GetCompanyID(User));
            Params.Add("@p2", nCategoryId);
            Params.Add("@p3", nFnYearID);


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                }
                if (dt.Rows.Count == 0)
                {
                    return Ok(_api.Notice("No Data Found"));
                }
                else
                {
                    return Ok(_api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }


        //Save....
        [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds, string xCategory)
        {
            try
            {
                DataTable MasterTable;
                MasterTable = ds.Tables["master"];
                string xButtonAction = "";
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    int N_CategoryID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_CategoryID"].ToString());
                    int N_FnYearId = myFunctions.getIntVAL(MasterTable.Rows[0]["N_FnYearId"].ToString());
                    int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_CompanyId"].ToString());
                    SortedList Params = new SortedList();
                    //  Auto Gen;
                    string CategoryCode = "";
                    var values = MasterTable.Rows[0]["X_CategoryCode"].ToString();
                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", MasterTable.Rows[0]["N_CompanyId"].ToString());
                        Params.Add("N_YearID", N_FnYearId);
                        Params.Add("N_FormID", 73);
                        CategoryCode = dLayer.GetAutoNumber("Inv_ItemCategory", "X_CategoryCode", Params, connection, transaction);
                        xButtonAction = "update";


                        if (CategoryCode == "")
                        {

                            // string ipAddress = "";
                            // if (Request.Headers.ContainsKey("X-Forwarded-For"))
                            //     ipAddress = Request.Headers["X-Forwarded-For"];
                            // else
                            //     ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                            // myFunctions.LogScreenActivitys(N_FnYearId, N_CategoryID, xCategory, 73, xButtonAction, ipAddress, "", User, dLayer, connection, transaction);
                            transaction.Rollback();
                            // SortedList Result = new SortedList();
                            // Result.Add("N_CategoryID", N_CategoryID);
                            // Result.Add("X_Category", xCategory);
                            return Ok(_api.Error(User, "Unable to generate Category Code"));
                        }
                        MasterTable.Rows[0]["X_CategoryCode"] = CategoryCode;
                    }
                    MasterTable.Columns.Remove("N_FnYearId");
                    //MasterTable.Columns.Remove("b_IsParent");
                    string X_Category = MasterTable.Rows[0]["X_Category"].ToString();
                    string DupCriteria = "X_Category='" + X_Category + "' and N_CompanyID=" + nCompanyID + "";
                    N_CategoryID = dLayer.SaveData("Inv_ItemCategory", "N_CategoryID", DupCriteria, "", MasterTable, connection, transaction);
                    if (N_CategoryID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to save...Category Name Exists"));
                    }

                    else

                    {
                        xButtonAction = "Insert";
                        string ipAddress = "";
                        if (Request.Headers.ContainsKey("X-Forwarded-For"))
                            ipAddress = Request.Headers["X-Forwarded-For"];
                        else
                            ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                        myFunctions.LogScreenActivitys(N_FnYearId, N_CategoryID, X_Category, 73, xButtonAction, ipAddress, "", User, dLayer, connection, transaction);
                        transaction.Commit();
                        SortedList Result = new SortedList();
                        Result.Add("N_CategoryID", N_CategoryID);
                        Result.Add("X_Category", X_Category);
                        return Ok(_api.Success("Financial Category Saved"));
                    }

                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }
        }

        [HttpDelete("delete")]
        public ActionResult DeleteData(int nCategoryID, int nFnyearID)
        {
            int Results = 0;
            int nCompanyID = myFunctions.GetCompanyID(User);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable TransData = new DataTable();
                    SortedList ParamList = new SortedList();
                    ParamList.Add("@nCategoryID", nCategoryID);
                    ParamList.Add("@nCompanyID", nCompanyID);

                    string Sql = "select X_CategoryCode,X_Category from Inv_ItemCategory where N_CategoryID=@nCategoryID and N_CompanyID=@nCompanyID";
                    object xCategory = dLayer.ExecuteScalar("Select X_Category From Inv_ItemCategory Where N_CategoryID=" + nCategoryID + " and N_CompanyID =" + myFunctions.GetCompanyID(User), connection);
                    object Objcount = dLayer.ExecuteScalar("Select count(1) From Inv_ItemMaster where N_CategoryID=" + nCategoryID + " and N_CompanyID =" + myFunctions.GetCompanyID(User), connection);
                    int Obcount = myFunctions.getIntVAL(Objcount.ToString());
                    string xButtonAction = "Delete";
                    string N_CategoryID = "";
                    TransData = dLayer.ExecuteDataTable(Sql, ParamList, connection);

                    SqlTransaction transaction = connection.BeginTransaction();

                    string ipAddress = "";
                    if (Request.Headers.ContainsKey("X-Forwarded-For"))
                        ipAddress = Request.Headers["X-Forwarded-For"];
                    else
                        ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                    myFunctions.LogScreenActivitys(myFunctions.getIntVAL(nFnyearID.ToString()), nCategoryID, TransData.Rows[0]["X_CategoryCode"].ToString(), 73, xButtonAction, ipAddress, "", User, dLayer, connection, transaction);
                    if (Obcount != 0)
                    {
                        transaction.Commit();
                        return Ok(_api.Error(User, "Unable to Delete.Financial category Allready Used"));
                    }
                    DataRow TransRow = TransData.Rows[0];

                    //          SqlTransaction transaction = connection.BeginTransaction();

                    //      string ipAddress = "";
                    // if (  Request.Headers.ContainsKey("X-Forwarded-For"))
                    //     ipAddress = Request.Headers["X-Forwarded-For"];
                    // else
                    //     ipAddress = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                    //        myFunctions.LogScreenActivitys(myFunctions.getIntVAL( nFnyearID.ToString()),nCategoryID,TransRow["N_CategoryID"].ToString(),73,xButtonAction,ipAddress,"",User,dLayer,connection, transaction);

                    Results = dLayer.DeleteData("Inv_ItemCategory", "N_CategoryID", nCategoryID, "", connection, transaction);


                    if (Results > 0)
                    {

                        dLayer.ExecuteNonQuery("Update  Gen_Settings SET  X_Value='' Where X_Group ='Inventory' and X_Description='Default Item Category' and X_Value='" + xCategory.ToString() + "'", connection, transaction);
                        transaction.Commit();
                        return Ok(_api.Success("Financial category deleted"));
                    }
                    else
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, "Unable to delete Financial category"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }


        }

        [HttpGet("dashboardList")]
        public ActionResult GetProductUnitList(int nPage, bool adjustment, int nSizeperpage, string xSearchkey, string xSortBy, int nCategoryID, int nCompanyId)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable dt = new DataTable();
                    SortedList Params = new SortedList();
                    nCompanyId = myFunctions.GetCompanyID(User);
                    string sqlCommandCount = "", xCriteria = "";
                    int Count = (nPage - 1) * nSizeperpage;
                    string sqlCommandText = "";
                    string criteria = "";
                    string cndn = "";
                    Params.Add("@p1", nCompanyId);
                    string Searchkey = "";

                    //    if (xSearchkey != null && xSearchkey.Trim() != "")
                    //     Searchkey = "and (X_CategoryCode like '%" + xSearchkey + "%' OR X_Category like '%" + xSearchkey + "%')";



                    //    if (xSortBy == null || xSortBy.Trim() == "")
                    //         xSortBy = "order by N_CategoryID desc";

                    if (Count == 0)
                        sqlCommandText = "select top(" + nSizeperpage + ") X_Category,N_CategoryID,X_CategoryCode from Inv_ItemCategory where N_CompanyID=@p1" + criteria + cndn + Searchkey + " " + xSortBy;
                    else
                        // sqlCommandText = "select top(" + nSizeperpage + ") ,X_Category,N_CategoryID,X_CategoryCode from Inv_ItemCategory where N_CompanyID=@p1";
                        sqlCommandText = "select top(" + nSizeperpage + ") * from Inv_ItemCategory where ISNULL(N_BaseUnitID,0)=0 and N_CompanyID=@p1 and N_CategoryID not in (select top(" + Count + ") N_CategoryID from Inv_ItemCategory where ISNULL(N_BaseUnitID,0)=0 and N_CompanyID=@p1)" + criteria + cndn + Searchkey + " " + xSortBy;




                    SortedList OutPut = new SortedList();

                    dt = dLayer.ExecuteDataTable(sqlCommandText + xSortBy, Params, connection);
                    sqlCommandCount = "select count(1) as N_Count  from Inv_ItemCategory where N_CompanyID=@p1";
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
                return BadRequest(_api.Error(User, e));
            }


        }


    }
}