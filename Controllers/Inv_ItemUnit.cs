using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SmartxAPI.GeneralFunctions;
using System;
using System.Data;
using System.Collections;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;

namespace SmartxAPI.Controllers

{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("itemunit")]
    [ApiController]
    public class Inv_ItemUnit : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;


        public Inv_ItemUnit(IApiFunctions apiFun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apiFun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }



        [HttpGet("list")]
        public ActionResult GetItemUnitList()
        {
            int nCompanyId= myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "select Code,[Unit Code],Description from vw_InvItemUnit_Disp where N_CompanyID=@p1 and N_ItemID is null order by ItemCode,[Unit Code]";
            Params.Add("@p1", nCompanyId);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
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
                return Ok(api.Error(User,e));
            }
        }

        [HttpGet("listdetails")]
        public ActionResult GetItemUnitListDetails(int? nCompanyId,int? nItemUnitID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();

            string sqlCommandText = "select * from Inv_ItemUnit where N_CompanyID=@p1 and N_ItemUnitID=@p2 ";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nItemUnitID);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
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
                return Ok(api.Error(User,e));
            }
        }
   [HttpGet("dashboardList")]
        public ActionResult GetProductUnitList(int nPage,bool adjustment,int nSizeperpage, string xSearchkey, string xSortBy,int nItemUnitID,int nCompanyId)
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
                    string Searchkey = "";
                    Params.Add("@p1", nCompanyId);
                   

                   if (Count == 0)
                        sqlCommandText = "select top(" + nSizeperpage + ") * from Inv_ItemUnit where ISNULL(N_BaseUnitID,0)=0 and N_CompanyID=@p1 and ISNULL(N_ItemID,0)=0";
                    else
                        sqlCommandText = "select top(" + nSizeperpage + ") * from Inv_ItemUnit where ISNULL(N_BaseUnitID,0)=0 and N_CompanyID=@p1 and ISNULL(N_ItemID,0)=0";


                    SortedList OutPut = new SortedList();

                    dt = dLayer.ExecuteDataTable(sqlCommandText + xSortBy, Params, connection);
                   sqlCommandCount = "select count(*) as N_Count  from Inv_ItemUnit where ISNULL(N_BaseUnitID,0)=0 and N_CompanyID=@p1 and ISNULL(N_ItemID,0)=0";
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
                return BadRequest(api.Error(User, e));
                }
            

        }

        [HttpGet("itemwiselist")]
        public ActionResult GetItemWiseUnitList( string baseUnit, int itemId)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            if (baseUnit == null) { baseUnit = ""; }
            try
            {
                SortedList mParamsList = new SortedList()
                    {
                        {"@N_CompanyID",nCompanyId},
                        {"@X_ItemUnit",baseUnit},
                        {"@N_ItemID",itemId}
                    };
                DataTable masterTable = new DataTable();

// string sql = " Select Inv_ItemUnit.X_ItemUnit,Inv_ItemUnit.N_Qty,dbo.SP_SellingPrice(Inv_ItemUnit.N_ItemID,Inv_ItemUnit.N_CompanyID) as N_SellingPrice,Inv_ItemUnit.N_SellingPrice as N_UnitSellingPrice,Inv_ItemUnit.B_BaseUnit,Inv_ItemUnit.N_ItemUnitID,Inv_ItemMaster.N_PurchaseCost from Inv_ItemUnit Left Outer join Inv_ItemUnit as Base On Inv_ItemUnit.N_BaseUnitID=Base.N_ItemUnitID inner join Inv_ItemMaster ON Inv_ItemUnit.N_ItemID=Inv_ItemMaster.N_ItemID and Inv_ItemUnit.N_CompanyID=Inv_ItemMaster.N_CompanyID where Base.X_ItemUnit=@X_ItemUnit and Inv_ItemUnit.N_CompanyID=@N_CompanyID and Inv_ItemUnit.N_ItemID =@N_ItemID and isnull(dbo.Inv_ItemUnit.B_InActive,0)=0 UNION Select Inv_ItemUnit.X_ItemUnit,Inv_ItemUnit.N_Qty,dbo.SP_SellingPrice(Inv_ItemUnit.N_ItemID,Inv_ItemUnit.N_CompanyID) as N_SellingPrice,Inv_ItemUnit.N_SellingPrice as N_UnitSellingPrice,Inv_ItemUnit.B_BaseUnit,Inv_ItemUnit.N_ItemUnitID,Inv_ItemMaster.N_PurchaseCost from Inv_ItemUnit inner join Inv_ItemMaster ON Inv_ItemUnit.N_ItemID=Inv_ItemMaster.N_ItemID and Inv_ItemUnit.N_CompanyID=Inv_ItemMaster.N_CompanyID where Inv_ItemUnit.X_ItemUnit=@X_ItemUnit and Inv_ItemUnit.N_CompanyID=@N_CompanyID and Inv_ItemUnit.N_ItemID =@N_ItemID and isnull(dbo.Inv_ItemUnit.B_InActive,0)=0";
string sql ="SELECT        Inv_ItemUnit.X_ItemUnit, Inv_ItemUnit.N_Qty, dbo.SP_SellingPrice(Inv_ItemUnit.N_ItemID, Inv_ItemUnit.N_CompanyID) AS N_SellingPrice, Inv_ItemUnit.N_SellingPrice AS N_UnitSellingPrice, Inv_ItemUnit.B_BaseUnit, Inv_ItemUnit.N_ItemUnitID, Inv_ItemMaster.N_PurchaseCost,Inv_ItemUnit.N_DefaultType "
+ " FROM            Inv_ItemUnit LEFT OUTER JOIN Inv_ItemMaster ON Inv_ItemUnit.N_CompanyID = Inv_ItemMaster.N_CompanyID AND Inv_ItemUnit.N_ItemID = Inv_ItemMaster.N_ItemID where Inv_ItemUnit.N_CompanyID=@N_CompanyID and Inv_ItemUnit.N_ItemID =@N_ItemID and isnull(dbo.Inv_ItemUnit.B_InActive,0)=0 ";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    masterTable = dLayer.ExecuteDataTable(sql, mParamsList, connection);
                    // masterTable = dLayer.ExecuteDataTablePro("SP_FillItemUnit", mParamsList, connection);
                }


                if (masterTable.Rows.Count == 0) { return Ok(api.Notice("No Data Found")); }
                return Ok(api.Success(masterTable));
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }


        [HttpGet("itemUnitList")]
        public ActionResult GetItemUnitList( string baseUnit, int itemId)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            if (baseUnit == null) { baseUnit = ""; }
            try
            {
                SortedList mParamsList = new SortedList()
                    {
                        {"@N_CompanyID",nCompanyId},
                        {"@X_ItemUnit",baseUnit},
                        {"@N_ItemID",itemId}
                    };
                DataTable masterTable = new DataTable();

string sql = " Select Inv_ItemUnit.X_ItemUnit,Inv_ItemUnit.N_Qty,dbo.SP_SellingPrice(Inv_ItemUnit.N_ItemID,Inv_ItemUnit.N_CompanyID) as N_SellingPrice,Inv_ItemUnit.N_SellingPrice as N_UnitSellingPrice,Inv_ItemUnit.B_BaseUnit from Inv_ItemUnit Left Outer join Inv_ItemUnit as Base On Inv_ItemUnit.N_BaseUnitID=Base.N_ItemUnitID where Base.X_ItemUnit=@X_ItemUnit and Inv_ItemUnit.N_CompanyID=@N_CompanyID and Inv_ItemUnit.N_ItemID =@N_ItemID and isnull(dbo.Inv_ItemUnit.B_InActive,0)=0 UNION Select X_ItemUnit,Inv_ItemUnit.N_Qty,dbo.SP_SellingPrice(Inv_ItemUnit.N_ItemID,Inv_ItemUnit.N_CompanyID) as N_SellingPrice,Inv_ItemUnit.N_SellingPrice as N_UnitSellingPrice,Inv_ItemUnit.B_BaseUnit from Inv_ItemUnit where X_ItemUnit=@X_ItemUnit and N_CompanyID=@N_CompanyID and N_ItemID =@N_ItemID and isnull(dbo.Inv_ItemUnit.B_InActive,0)=0";

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    masterTable = dLayer.ExecuteDataTable(sql, mParamsList, connection);
                    // masterTable = dLayer.ExecuteDataTablePro("SP_FillItemUnit", mParamsList, connection);
                }


                if (masterTable.Rows.Count == 0) { return Ok(api.Notice("No Data Found")); }
                return Ok(api.Success(masterTable));
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }      

        [HttpDelete("delete")]
        public ActionResult DeleteData(int nItemUnitID)
        {
            int Results = 0;
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                Results = dLayer.DeleteData("Inv_ItemUnit", "N_ItemUnitID", nItemUnitID, "",connection);
                }
                if (Results > 0)
                {
                    return Ok(api.Success( "Product Unit deleted"));
                }
                else
                {
                    return Ok(api.Warning("Unable to delete product Unit"));
                }

            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }


        }
    }
}