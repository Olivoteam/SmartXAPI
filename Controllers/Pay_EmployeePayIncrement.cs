

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
    [Route("amendment")]
    [ApiController]
    public class Pay_EmployeePayIncrement : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;

        public Pay_EmployeePayIncrement(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }
        [HttpGet("list")]
        public ActionResult GetSalaryRevisionList(int nPage, int nSizeperpage, string xSearchkey, string xSortBy, int nFnyearID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            Params.Add("@nCompanyID", nCompanyID);
            Params.Add("@nFnYearID", nFnyearID);

            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandCount = "";
            string Searchkey = "";
            string Criteria = " where N_CompanyID =@nCompanyID and N_FnyearID=@nFnYearID";
            string sqlCommandText = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = " and (X_HistoryCode like '%" + xSearchkey + "%' or X_EmpName like '%" + xSearchkey + "%' or X_EmpCode like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by n_historyid desc";
            else
                xSortBy = " order by " + xSortBy;

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from VW_SalaryRivisionDisp " + Criteria + Searchkey + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from VW_SalaryRivisionDisp " + Criteria + Searchkey + " and N_HistoryID not in(select top(" + Count + ") N_HistoryID from VW_SalaryRivisionDisp " + Criteria + Searchkey + xSortBy + " ) " + xSortBy;
            SortedList OutPut = new SortedList();
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    sqlCommandCount = "select count(1) as N_Count  from VW_SalaryRivisionDisp where N_CompanyID=@nCompanyID " + Searchkey;
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("Details", api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                }
                // dt = api.Format(dt);
                if (dt.Rows.Count == 0)
                {
                    return Ok(api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(api.Success(OutPut));
                }
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }
        [HttpGet("defaultdetails")]
        public ActionResult GetSalaryRevisionDefaultDetails(int nEmpID, int nFnYearID, DateTime EffectiveDate)
        {
            DataTable dtOtherinfo = new DataTable();
            DataTable dtSalaryHistory = new DataTable();
            DataTable dtAccrual = new DataTable();
            DataTable dtBenefits = new DataTable();

            DataSet DS = new DataSet();
            SortedList Params = new SortedList();
            SortedList dParamList = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);


            //string sqlAcrual = "Select *,(Select count(1) from Pay_VacationDetails Where N_CompanyID = vw_Pay_EmployeeAccrul.N_CompanyID AND N_EmpID = vw_Pay_EmployeeAccrul.N_EmpID AND N_VacTypeID = vw_Pay_EmployeeAccrul.N_VacTypeID ) AS N_NoEdit from vw_Pay_EmployeeAccrul Where N_CompanyID=@p1 and N_EmpID=@p3";
            string sqlAcrual = "Select *,(Select count(1) from Pay_VacationDetails Where N_CompanyID = vw_Pay_EmployeeAccrul.N_CompanyID AND N_EmpID = vw_Pay_EmployeeAccrul.N_EmpID AND N_VacTypeID = vw_Pay_EmployeeAccrul.N_VacTypeID ) AS N_NoEdit from vw_Pay_EmployeeAccrul Where N_CompanyID=@p1 and N_EmpID=@p3";
            string sqlBenefits = "Select *,(Select count(1) from Pay_PaymentDetails Where N_CompanyID = vw_EmpPayInformationAmendments.N_CompanyID AND N_EmpID = vw_EmpPayInformationAmendments.N_EmpID AND N_PayID = vw_EmpPayInformationAmendments.N_PayID AND N_Value = vw_EmpPayInformationAmendments.N_value ) AS N_NoEdit from vw_EmpPayInformationAmendments  Where N_CompanyID=@p1 and N_EmpID=@p3 and N_FnYearID=@p2 and N_PaymentID in (6,7)";
            string sqlOtherinfo = "select * from vw_SalaryRevision Where N_CompanyID=@p1 and N_FnYearID=@p2 and N_EmpID=@p3 order by n_type";



            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", nFnYearID);
            Params.Add("@p3", nEmpID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dParamList.Add("@N_CompanyID", nCompanyId);
                    dParamList.Add("@N_FnYearID", nFnYearID);
                    dParamList.Add("@N_EmpID", nEmpID);
                    dParamList.Add("@Date", Convert.ToDateTime(EffectiveDate));

                    dtSalaryHistory = dLayer.ExecuteDataTablePro("SP_Pay_SalaryRevisionDisp", dParamList, connection);
                    dtAccrual = dLayer.ExecuteDataTable(sqlAcrual, Params, connection);
                    dtBenefits = dLayer.ExecuteDataTable(sqlBenefits, Params, connection);
                    dtOtherinfo = dLayer.ExecuteDataTable(sqlOtherinfo, Params, connection);
                      if(dtOtherinfo.Rows.Count>0)
                {
                      if (!dtOtherinfo.Columns.Contains("n_SalaryFrom"))
                        dtOtherinfo = myFunctions.AddNewColumnToDataTable(dtOtherinfo, "n_SalaryFrom", typeof(string), 0);
                    if (!dtOtherinfo.Columns.Contains("n_SalaryTo"))
                        dtOtherinfo = myFunctions.AddNewColumnToDataTable(dtOtherinfo, "n_SalaryTo", typeof(string), 0);
                  
                    if (dtOtherinfo.Columns.Contains("N_Value"))
                    {
                    dtOtherinfo.Rows[0]["n_SalaryFrom"] = dLayer.ExecuteScalar("Select n_SalaryFrom from vw_PayEmployee where N_CompanyID=@p1 and N_FnYearID=@p2 and N_EmpID=@p3", Params, connection);
                    dtOtherinfo.Rows[0]["n_SalaryTo"] = dLayer.ExecuteScalar("Select n_SalaryTo from vw_PayEmployee where N_CompanyID=@p1 and N_FnYearID=@p2 and N_EmpID=@p3", Params, connection);

                }
                }

                }
              
                dtSalaryHistory = api.Format(dtSalaryHistory, "Salaryhistory");
                dtAccrual = api.Format(dtAccrual, "Accrual");
                dtBenefits = api.Format(dtBenefits, "Benefits");
                dtOtherinfo = api.Format(dtOtherinfo, "Otherinfo");
                
                DS.Tables.Add(dtSalaryHistory);
                DS.Tables.Add(dtAccrual);
                DS.Tables.Add(dtBenefits);
                DS.Tables.Add(dtOtherinfo);
                return Ok(api.Success(DS));

            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }


        [HttpGet("details")]
        public ActionResult GetSalaryRevisionDetails(int nFnYearID, string x_HistoryCode)
        {
            DataTable dtOtherinfo = new DataTable();
            DataTable dtSalaryHistory = new DataTable();
            DataTable dtAccrual = new DataTable();
            DataTable dtBenefits = new DataTable();
            DataTable dtMaster = new DataTable();

            DataSet DS = new DataSet();
            SortedList Params = new SortedList();
            SortedList dParamList = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);


            string sqlAcrual = "Select *,(Select count(1) from Pay_VacationDetails Where N_CompanyID = vw_Pay_EmployeeAccrul.N_CompanyID AND N_EmpID = vw_Pay_EmployeeAccrul.N_EmpID AND N_VacTypeID = vw_Pay_EmployeeAccrul.N_VacTypeID ) AS N_NoEdit from vw_Pay_EmployeeAccrul Where N_CompanyID=@p1 and N_EmpID=@p2";
            string sqlBenefits = "Select *,(Select count(1) from Pay_PaymentDetails Where N_CompanyID = vw_EmpPayInformationAmendments.N_CompanyID AND N_EmpID = vw_EmpPayInformationAmendments.N_EmpID AND N_PayID = vw_EmpPayInformationAmendments.N_PayID AND N_Value = vw_EmpPayInformationAmendments.N_value ) AS N_NoEdit from vw_EmpPayInformationAmendments  Where N_CompanyID=@p1 and N_EmpID=@p2 and N_FnYearID=@p3 and N_HistoryID=@p4";
            //string sqlSalaryHistory = "select top 1 * from vw_Pay_EmployeeAdditionalInfo Where N_CompanyID=@p1 and N_HistoryID=@p4";
            string sqlMaster = "select  * from VW_SalaryRivisionDisp Where N_CompanyID=@p1 and x_HistoryCode=" + x_HistoryCode;
            string sqlOtherinfo = "select * from vw_SalaryRevision Where N_CompanyID=@p1 and N_FnYearID=@p3 and N_EmpID=@p2 order by n_type";

            Params.Add("@p1", nCompanyId);
            Params.Add("@p3", nFnYearID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dtMaster = dLayer.ExecuteDataTable(sqlMaster, Params, connection);
                    Params.Add("@p4", Convert.ToUInt32(dtMaster.Rows[0]["n_HistoryID"].ToString()));
                    Params.Add("@p2", Convert.ToUInt32(dtMaster.Rows[0]["n_EmpID"].ToString()));


                    dParamList.Add("@N_CompanyID", nCompanyId);
                    dParamList.Add("@N_FnYearID", nFnYearID);
                    dParamList.Add("@N_EmpID", Convert.ToUInt32(dtMaster.Rows[0]["n_EmpID"].ToString()));
                    dParamList.Add("@Date", Convert.ToDateTime(dtMaster.Rows[0]["D_EffectiveDate"].ToString()));
                    dParamList.Add("@N_HistoryID",  Convert.ToUInt32(dtMaster.Rows[0]["n_HistoryID"].ToString()));
               
                    dtSalaryHistory = dLayer.ExecuteDataTablePro("SP_Pay_SalaryHistoryDisp", dParamList, connection);
                    dtAccrual = dLayer.ExecuteDataTable(sqlAcrual, Params, connection);
                    dtBenefits = dLayer.ExecuteDataTable(sqlBenefits, Params, connection);
                    dtOtherinfo = dLayer.ExecuteDataTable(sqlOtherinfo, Params, connection);
                     if(dtOtherinfo.Rows.Count>0)
                {
                      if (!dtOtherinfo.Columns.Contains("n_SalaryFrom"))
                        dtOtherinfo = myFunctions.AddNewColumnToDataTable(dtOtherinfo, "n_SalaryFrom", typeof(string), 0);
                    if (!dtOtherinfo.Columns.Contains("n_SalaryTo"))
                        dtOtherinfo = myFunctions.AddNewColumnToDataTable(dtOtherinfo, "n_SalaryTo", typeof(string), 0);
                  
                    if (dtOtherinfo.Columns.Contains("N_Value"))
                    {
                    dtOtherinfo.Rows[0]["n_SalaryFrom"] = dLayer.ExecuteScalar("Select n_SalaryFrom from vw_PayEmployee where N_CompanyID=@p1 and N_FnYearID=@p3 and N_EmpID=@p2", Params, connection);
                    dtOtherinfo.Rows[0]["n_SalaryTo"] = dLayer.ExecuteScalar("Select n_SalaryTo from vw_PayEmployee where N_CompanyID=@p1 and N_FnYearID=@p3 and N_EmpID=@p2", Params, connection);

                }
                }

                }
                dtSalaryHistory = api.Format(dtSalaryHistory, "Salaryhistory");
                dtAccrual = api.Format(dtAccrual, "Accrual");
                dtBenefits = api.Format(dtBenefits, "Benefits");
                dtOtherinfo = api.Format(dtOtherinfo, "Otherinfo");
                dtMaster = api.Format(dtMaster, "Master");

                DS.Tables.Add(dtSalaryHistory);
                DS.Tables.Add(dtAccrual);
                DS.Tables.Add(dtBenefits);
                DS.Tables.Add(dtOtherinfo);
                DS.Tables.Add(dtMaster);

                if (dtOtherinfo.Rows.Count == 0)
                {
                    return Ok(api.Warning("No Results Found"));
                }
                else
                {
                    return Ok(api.Success(DS));
                }
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
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


                    DataTable SalaryHistory = ds.Tables["SalaryHistory"];
                    DataTable Benefits = ds.Tables["Benefits"];
                    DataTable Otherinfo = ds.Tables["additional"];


                    DataTable MasterTable = ds.Tables["master"];
                    DataTable pay_PaySetup = ds.Tables["pay_PaySetup"];
                    DataTable pay_EmployeePayHistory = ds.Tables["pay_EmployeePayHistory"];
                    DataTable Accrual = ds.Tables["pay_EmpAccruls"];
                    SortedList Params = new SortedList();
                    DataRow MasterRow = MasterTable.Rows[0];



                    int N_FnYearID = myFunctions.getIntVAL(MasterRow["n_FnYearID"].ToString());
                    int N_CompanyID = myFunctions.getIntVAL(MasterRow["n_CompanyID"].ToString());
                    string x_HistoryNo = MasterRow["x_HistoryCode"].ToString();
                    int N_EmpID = myFunctions.getIntVAL(MasterRow["N_EmpID"].ToString());

if(MasterTable.Columns.Contains("n_FnYearID")){
      MasterTable.Columns.Remove("n_FnYearID");
}

                    if (x_HistoryNo == "@Auto")
                    {
                        Params.Add("N_CompanyID", N_CompanyID);
                        Params.Add("N_YearID", N_FnYearID);
                        Params.Add("N_FormID", 305);
                        Params.Add("N_BranchID", myFunctions.getIntVAL(MasterRow["n_BranchID"].ToString()));
                        x_HistoryNo = dLayer.GetAutoNumber("Pay_PayHistoryMaster", "X_HistoryCode", Params, connection, transaction);
                        if (x_HistoryNo == "")
                        {
                            transaction.Rollback();
                            return Ok("Unable to generate Invoice Number");
                        }
                        MasterTable.Rows[0]["X_HistoryCode"] = x_HistoryNo;
                    }
                    string DupCriteria = "";


                    int n_HistoryId = dLayer.SaveData("Pay_PayHistoryMaster", "N_HistoryID", DupCriteria, "", MasterTable, connection, transaction);
                    if (n_HistoryId <= 0)
                    {
                        transaction.Rollback();
                        return Ok("Unable to save");
                    }

                    for (int i = 0; i < pay_PaySetup.Rows.Count; i++)
                    {
                        pay_PaySetup.Rows[i]["n_HistoryID"] = n_HistoryId;

                    }
                    for (int i = 0; i < pay_EmployeePayHistory.Rows.Count; i++)
                    {
                        pay_EmployeePayHistory.Rows[i]["n_HistoryID"] = n_HistoryId;

                    }
                    //Salary & Benefits Save
                    if(n_HistoryId>0)
                        dLayer.DeleteData("Pay_EmployeePayHistory", "N_HistoryId", n_HistoryId, "", connection, transaction);


                    dLayer.SaveData("Pay_PaySetup", "N_PaySetupID", pay_PaySetup, connection, transaction);
                    dLayer.SaveData("Pay_EmployeePayHistory", "N_PayHistoryID", pay_EmployeePayHistory, connection, transaction);

                    // Other Details
                    if (Otherinfo.Rows[0]["n_NPositionID"].ToString() != "0")
                        dLayer.ExecuteNonQuery("update Pay_Employee set N_PositionID=" + Otherinfo.Rows[0]["n_NPositionID"].ToString() + " where N_EmpID =" + N_EmpID + " and  N_CompanyID =" + N_CompanyID + " and N_FnYearID=" + N_FnYearID, connection, transaction);
                    if (Otherinfo.Rows[0]["n_NDepartmentID"].ToString() != "0")
                        dLayer.ExecuteNonQuery("update Pay_Employee set N_DepartmentID=" + Otherinfo.Rows[0]["n_NDepartmentID"].ToString() + " where N_EmpID =" + N_EmpID + " and  N_CompanyID =" + N_CompanyID + " and N_FnYearID=" + N_FnYearID, connection, transaction);
                    
                  if(Otherinfo.Columns.Contains("N_NProjectID")){
                   if (Otherinfo.Rows[0]["n_NProjectID"].ToString() != "0")
                        dLayer.ExecuteNonQuery("update Pay_Employee set N_ProjectID=" + Otherinfo.Rows[0]["n_NProjectID"].ToString() + " where N_EmpID =" + N_EmpID + " and  N_CompanyID =" + N_CompanyID + " and N_FnYearID=" + N_FnYearID, connection, transaction);
                  }
                 
                    if (Otherinfo.Rows[0]["n_NBranchID"].ToString() != "0")
                        dLayer.ExecuteNonQuery("update Pay_Employee set N_BranchID=" + Otherinfo.Rows[0]["n_NBranchID"].ToString() + " where N_EmpID =" + N_EmpID + " and  N_CompanyID =" + N_CompanyID + " and N_FnYearID=" + N_FnYearID, connection, transaction);
                    if (Otherinfo.Rows[0]["n_NEmpTypeID"].ToString() != "0")
                        dLayer.ExecuteNonQuery("update Pay_Employee set N_EmpTypeID=" + Otherinfo.Rows[0]["n_NEmpTypeID"].ToString() + " where N_EmpID =" + N_EmpID + " and  N_CompanyID =" + N_CompanyID + " and N_FnYearID=" + N_FnYearID, connection, transaction);
                    if (Otherinfo.Rows[0]["n_NLocation"].ToString() != "0")
                        dLayer.ExecuteNonQuery("update pay_employee set N_WorkLocationID='" + Otherinfo.Rows[0]["n_NLocation"].ToString() + "' where N_EmpID =" + N_EmpID + " and  N_CompanyID =" + N_CompanyID, connection, transaction);
                   if(Otherinfo.Columns.Contains("n_NInsClassID")){
                    if (Otherinfo.Rows[0]["n_NInsClassID"].ToString() != "0")
                        dLayer.ExecuteNonQuery("update Pay_Employee set N_InsClassID=" + Otherinfo.Rows[0]["n_NInsClassID"].ToString() + " where N_EmpID =" + N_EmpID + " and  N_CompanyID =" + N_CompanyID + " and N_FnYearID=" + N_FnYearID, connection, transaction);
                   }
                    if (Otherinfo.Rows[0]["n_NSalaryGrade"].ToString() != "0")
                        dLayer.ExecuteNonQuery("update Pay_Employee set n_SalaryGrade=" + Otherinfo.Rows[0]["n_NSalaryGrade"].ToString() + " where N_EmpID =" + N_EmpID + " and  N_CompanyID =" + N_CompanyID + " and N_FnYearID=" + N_FnYearID, connection, transaction);

                    Otherinfo.Rows[0]["n_CLocation"]=0;

                    dLayer.SaveData("Pay_EmployeeAdditionalInfo", "N_DetailsID", Otherinfo, connection, transaction);


                    //Accrual Save
                    if(!Accrual.Columns.Contains("N_EmpAccID"))
                        Accrual = myFunctions.AddNewColumnToDataTable(Accrual, "N_EmpAccID", typeof(int), 0);
                    
                    for (int i = 0; i <= Accrual.Rows.Count - 1; i++)
                    {
                        if (myFunctions.getBoolVAL(Accrual.Rows[i]["b_IsChecked"].ToString()) == false)
                        {
                            dLayer.DeleteData("Pay_EmpAccruls", "N_EmpAccID", myFunctions.getIntVAL(Accrual.Rows[i]["N_EmpAccID"].ToString()), "", connection, transaction);
                            //Accrual.Rows[i].Delete();
                            continue;
                        }
                    }
                    // DataTable AccrualValues=new DataTable();;

                    for (int x = Accrual.Rows.Count - 1; x >= 0; x--)
                    {
                        DataRow dr = Accrual.Rows[x];
                        if (myFunctions.getBoolVAL(dr["b_IsChecked"].ToString()) == false)
                        {
                            dr.Delete();
                        }
                    }

                    Accrual.AcceptChanges();
                    Accrual.Columns.Remove("b_IsChecked");
                    dLayer.SaveData("Pay_EmpAccruls", "N_EmpAccID", Accrual, connection, transaction);
                    transaction.Commit();
                    SortedList Result = new SortedList();

                    return Ok(api.Success(Result, "Saved"));
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }
        }
    }
}



