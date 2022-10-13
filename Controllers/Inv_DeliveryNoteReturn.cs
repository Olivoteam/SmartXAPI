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
     [Route("invDeliveryNoteReturn")]
     [ApiController]
    public class InvDeliveryNoteReturn : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions api;
        private readonly string connectionString;
        private readonly IMyFunctions myFunctions;
        private readonly IApiFunctions _api;
        public InvDeliveryNoteReturn(IDataAccessLayer dl,IMyFunctions myFun, IApiFunctions apiFun, IConfiguration conf)
        {
            dLayer = dl;
            api = apiFun;
            _api = api;
            myFunctions=myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }
           
        [HttpGet("dashboardList")]
        public ActionResult GetDeliveryNoteReturnDashboardList(int nFormID, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

           
            if (xSearchkey != null && xSearchkey.Trim() != "")
                      Searchkey = "and (X_ReturnNo like'%" + xSearchkey + "%'or X_CustomerName like'%" + xSearchkey + "%')";

         
                    if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_DeliveryNoteRtnID desc";
            else
            {
                switch (xSortBy.Split(" ")[0])
                {
                    case "X_ReturnNo":
                        xSortBy = "[X_ReturnNo] " + xSortBy.Split(" ")[1];
                        break;
                    case "d_ReturnDate":
                        xSortBy = "Cast([D_ReturnDate] as DateTime )" + xSortBy.Split(" ")[1];
                        break;
               
                    default: break;
                }
                xSortBy = " order by " + xSortBy;
            }

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_DeliveryNoteReturn where N_CompanyID=@nCompanyID and N_FormID=@nFormID " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_Inv_DeliveryNoteReturn where N_CompanyID=@nCompanyID and N_FormID=@nFormID " + Searchkey + " and N_DeliveryNoteRtnID not in (select top(" + Count + ") N_DeliveryNoteRtnID from vw_DeliveryNoteReturnMaster where N_CompanyID=@nCompanyID and N_FormID=@nFormID " + xSortBy + " ) " + " " + xSortBy;

            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFormID", nFormID);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    SortedList OutPut = new SortedList();

                    sqlCommandCount = "select count(*) as N_Count  from vw_Inv_DeliveryNoteReturn where N_CompanyID=@nCompanyID and N_FormID=@nFormID " + Searchkey + "";
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
                return Ok(api.Error(User, e));
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
                    MasterTable = ds.Tables["master"];
                    DetailTable = ds.Tables["details"];
                    DataTable rentalItem = ds.Tables["segmentTable"];
                    DataRow MasterRow = MasterTable.Rows[0];
                    SortedList Params = new SortedList();

                    int nDeliveryNoteRtnID = myFunctions.getIntVAL(MasterRow["n_DeliveryNoteRtnID"].ToString());
                    int nDNRtnID = myFunctions.getIntVAL(MasterRow["n_DeliveryNoteRtnID"].ToString());
                    int nFnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearID"].ToString());
                    int nCompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyID"].ToString());
                    int nFormID = myFunctions.getIntVAL(MasterRow["n_FormID"].ToString());
                    string xReturnNo = MasterRow["x_ReturnNo"].ToString();

                    if (xReturnNo == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearID);
                        Params.Add("N_FormID", nFormID);
                        xReturnNo = dLayer.GetAutoNumber("Inv_DeliveryNoteReturn", "X_ReturnNo", Params, connection, transaction);
                        if (xReturnNo == "")
                        {
                            transaction.Rollback();
                            return Ok("Unable to generate Delivery Note Return No.");
                        }
                        MasterTable.Rows[0]["X_ReturnNo"] = xReturnNo;
                    }

                    nDeliveryNoteRtnID = dLayer.SaveData("Inv_DeliveryNoteReturn", "N_DeliveryNoteRtnID", "", "", MasterTable, connection, transaction);
                    if (nDeliveryNoteRtnID <= 0)
                    {
                        transaction.Rollback();
                        return Ok("Unable to save Delivery Note Return");
                    }
                    dLayer.DeleteData("Inv_DeliveryNoteReturnDetails", "N_DeliveryNoteRtnID", nDeliveryNoteRtnID, "", connection, transaction);
                    // for (int j = 0; j < DetailTable.Rows.Count; j++)
                    // {
                    //     DetailTable.Rows[j]["N_DeliveryNoteRtnID"] = nDeliveryNoteRtnID;
                    // }

                    // int nDeliveryNoteRtnDtlsID = dLayer.SaveData("Inv_DeliveryNoteReturnDetails", "N_DeliveryNoteRtnDtlsID", DetailTable, connection, transaction);
                   
                    int nDeliveryNoteRtnDtlsID =0;
                      for (int j = 0; j < DetailTable.Rows.Count; j++)
                        {
                           DetailTable.Rows[j]["N_DeliveryNoteRtnID"] = nDeliveryNoteRtnID;
                         
                             nDeliveryNoteRtnDtlsID = dLayer.SaveDataWithIndex("Inv_DeliveryNoteReturnDetails", "N_DeliveryNoteRtnDtlsID", "", "", j, DetailTable, connection, transaction);
                            
                          
                            if (nDeliveryNoteRtnDtlsID > 0)
                            {
                                for (int k = 0; k < rentalItem.Rows.Count; k++)
                                {
                                    
                                    if (myFunctions.getIntVAL(rentalItem.Rows[k]["rowID"].ToString()) == j)
                                    {
                                     
                                       rentalItem.Rows[k]["n_TransID"] = nDeliveryNoteRtnID;
                                       rentalItem.Rows[k]["n_TransDetailsID"] = nDeliveryNoteRtnDtlsID;
                                        
                                         
                                        rentalItem.AcceptChanges();
                                    }
                                    rentalItem.AcceptChanges();
                                }

                               

                                rentalItem.AcceptChanges();
                            }
                            DetailTable.AcceptChanges();


                        }
                         
                           if (rentalItem.Columns.Contains("rowID"))
                            rentalItem.Columns.Remove("rowID");
                        
                        rentalItem.AcceptChanges();
                      
                            if (nDNRtnID > 0)
                    {
                             int N_FormID = myFunctions.getIntVAL(rentalItem.Rows[0]["n_FormID"].ToString());
                            dLayer.ExecuteScalar("delete from Inv_RentalSchedule where N_TransID=" + nDeliveryNoteRtnID.ToString() + " and N_FormID="+ N_FormID + " and N_CompanyID=" + nCompanyID, connection, transaction);
                       
                    }
                         dLayer.SaveData("Inv_RentalSchedule", "N_ScheduleID", rentalItem, connection, transaction);


                   
                   
                   
                    if (nDeliveryNoteRtnDtlsID <= 0)
                    {
                        transaction.Rollback();
                        return Ok("Unable to save Delivery Note Return");
                    }
                    transaction.Commit();
                    SortedList Result = new SortedList();
                    Result.Add("n_DeliveryNoteRtnID", nDeliveryNoteRtnID);
                    Result.Add("x_ReturnNo", xReturnNo);
                    Result.Add("n_DeliveryNoteRtnDtlsID", nDeliveryNoteRtnDtlsID);

                    return Ok(_api.Success(Result, "Delivery Note Return Saved"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }

        [HttpGet("details")]
        public ActionResult DeliveryNoteReturnDetails(string xReturnNo, int nDeliveryNoteId ,int nFormID)
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
                    string crieteria = "";
                    Params.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    Params.Add("@nFormID", nFormID);
                    if(nFormID>0)
                    {
                    crieteria = " and N_FormID = @nFormID ";
                    }
                    if (nDeliveryNoteId > 0)
                    {
                        Params.Add("@nDeliveryNoteId", nDeliveryNoteId);
                        Mastersql = "select * from vw_DeliveryNoteDisptoReturn where N_CompanyId=@nCompanyID and N_DeliveryNoteID=@nDeliveryNoteId";
                        MasterTable = dLayer.ExecuteDataTable(Mastersql, Params, connection);
                        if (MasterTable.Rows.Count == 0) { return Ok(_api.Warning("No data found")); }
                        MasterTable = _api.Format(MasterTable, "Master");
                        DetailSql = "select * from vw_DeliveryNoteDispDetailstoReturn where N_CompanyId=@nCompanyID and N_DeliveryNoteID=@nDeliveryNoteId";
                        DetailTable = dLayer.ExecuteDataTable(DetailSql, Params, connection);
                        DetailTable = _api.Format(DetailTable, "Details");
                        string RentalScheduleSql = "SELECT * FROM  vw_RentalScheduleItems  Where N_CompanyID=@nCompanyID and N_TransID=@nDeliveryNoteId " +crieteria;
                        DataTable RentalSchedule = dLayer.ExecuteDataTable(RentalScheduleSql, Params, connection);
                        RentalSchedule = _api.Format(RentalSchedule, "RentalSchedule");
                        dt.Tables.Add(MasterTable);
                        dt.Tables.Add(DetailTable);
                        dt.Tables.Add(RentalSchedule);
                        return Ok(_api.Success(dt));
                    } else {
                        Params.Add("@xReturnNo", xReturnNo);
                        Mastersql = "select * from vw_Inv_DeliveryNoteReturn where N_CompanyID=@nCompanyID and X_ReturnNo=@xReturnNo  ";
                   
                        MasterTable = dLayer.ExecuteDataTable(Mastersql, Params, connection);
                        if (MasterTable.Rows.Count == 0) { return Ok(_api.Warning("No data found")); }
                        int nDeliveryNoteRtnID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_DeliveryNoteRtnID"].ToString());
                        Params.Add("@nDeliveryNoteRtnID", nDeliveryNoteRtnID);

                        MasterTable = _api.Format(MasterTable, "Master");
                        DetailSql = "select * from vw_Inv_DeliveryNoteReturnDetails where N_CompanyID=@nCompanyID and N_DeliveryNoteRtnID=@nDeliveryNoteRtnID ";
                        DetailTable = dLayer.ExecuteDataTable(DetailSql, Params, connection);
                        DetailTable = _api.Format(DetailTable, "Details");
                        string RentalScheduleSql = "SELECT * FROM  vw_RentalScheduleItems  Where N_CompanyID=@nCompanyID and N_TransID=" + nDeliveryNoteRtnID + crieteria ;
                        DataTable RentalSchedule = dLayer.ExecuteDataTable(RentalScheduleSql, Params, connection);
                        RentalSchedule = _api.Format(RentalSchedule, "RentalSchedule");

                        dt.Tables.Add(MasterTable);
                        dt.Tables.Add(DetailTable);
                        dt.Tables.Add(RentalSchedule);
                        return Ok(_api.Success(dt));
                    };
                }
            }
            catch (Exception e)
            {
                return Ok(_api.Error(User,e));
            }
        }
            
        [HttpGet("list")]
        public ActionResult GetDeliveryNoteReturnList()
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nComapnyID", nCompanyID);
            SortedList OutPut = new SortedList();
            string sqlCommandText = "select * from vw_DeliveryNoteReturnMaster where N_CompanyID=@nComapnyID";
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
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

        [HttpDelete("delete")]
        public ActionResult DeleteData(int nDeliveryNoteRtnID, int nCompanyID, int nFnYearID)
        {
            int Results = 0;
            try
            {
                SortedList QueryParams = new SortedList();
                QueryParams.Add("@nCompanyID", nCompanyID);
                QueryParams.Add("@nFnYearID", nFnYearID);
                QueryParams.Add("@nDeliveryNoteRtnID", nDeliveryNoteRtnID);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    Results = dLayer.DeleteData("Inv_DeliveryNoteReturn", "N_DeliveryNoteRtnID", nDeliveryNoteRtnID, "", connection);


                    if (Results > 0)
                    {
                        dLayer.DeleteData("Inv_DeliveryNoteReturnDetails", "N_DeliveryNoteRtnID", nDeliveryNoteRtnID, "", connection);
                        return Ok(_api.Success("Delivery Note Return deleted"));
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
    