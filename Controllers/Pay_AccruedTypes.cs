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
    [Route("accruedtypes")]
    [ApiController]
    public class Pay_AccruedTypes : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly IApiFunctions _api;
        private readonly string connectionString;
        private readonly int N_FormID = 587;

        public Pay_AccruedTypes(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            _api = api;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }



        [HttpGet("Dashboardlist")]
        public ActionResult PayAccruedList(int nPage, int nSizeperpage, string xSearchkey, string xSortBy, int nCountryID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            string sqlCommandCount = "";
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string Searchkey = "";
            Params.Add("@p1", nCompanyId);
            if (xSearchkey != null && xSearchkey.Trim() != "")

                Searchkey = " and (x_VacCode like '%" + xSearchkey + "%' or Name like'%" + xSearchkey + "%'or x_TypeName like'%" + xSearchkey + "%' or x_Period like'%" + xSearchkey + "%')";
            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by cast(x_VacCode as numeric) desc";
            else
            {
                   switch (xSortBy.Split(" ")[0])
                        {
                             case "x_VacCode":
                                xSortBy = "N_VacTypeID " + xSortBy.Split(" ")[1];
                                break;
                           
                            default: break;
                        }
            
                xSortBy = " order by " + xSortBy;
        }

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_PayAccruedCode_List_Web where N_CompanyID=@p1 and N_CountryID=" + nCountryID + " " + Searchkey + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_PayAccruedCode_List_Web where N_CompanyID=" + nCompanyId + "  and N_CountryID=" + nCountryID + " " + Searchkey + " and N_VacTypeID not in (select top(" + Count + ") N_VacTypeID from vw_PayAccruedCode_List_Web where N_CompanyID=" + nCompanyId + " and N_CountryID=" + nCountryID + " " + xSortBy + " ) " + xSortBy;



            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

<<<<<<< HEAD
                    sqlCommandCount = "select count(*) as N_Count  from vw_PayAccruedCode_List_Web where N_CompanyID=@p1 and N_CountryID=" + nCountryID + "" + Searchkey;
=======
                    sqlCommandCount = "select count(1) as N_Count  from vw_PayAccruedCode_List_Web where N_CompanyID=@p1" + Searchkey;
>>>>>>> 8bb5e85fea1d85a158582bc5cd7ca450e1a2a934
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("Details", _api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                    if (dt.Rows.Count == 0)
                    {
                        //return Ok(_api.Warning("No Results Found"));
                          return Ok(_api.Success(OutPut));
                    }
                    else
                    {
                        return Ok(_api.Success(OutPut));
                    }

                }

            }
            catch (Exception e)
            {
                return BadRequest(_api.Error(User,e));
            }
        }


        [HttpGet("VactionList")]
        public ActionResult AccruedTypeList()
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nComapnyID", nCompanyID);
            SortedList OutPut = new SortedList();
            string sqlCommandText = "select N_CompanyID,X_VacCode,X_VacType,N_VacTypeID from Pay_VacationType where N_CompanyID=@nComapnyID and X_Type<>'T'";
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





        [HttpGet("details")]
        public ActionResult PayAccruedDetails(string xVacCode, int nVacTypeID, int nCountryID)
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
                    Params.Add("@xVacCode", xVacCode);
                    Mastersql = "select * from vw_PayVacationType_Web where N_CompanyId=@nCompanyID and x_VacCode=@xVacCode and N_CountryID=" + nCountryID + "  ";

                    MasterTable = dLayer.ExecuteDataTable(Mastersql, Params, connection);
                    if (MasterTable.Rows.Count == 0) { return Ok(_api.Warning("No data found")); }
                    int VacTypeID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_VacTypeID"].ToString());
                    Params.Add("@nVacTypeID", VacTypeID);

                    MasterTable = _api.Format(MasterTable, "Master");
                    DetailSql = "select * from Pay_VacationTypeDetails where N_CompanyId=@nCompanyID and N_VacTypeID=@nVacTypeID ";
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





        [HttpPost("Save")]
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
                    DataRow MasterRow = MasterTable.Rows[0];
                    SortedList Params = new SortedList();

                    int n_VacTypeID = myFunctions.getIntVAL(MasterRow["N_VacTypeID"].ToString()); 
                    int N_CountryID = myFunctions.getIntVAL(MasterRow["N_CountryID"].ToString()); 
                    int N_FnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearID"].ToString());
                    int N_CompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyID"].ToString());
                    DateTime dtpModDate = Convert.ToDateTime(MasterTable.Rows[0]["d_ModifiedDate"].ToString());
                    string X_VacType=(MasterRow["X_VacType"].ToString());
              
                    string x_VacCode = MasterRow["X_VacCode"].ToString();
                    var values = MasterTable.Rows[0]["X_VacCode"].ToString();
                    if (n_VacTypeID > 0)
                    {
                        string N_StartType = (MasterRow["N_StartType"].ToString());
                        object objVacationStarted;
                        MasterTable.Columns.Remove("n_FnYearId");

                        // object LastProcessed = null;
                        // LastProcessed = dLayer.ExecuteScalar("select 1 From Pay_VacationDetails Where N_VacTypeID= " + n_VacTypeID + " and N_CompanyID= " + N_CompanyID, connection, transaction);
                        // if (LastProcessed != null)
                        // {
                        //     DateTime DtpDate = Convert.ToDateTime(dLayer.ExecuteScalar("select MAX(D_VacSanctionDate) FRom Pay_VacationDetails Where N_VacTypeID= " + n_VacTypeID + " and N_CompanyID= " + N_CompanyID, connection, transaction));
                        //     var ProcessDate = DtpDate;
                        //     var Moddate = dtpModDate;
                        //     if (Moddate < ProcessDate)
                        //     {
                        //          return Ok(_api.Error(User,"Cannot save by this Date!!!!!!"));

                        //     }
                        // }

                        dLayer.DeleteData("Pay_VacationTypeDetails", "N_VacTypeID", n_VacTypeID, " N_CompanyID=" +N_CompanyID+"", connection, transaction);
                    }
                    if (x_VacCode == "@Auto")
                    {
                        Params.Add("N_CompanyID", N_CompanyID);
                        Params.Add("N_YearID", N_FnYearID);
                        Params.Add("N_FormID", N_FormID);
                        x_VacCode = dLayer.GetAutoNumber("Pay_VacationType", "X_VacCode", Params, connection, transaction);
                        if (x_VacCode == "")
                        {
                            transaction.Rollback();
                            return Ok("Unable to generate Accrual Code");
                        }
                        MasterTable.Rows[0]["X_VacCode"] = x_VacCode;
                        MasterTable.Columns.Remove("n_FnYearId");
                    }
                    //string DupCriteria = "N_companyID=" + N_CompanyID + " And x_VacCode = '" + values + "' and N_CountryID="+N_CountryID+"";
                     string DupCriteria = "N_CompanyID=" + N_CompanyID + " and N_CountryID =" +N_CountryID+ " and (X_VacType='" +X_VacType + "' or X_VacCode='" + values + "') ";


                    n_VacTypeID = dLayer.SaveData("Pay_VacationType", "n_VacTypeID", DupCriteria,"N_CompanyID=" +N_CompanyID+" and N_CountryID = "+N_CountryID+"", MasterTable, connection, transaction);
                    if (n_VacTypeID <= 0)
                    {
                        transaction.Rollback();
                        return Ok("Unable to save Accrual Code");
                    }
                    for (int j = 0; j < DetailTable.Rows.Count; j++)
                    {
                        DetailTable.Rows[j]["n_VacTypeID"] = n_VacTypeID;
                    }
                    int n_VacTypeDetailsID = dLayer.SaveData("Pay_VacationTypeDetails", "n_VacTypeDetailsID", DetailTable, connection, transaction);
                    if (n_VacTypeDetailsID <= 0)
                    {
                        transaction.Rollback();
                        return Ok("Unable to save Accrual Code");
                    }

                    transaction.Commit();
                    SortedList Result = new SortedList();
                    Result.Add("n_VacTypeID", n_VacTypeID);
                    Result.Add("x_VacCode", x_VacCode);
                    Result.Add("n_VacTypeDetailsID", n_VacTypeDetailsID);

                    return Ok(_api.Success(Result, " Accrual Code Created"));
                }
            }
            catch (Exception ex)
            {
                return Ok(_api.Error(User,ex));
            }
        }



        [HttpDelete("delete")]
        public ActionResult DeleteData(int nVacTypeID, int nCompanyID, int fnYearID)

        {

            int Results = 0;
            object objVacationStarted;
            object objAssigned;

            try
            {
                SortedList Params = new SortedList();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    objAssigned = dLayer.ExecuteScalar("select 1 FRom Pay_EmpAccruls Where N_VacTypeID= " + nVacTypeID + " and N_CompanyID=" + nCompanyID + "", connection, transaction);
                    objVacationStarted = dLayer.ExecuteScalar("select 1 FRom Pay_VacationDetails Where N_VacTypeID= " + nVacTypeID + " and N_CompanyID= " + nCompanyID + " and N_FnYearID=" + fnYearID, connection, transaction);
                    if (objVacationStarted != null)
                    {

                        return Ok(_api.Error(User,"Transaction started cannot delete Accrual Code"));


                    }
                    else if (objAssigned != null)
                    {
                        return Ok(_api.Error(User,"can't delete Accrual Code"));

                    }
                    else
                    {

                        Results = dLayer.DeleteData("Pay_VacationType", "N_VacTypeID", nVacTypeID, "", connection, transaction);
                        transaction.Commit();
                    }
                }
                if (Results > 0)
                {
                    Dictionary<string, string> res = new Dictionary<string, string>();
                    res.Add("N_VacTypeID", nVacTypeID.ToString());
                    return Ok(api.Success(res, "Accrual Code deleted"));
                }
                else
                {
                    return Ok(api.Error(User,"Unable to delete Accrual Code"));
                }

            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }



        }
    }
}