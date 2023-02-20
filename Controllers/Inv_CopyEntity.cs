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
    [Route("copyEntity")]
    [ApiController]
    public class Inv_CopyEntity : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly int FormID;
        private readonly IMyFunctions myFunctions;
        private readonly IMyAttachments myAttachments;
        private readonly string connectionString;

        public Inv_CopyEntity(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf, IMyAttachments myAtt)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            myAttachments = myAtt;
            connectionString = conf.GetConnectionString("SmartxConnection");
            FormID = 395;
        }

        [HttpGet("list")]
        public ActionResult CompanyList(string XUserID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable dt = new DataTable();
                    SortedList Params = new SortedList();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    Params.Add("@XUserID", XUserID);

                    string sqlCommandText = "select * from Acc_Company where N_CompanyID in ( select N_CompanyID from Sec_User where x_userID=@XUserID)";

                    SortedList OutPut = new SortedList();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    dt = _api.Format(dt);
                    if (dt.Rows.Count == 0)
                    {
                        return Ok(_api.Warning("No Results Found"));
                    }
                    else
                    {
                        return Ok(_api.Success(dt));
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(_api.Error(User, e));
            }
        }



        [HttpGet("details")]

        public ActionResult GetData(int nCompanyID, string xType)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            string sqlCommandCount = "";

            switch (xType.ToString().ToLower())
            {
                case "products":
                    sqlCommandCount = "select count(1) as N_Count  from vw_InvItemMaster where N_CompanyID=@p1";
                    break;

                default: break;
            }

            Params.Add("@p1", nCompanyID);
            SortedList OutPut = new SortedList();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("TotalCount", TotalCount);
                    return Ok(_api.Success(OutPut));
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User, e));
            }
        }



        [HttpPost("save")]
        public ActionResult UpdateStatus([FromBody] DataSet ds)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataTable mastertable = new DataTable();
                    DataTable dt = new DataTable();
                    mastertable = ds.Tables["master"];
                    SortedList OutPut = new SortedList();
                    int nCopyFromID = myFunctions.getIntVAL(mastertable.Rows[0]["nCopyFromCompany"].ToString());
                    string type = mastertable.Rows[0]["type"].ToString();
                    int nCompanyID = myFunctions.GetCompanyID(User);
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Param = new SortedList();
                    Param.Add("@nCopyFromID", nCopyFromID);
                    string sqlCommandText = "";

                    switch (type.ToString().ToLower())
                    {
                        case "products":
                            sqlCommandText = "select " + nCompanyID + " as N_CompanyID,N_ItemID as [Pkey_Code],x_ItemCode as [Product_Code],x_ItemName as [Description],x_PurchaseDescription as [Description_Locale],x_ClassName as [Product_Type],x_Category as [Product_Category],x_PartNo as [Part_No],x_ItemManufacturer as [manufacturer],x_VendorName as [Default_Vendor],n_ItemCost as unit_Cost,n_SalesSprice as selling_Price,x_StockUnit as Stock_Unit,x_PurchaseUnit as Purchase_Unit,n_PurchaseQty as [Qty_Purchase_Unit],n_Saleqty as Qty_Sales_Unit from vw_InvItemMaster where N_CompanyID=@nCopyFromID";
                            dt = dLayer.ExecuteDataTable(sqlCommandText, Param, connection, transaction);
                            dLayer.SaveData("Mig_Items", "Pkey_Code", dt, connection, transaction);
                            break;

                        default: break;
                    }





                    SortedList Params = new SortedList();
                    Params.Add("N_CompanyID", myFunctions.getIntVAL(mastertable.Rows[0]["N_CompanyID"].ToString()));
                    Params.Add("N_FnYearID", myFunctions.getIntVAL(mastertable.Rows[0]["N_FnYearID"].ToString()));
                    Params.Add("X_Type", type);
                    Params.Add("N_BranchID", myFunctions.getIntVAL(mastertable.Rows[0]["N_BranchID"].ToString()));
                    Params.Add("N_LocationID", myFunctions.getIntVAL(mastertable.Rows[0]["N_LocationID"].ToString()));

                    try
                    {
                        dLayer.ExecuteNonQueryPro("SP_SetupData_cloud", Params, connection, transaction);
                        transaction.Commit();
                        return Ok(_api.Success("Copied"));
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return Ok(_api.Error(User, ex));
                    }

                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User, ex));
            }


        }


    }
}
