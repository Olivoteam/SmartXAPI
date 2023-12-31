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
    [Route("schAdmission")]
    [ApiController]
    public class Sch_Admission : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly string masterDBConnectionString;

        private readonly int N_FormID =155 ;


        public Sch_Admission(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            masterDBConnectionString = conf.GetConnectionString("OlivoClientConnection");
        }

        [HttpGet("list")]
        public ActionResult GetAdmissionList(int? nCompanyId, int nAcYearID, int nPage, int nSizeperpage, string xSearchkey, string xSortBy,int nRegID,int nInactive)
        {
            int nCompanyID = myFunctions.GetCompanyID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string sqlCommandCount = "";
            string Searchkey = "";

            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_AdmissionNo like '%" + xSearchkey + "%' or X_Name like '%" + xSearchkey + "%' or D_AdmissionDate like '%" + xSearchkey + "%' or X_StudentMobile like '%" + xSearchkey + "%' or X_GaurdianName like '%" + xSearchkey + "%'  or X_StudentCatName like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_AdmissionID desc";
            else
            {
                switch (xSortBy.Split(" ")[0])
                {
                    case "X_AdmissionNo":
                        xSortBy = "X_AdmissionNo " + xSortBy.Split(" ")[1];
                        break;
                           case "x_Name":
                        xSortBy = "x_Name " + xSortBy.Split(" ")[1];
                        break;
                    default: break;
                }
                xSortBy = " order by " + xSortBy;
            }

            if(nInactive==0){
                if (Count == 0)
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_SchAdmission_Dashboard where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=0 " + Searchkey + " " + xSortBy;
                else
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_SchAdmission_Dashboard where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=0 " + Searchkey + " and N_AdmissionID not in (select top(" + Count + ") N_AdmissionID from vw_SchAdmission where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=0 " + xSortBy + " ) " + " " + xSortBy;
            }        
            else if(nInactive==2){
                if (Count == 0)
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_SchAdmission_Dashboard where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=2 " + Searchkey + " " + xSortBy;
                else
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_SchAdmission_Dashboard where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=2 " + Searchkey + " and N_AdmissionID not in (select top(" + Count + ") N_AdmissionID from vw_SchAdmission where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=2 " + xSortBy + " ) " + " " + xSortBy;
            }        
            else{
                if (Count == 0)
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_SchAdmission_Dashboard where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=1 " + Searchkey + " " + xSortBy;
                else
                    sqlCommandText = "select top(" + nSizeperpage + ") * from vw_SchAdmission_Dashboard where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=1 " + Searchkey + " and N_AdmissionID not in (select top(" + Count + ") N_AdmissionID from vw_SchAdmission where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=1 " + xSortBy + " ) " + " " + xSortBy;
            }  

       
            Params.Add("@nCompanyId", nCompanyID);
            Params.Add("@nAcYearID", nAcYearID);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    SortedList OutPut = new SortedList();

                    sqlCommandCount = "select count(1) as N_Count  from vw_SchAdmission_Dashboard  where N_CompanyID=@nCompanyId and N_AcYearID=@nAcYearID and isNull(N_Inactive,0)=0 " + Searchkey + "";
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("Details", api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                    if (dt.Rows.Count == 0)
                    {
                        //return Ok(api.Warning("No Results Found"));
                        return Ok(api.Success(OutPut));
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

        [HttpGet("details")]
        public ActionResult AdmissionDetails(string xAdmissionNo, int nAcYearID,int nRegID,int nStudentID)
        {
            DataSet dt=new DataSet();
            DataTable MasterTable = new DataTable();
            DataTable BusDetails = new DataTable();
            DataTable FeeDetails = new DataTable();
            DataTable FeeAmtDetails = new DataTable();
            DataTable CourseDetails = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId=myFunctions.GetCompanyID(User);
            string sqlCommandText = "";
           
            
            if(nStudentID>0)
            {
                Params.Add("@p5", nStudentID);
                sqlCommandText=" select * from vw_SchAdmission where N_CompanyID=@p1 and N_AdmissionID=@p5 and N_AcYearID=@p4";  
            }
            else  if (xAdmissionNo!=null)
            {
                Params.Add("@p2", xAdmissionNo);
                sqlCommandText=" select * from vw_SchAdmission where N_CompanyID=@p1 and X_AdmissionNo=@p2 and N_AcYearID=@p4";
            }

            if(nRegID>0)
            {
                Params.Add("@nRegID", nRegID);
                sqlCommandText=" select * from vw_StudentRegToAdmission where N_CompanyId=@p1 and N_RegID=@nRegID";
            }
            Params.Add("@p1", nCompanyId);  
            Params.Add("@p4", nAcYearID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    MasterTable = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);

                    if (MasterTable.Rows.Count == 0)
                    {
                        return Ok(api.Warning("No Results Found"));
                    }
                
                    MasterTable = api.Format(MasterTable, "Master");
                    dt.Tables.Add(MasterTable);

                    int N_AdmissionID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_AdmissionID"].ToString());
                    Params.Add("@p3", N_AdmissionID);

                    string BusDetailSql = "select * from vw_SchReg_Disp where N_CompanyID=@p1 and N_AdmissionID=@p3 and N_AcYearID=@p4";

                    BusDetails = dLayer.ExecuteDataTable(BusDetailSql, Params, connection);
                    BusDetails = api.Format(BusDetails, "BusDetails");
                    dt.Tables.Add(BusDetails);

                    string FeeDetailSql = "select N_CompanyID,N_AcYearID,X_FeeDescription,N_Amount,N_DiscountAmt,N_FeeCodeID,N_FrequencyID,N_Frequency,N_Installment from vw_SchStudentFee_view where N_CompanyID=@p1 and N_AcYearID=@p4 and N_RefID=@p3 order by N_Sort ASC";

                    FeeDetails = dLayer.ExecuteDataTable(FeeDetailSql, Params, connection);
                    FeeDetails = api.Format(FeeDetails, "FeeDetails");
                    dt.Tables.Add(FeeDetails);  

                    string FeeAmtDetailSql = "select * from  vw_SchFeeReceived where N_CompanyID=@p1 and N_FnYearID=@p4 and N_Refid=@p3";

                    FeeAmtDetails = dLayer.ExecuteDataTable(FeeAmtDetailSql, Params, connection);
                    FeeAmtDetails = api.Format(FeeAmtDetails, "FeeAmtDetails");
                    dt.Tables.Add(FeeAmtDetails);

                    Params.Add("@nStudentID", nStudentID);

                    string CourseDetailSql = "select * from  Vw_SchCourseDetails where N_CompanyID=@p1 and N_AcYearID=@p4 and N_StudentID=@p3";
                    CourseDetails = dLayer.ExecuteDataTable(CourseDetailSql, Params, connection);
                    CourseDetails = api.Format(CourseDetails, "CourseDetails");
                    dt.Tables.Add(CourseDetails);

                }
                return Ok(api.Success(dt));                                       
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }

        //Save....
        [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                DataTable dtCustomer;
                DataTable CourseTable;
                MasterTable = ds.Tables["master"];
                CourseTable = ds.Tables["courseDetails"];
                int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyId"].ToString());
                int nFnYearId = myFunctions.getIntVAL(MasterTable.Rows[0]["n_FnYearId"].ToString());
                int nAcYearID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_AcYearID"].ToString());
                int nAdmissionID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_AdmissionID"].ToString());
                int nBranchID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_BranchID"].ToString());
                int nLocationID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_LocationID"].ToString());
                int nCustomerID = myFunctions.getIntVAL(MasterTable.Rows[0]["N_CustomerID"].ToString());
                int nUserID = myFunctions.GetUserID(User);
                 var X_AdmissionNo = MasterTable.Rows[0]["X_AdmissionNo"].ToString();
                int nAdmID=nAdmissionID; 
                string xEmail = MasterTable.Rows[0]["x_Email"].ToString();             

                if (MasterTable.Columns.Contains("N_BranchID"))
                    MasterTable.Columns.Remove("N_BranchID");
                if (MasterTable.Columns.Contains("N_LocationID"))
                    MasterTable.Columns.Remove("N_LocationID");

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    SortedList CustParams = new SortedList();

                    // Auto Gen
                    string Code = "",CustCode="";
                  
                    object CustomerCode = dLayer.ExecuteScalar("select X_CustomerCode from Inv_Customer where N_CompanyID="+nCompanyID+" and N_FnYearID="+nAcYearID+" and N_CustomerID="+nCustomerID, Params, connection, transaction);
                    if (CustomerCode != null)
                    {
                        CustCode=CustomerCode.ToString();
                    }

                    var values = MasterTable.Rows[0]["X_AdmissionNo"].ToString();
                    if(values!= null && values != "@Auto" && nAdmissionID == 0)
                    {
                           object AdCode = dLayer.ExecuteScalar("select count(1) from Sch_Admission  where N_CompanyID="+ nCompanyID +"  and X_AdmissionNo='"+values+"' and N_AcYearID= "+nAcYearID +" and N_AdmissionID<>" + nAdmissionID, Params, connection, transaction);
                         
                           if(myFunctions.getIntVAL(AdCode.ToString())>0)
                           {
                             transaction.Rollback();
                             return Ok(api.Error(User,"Unable to generate Admission No!...... Admission No already exist")); 
                           }
                       dLayer.ExecuteNonQuery("update inv_invoicecounter set N_lastUsedNo=" + values + " where n_formid=" + this.N_FormID + "and n_companyid=" + nCompanyID + " and N_FnyearID=" + nFnYearId, Params, connection, transaction);
                    }
                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                         Params.Add("N_YearID", nFnYearId);
                        Params.Add("N_FormID", this.N_FormID);
                        Code = dLayer.GetAutoNumber("Sch_Admission", "X_AdmissionNo", Params, connection, transaction);
                        if (Code == "") {
                             transaction.Rollback();
                             return Ok(api.Error(User,"Unable to generate Admission No")); }
                             MasterTable.Rows[0]["X_AdmissionNo"] = Code;

                        //Generating Customer Code
                        
                    
                    }
                    if(nAdmissionID==0)
                    {   
                        CustParams.Add("N_CompanyID", nCompanyID);
                        CustParams.Add("N_YearID", nAcYearID);
                        CustParams.Add("N_FormID", 51);
                        
                        CustCode = dLayer.GetAutoNumber("Inv_Customer", "X_CustomerCode", CustParams, connection, transaction);
                        if (CustCode == "") 
                        { 
                            transaction.Rollback();
                        return Ok(api.Error(User,"Unable to generate Customer Code")); 
                        }
                        
                    }
                    
                     MasterTable.Columns.Remove("n_FnYearId");
                     string image = myFunctions.ContainColumn("i_Photo", MasterTable) ? MasterTable.Rows[0]["i_Photo"].ToString() : "";
                     Byte[] photoBitmap = new Byte[image.Length];
                     photoBitmap = Convert.FromBase64String(image);
                    if (myFunctions.ContainColumn("i_Photo", MasterTable))
                        MasterTable.Columns.Remove("i_Photo");
                        MasterTable.AcceptChanges();

                    if(nAdmissionID>0)
                    {
                        // object PayCount = dLayer.ExecuteScalar("select COUNT(Inv_PayReceiptDetails.N_InventoryID) from Inv_PayReceiptDetails INNER JOIN Sch_Sales ON Sch_Sales.N_CompanyID=Inv_PayReceiptDetails.n_companyid and Sch_Sales.N_RefSalesID=Inv_PayReceiptDetails.N_InventoryID where Inv_PayReceiptDetails.N_CompanyID="+ nCompanyID +" and Inv_PayReceiptDetails.X_TransType='SALES' and Sch_Sales.N_Type=1 and Sch_Sales.N_RefId="+ nAdmissionID, Params, connection, transaction);
                        object PayCount = dLayer.ExecuteScalar("select COUNT(*) from Inv_SalesDetails where N_CompanyID="+ nCompanyID +" and N_SalesID in (select N_SalesId from Inv_Sales where N_CompanyID="+ nCompanyID +" and N_FnYearId="+ nFnYearId +" and N_CustomerId="+ nCustomerID +")", Params, connection, transaction);
                        if (PayCount != null)
                        {
                            if(myFunctions.getIntVAL(PayCount.ToString())==0)
                            {
                                DataTable dtSch_Sales=dLayer.ExecuteDataTable("select N_CompanyId,N_FnYearId,N_RefSalesID,N_SalesID from Sch_Sales where N_CompanyId="+ nCompanyID +" and N_FnYearId="+nAcYearID+" and N_RefId="+nAdmissionID+" and N_Type=1 and ISNULL(N_IsSetupDefault,0)=0",Params,connection,transaction);

                                for (int j = 0; j < dtSch_Sales.Rows.Count; j++)
                                {
                                    SortedList DeleteParams = new SortedList(){
                                    {"N_CompanyID",nCompanyID},
                                    {"X_TransType","SALES"},
                                    {"N_VoucherID",myFunctions.getIntVAL(dtSch_Sales.Rows[j]["N_RefSalesID"].ToString())}};
                                    try
                                    {
                                        dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_SaleAccounts", DeleteParams, connection, transaction);
                                    }
                                    catch (Exception ex)
                                    {
                                        transaction.Rollback();
                                        return Ok(api.Error(User, ex));
                                    }

                                    dLayer.DeleteData("Sch_SalesDetails", "N_SalesID", myFunctions.getIntVAL(dtSch_Sales.Rows[j]["N_SalesID"].ToString()), "N_CompanyID =" + nCompanyID, connection, transaction);                   
                                }
                                dLayer.DeleteData("Sch_Sales", "N_RefId", nAdmissionID, "N_CompanyID =" + nCompanyID+" and N_FnYearId="+nAcYearID+" and N_Type=1 and ISNULL(N_IsSetupDefault,0)=0", connection, transaction);                                                  
                            }
                        }

                    }

                    string DupCriteriaAd = "N_CompanyID=" + nCompanyID + " and N_AcYearID=" + nAcYearID + " and X_AdmissionNo='" + Code + "'";
                    string X_CriteriaAd = "N_CompanyID=" + nCompanyID + " and N_AcYearID=" + nAcYearID;

                    nAdmissionID = dLayer.SaveData("Sch_Admission", "N_AdmissionID",DupCriteriaAd,X_CriteriaAd, MasterTable, connection, transaction);
                    if (nAdmissionID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User,"Unable to save"));
                    }

                    if (image.Length > 0)
                    {
                        dLayer.SaveImage("Sch_Admission", "I_Photo", photoBitmap, "N_AdmissionID",nAdmissionID, connection, transaction);
                    }
                    
                    //----------------------------------Customer Insert-------------------------------------------------------
                    string sqlCommandText = "SELECT N_CompanyID,N_CustomerID,'"+CustCode+"' AS X_CustomerCode,X_Name AS X_CustomerName,X_GaurdianName AS X_ContactName,X_StudentMobile AS X_PhoneNo1,0 AS N_CreditLimit,0 AS B_Inactive," +
                                            " (select N_FieldValue from Acc_AccountDefaults where x_fielddescr='Debtor Account' and N_CompanyID=vw_SchAdmission.N_CompanyID and N_FnYearID=vw_SchAdmission.N_AcYearID) AS N_LedgerID," +
                                            " N_AcYearID AS N_FnYearID,GETDATE() AS D_EntryDate,D_DOB,2 AS N_TypeID,N_BranchId,"+
                                            " (select N_CountryID from Acc_Company where N_CompanyID=vw_SchAdmission.N_CompanyID) AS N_CountryID," +
                                            " (select N_CurrencyID from Acc_Company where N_CompanyID=vw_SchAdmission.N_CompanyID) AS N_CurrencyID, 1 AS N_AllowCashPay" +
                                            " FROM vw_SchAdmission where N_CompanyID="+nCompanyID+" and N_AcYearID="+nAcYearID+" and N_AdmissionID="+ nAdmissionID;

                    dtCustomer = dLayer.ExecuteDataTable(sqlCommandText, Params,connection,transaction);

                    string DupCriteria = "N_CompanyID=" + nCompanyID + " and N_FnYearID=" + nAcYearID + " and X_CustomerCode='" + CustCode + "'";
                    string X_Criteria = "N_CompanyID=" + nCompanyID + " and N_FnYearID=" + nAcYearID;
                    nCustomerID = dLayer.SaveData("Inv_Customer", "n_CustomerID", DupCriteria, X_Criteria, dtCustomer, connection, transaction);
                    if (nCustomerID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User,"Unable to Genrate Customer"));
                    }
                        
                    object N_GroupID = dLayer.ExecuteScalar("Select Isnull(N_FieldValue,0) From Acc_AccountDefaults Where N_CompanyID=" + nCompanyID + " and X_FieldDescr ='Customer Account Group' and N_FnYearID=" + nAcYearID, Params, connection, transaction);
                    string X_LedgerName = "";

                    bool b_AutoGenerate =true;
                    b_AutoGenerate = Convert.ToBoolean(myFunctions.getIntVAL(myFunctions.ReturnSettings("155", "AutoGenerate_CustomerAccount", "N_Value", myFunctions.getIntVAL(nCompanyID.ToString()), dLayer, connection,transaction)));

                    if (b_AutoGenerate)
                    {
                        X_LedgerName = MasterTable.Rows[0]["X_Name"].ToString();
                        if (N_GroupID != null)
                        {
                            object N_LedgerID = dLayer.ExecuteScalar("Select Isnull(N_LedgerID,0) From Acc_MastLedger Where N_CompanyID=" + nCompanyID + " and N_FnYearID=" + nAcYearID + " and X_LedgerName='" + X_LedgerName + "' and N_GroupID=" + myFunctions.getIntVAL(N_GroupID.ToString()), Params, connection, transaction);
                            if (N_LedgerID != null)
                            {
                                dLayer.ExecuteNonQuery("Update Inv_Customer Set N_LedgerID =" + myFunctions.getIntVAL(N_LedgerID.ToString()) + " Where N_CustomerID =" + nCustomerID + " and N_CompanyID=" + nCompanyID + " and N_FnyearID= " + nAcYearID, Params, connection, transaction);
                            }
                            else
                            {
                                dLayer.ExecuteNonQuery("SP_Inv_CreateCustomerAccount " + nCompanyID + "," + nCustomerID + ",'" + CustCode + "','" + X_LedgerName + "'," + myFunctions.GetUserID(User) + "," + nAcYearID + "," + "Customer", Params, connection, transaction);
                            }
                        }
                    }
                    else
                    {
                        object N_LedgerID = dLayer.ExecuteScalar("select Isnull(N_FieldValue,0) from Acc_AccountDefaults where X_FieldDescr='Student Account Ledger' and n_companyid="+nCompanyID+" and N_FnYearID="+nAcYearID, Params, connection, transaction);
                        if (N_LedgerID != null)
                        {
                            dLayer.ExecuteNonQuery("Update Inv_Customer Set N_LedgerID =" + myFunctions.getIntVAL(N_LedgerID.ToString()) + " Where N_CustomerID =" + nCustomerID + " and N_CompanyID=" + nCompanyID + " and N_FnyearID= " + nAcYearID, Params, connection, transaction);
                        }
                    }
                    //--------------------------------------------^^^^^^^^^^^^---------------------------------------------------- 
                    dLayer.ExecuteNonQuery("update Sch_Admission set N_CustomerID="+nCustomerID+"  where N_CompanyID="+nCompanyID+" and N_AcYearID="+nAcYearID+" and N_AdmissionID="+nAdmissionID, Params, connection, transaction);

                    // object PayCount1 = dLayer.ExecuteScalar("select COUNT(Inv_PayReceiptDetails.N_InventoryID) from Inv_PayReceiptDetails INNER JOIN Sch_Sales ON Sch_Sales.N_CompanyID=Inv_PayReceiptDetails.n_companyid and Sch_Sales.N_RefSalesID=Inv_PayReceiptDetails.N_InventoryID where Inv_PayReceiptDetails.N_CompanyID="+ nCompanyID +" and Inv_PayReceiptDetails.X_TransType='SALES'  and Sch_Sales.N_Type=1 and Sch_Sales.N_RefId="+ nAdmissionID, Params, connection, transaction);
                    object PayCount1 = dLayer.ExecuteScalar("select COUNT(*) from Inv_SalesDetails where N_CompanyID="+ nCompanyID +" and N_SalesID in (select N_SalesId from Inv_Sales where N_CompanyID="+ nCompanyID +" and N_FnYearId="+ nFnYearId +" and N_CustomerId="+ nCustomerID +")", Params, connection, transaction);
                    if (PayCount1 != null)
                    {
                        object FeeCount = dLayer.ExecuteScalar("select COUNT(*) from vw_SchStudentFee where N_CompanyID="+ nCompanyID +" and N_AcYearID="+ nAcYearID +" and N_ClassID="+ myFunctions.getIntVAL(MasterTable.Rows[0]["n_ClassID"].ToString()) +" and N_StudentTypeID="+ myFunctions.getIntVAL(MasterTable.Rows[0]["n_StudentCatID"].ToString()), Params, connection, transaction);

                        if (myFunctions.getIntVAL(FeeCount.ToString()) > 0)
                        {
                            if(myFunctions.getIntVAL(PayCount1.ToString())==0)
                            {
                                //--------------------------------------Sch_Sales - SALES - Posting--------------------------------------
                                SortedList SalesParam = new SortedList();
                                SalesParam.Add("N_CompanyID", nCompanyID);
                                SalesParam.Add("N_AcYearID", nAcYearID);
                                SalesParam.Add("N_BranchID", nBranchID);
                                SalesParam.Add("N_LocationID ", nLocationID);
                                SalesParam.Add("N_StudentID ", nAdmissionID);
                                //SalesParam.Add("N_CustomerID ", nCustomerID);
                                SalesParam.Add("D_AdmDate ", Convert.ToDateTime(MasterTable.Rows[0]["D_AdmissionDate"].ToString()));
                                SalesParam.Add("N_UserID ", nUserID);
                                SalesParam.Add("N_Type ", 1);
                                SalesParam.Add("N_BusRegID ", 0);
                                try
                                {
                                    dLayer.ExecuteNonQueryPro("SP_StudentAdmFee_Insert", SalesParam, connection, transaction);
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    return Ok(api.Error(User, ex));
                                }
                            
                                //----------------------------------------^^^^^^^^^^^^^^^^^^^^^^^^-------------------------------------
                            }
                        }
                    }
                    if(CourseTable.Rows.Count>0)
                    {
                    for (int i = 0; i < CourseTable.Rows.Count; i++)
                    {
                    CourseTable.Rows[i]["N_StudentID"] = nAdmissionID;
                    }
                    dLayer.SaveData("Sch_CourseDetails", "N_CourseDetailsID", CourseTable, connection, transaction);
                    transaction.Commit();
                    }
                    

                      if (xEmail != "")
                        {
                            using (SqlConnection olivoCon = new SqlConnection(masterDBConnectionString))
                            {
                                olivoCon.Open();
                                SqlTransaction olivoTxn = olivoCon.BeginTransaction();
                                string Pwd = myFunctions.EncryptString(xEmail);
                                int nClientID = myFunctions.GetClientID(User);
                                    object glogalUserID = dLayer.ExecuteScalar("SELECT N_UserID FROM Users where x_EmailID='" + xEmail.ToString() + "' and N_ClientID=" + nClientID, olivoCon, olivoTxn);
                                if (glogalUserID == null)
                                {
                                    DataTable dtGobal = new DataTable();
                                    dtGobal.Clear();
                                    dtGobal.Columns.Add("X_EmailID");
                                    dtGobal.Columns.Add("N_UserID");
                                    dtGobal.Columns.Add("X_UserName");
                                    dtGobal.Columns.Add("N_ClientID");
                                    dtGobal.Columns.Add("N_ActiveAppID");
                                    dtGobal.Columns.Add("X_Password");
                                    dtGobal.Columns.Add("B_Inactive");
                                    dtGobal.Columns.Add("X_UserID");
                                    dtGobal.Columns.Add("B_EmailVerified");
                                    dtGobal.Columns.Add("N_UserType");

                                    DataRow rowGb = dtGobal.NewRow();
                                    rowGb["X_EmailID"] = xEmail;
                                    rowGb["X_UserName"] = xEmail;
                                    rowGb["N_ClientID"] = nClientID;
                                    rowGb["N_ActiveAppID"] = 20;
                                    rowGb["X_Password"] = Pwd;
                                    rowGb["B_Inactive"] = 0;
                                    rowGb["X_UserID"] = xEmail;
                                    rowGb["B_EmailVerified"] = 1;
                                    rowGb["N_UserType"] = 2;
                                    dtGobal.Rows.Add(rowGb);

                                    int GlobalUserID = dLayer.SaveData("Users", "N_UserID", dtGobal, olivoCon, olivoTxn);
                                    if (GlobalUserID > 0)
                                    {
                                        olivoTxn.Commit();
                                    }
                                }
                            
                                object objUserID = dLayer.ExecuteScalar("Select N_UserID from Sec_User where N_CompanyID=" + nCompanyID + "  and N_studentID=" + nAdmissionID + " and X_UserID='" + xEmail.ToString() + "'", connection, transaction);
                                 if (objUserID == null)
                                {
                                    object objUserCat = dLayer.ExecuteScalar("Select N_UserCategoryID from Sec_UserCategory where N_CompanyID=" + nCompanyID + "  and N_AppID=20", connection, transaction);
                                     if (objUserCat != null)
                                    {
                                        object objUserCheck = dLayer.ExecuteScalar("Select X_UserID from Sec_User where N_CompanyID=" + nCompanyID + "  and X_UserID='" + xEmail.ToString() + "' and N_studentID=" + nAdmissionID + " and N_UserCategoryID=" + myFunctions.getIntVAL(objUserCat.ToString()), connection, transaction);
                                         if (objUserCheck == null)
                                        {
                                             object objUserCheckng = dLayer.ExecuteScalar("Select X_UserID from Sec_User where N_CompanyID=" + nCompanyID + " and N_studentID=" + nAdmissionID, connection, transaction);
                                            if (objUserCheckng == null)
                                            {
                                                 object objUser = dLayer.ExecuteScalar("Select X_UserID from Sec_User where N_CompanyID=" + nCompanyID + "  and X_UserID='" + xEmail.ToString() + "'", connection, transaction);
                                                 if (objUser != null)
                                                {
                                                    dLayer.ExecuteNonQuery("update  Sec_User set N_studentID=" + nAdmissionID + ",B_Active= 1,N_UserCategoryID=" + myFunctions.getIntVAL(objUserCat.ToString()) + ",X_UserCategoryList=" + objUserCat.ToString() + " where X_UserID='" + xEmail.ToString() + "' and N_CompanyID= " + nCompanyID, Params, connection, transaction);
                                                }
                                                else
                                                {
                                                    DataTable dt = new DataTable();
                                                    dt.Clear();
                                                    dt.Columns.Add("N_CompanyID");
                                                    dt.Columns.Add("N_UserID");
                                                    dt.Columns.Add("X_UserID");
                                                    dt.Columns.Add("X_Password");
                                                    dt.Columns.Add("N_UserCategoryID");
                                                    dt.Columns.Add("B_Active");
                                                    dt.Columns.Add("N_BranchID");
                                                    dt.Columns.Add("N_LocationID");
                                                    dt.Columns.Add("X_UserName");
                                                    dt.Columns.Add("N_studentID");
                                                    dt.Columns.Add("N_LoginFlag");
                                                    dt.Columns.Add("X_UserCategoryList");
                                                    dt.Columns.Add("X_Email");
                                                    dt.Columns.Add("N_TypeID");

                                                    DataRow row = dt.NewRow();
                                                    row["N_CompanyID"] = nCompanyID;
                                                    row["X_UserID"] = xEmail;
                                                    row["X_Password"] = Pwd;
                                                    row["N_UserCategoryID"] = myFunctions.getIntVAL(objUserCat.ToString());
                                                    row["B_Active"] = 1;
                                                    row["N_BranchID"] = nBranchID;
                                                    row["N_LocationID"] = nLocationID;
                                                    row["X_UserName"] = xEmail;
                                                    row["N_studentID"] = nAdmissionID;
                                                    row["N_LoginFlag"] = 2;
                                                    row["X_UserCategoryList"] = objUserCat.ToString();
                                                    row["X_Email"] = xEmail;
                                                    row["N_TypeID"] = 3;
                                                    dt.Rows.Add(row);

                                                    int UserID = dLayer.SaveData("Sec_User", "N_UserID", dt, connection, transaction);

                                                  object SUAUserID = dLayer.ExecuteScalar("SELECT N_UserID FROM Users where x_EmailID='" + xEmail.ToString() + "' and N_ClientID=" + nClientID, olivoCon, olivoTxn);
                                                  DataTable dtUA = new DataTable();
                                                    dtUA.Clear();
                                                    dtUA.Columns.Add("N_CompanyID");
                                                    dtUA.Columns.Add("N_UserID");
                                                    dtUA.Columns.Add("N_AppMappingID");
                                                    dtUA.Columns.Add("N_AppID");
                                                     dtUA.Columns.Add("X_LandingPage");
                                                     dtUA.Columns.Add("N_GlobalUserID");
                                                    DataRow rowUA = dtUA.NewRow();
                                                    rowUA["N_CompanyID"] = nCompanyID;
                                                    rowUA["N_UserID"] = UserID;
                                                    rowUA["N_AppMappingID"] = 0;
                                                    rowUA["N_AppID"] =20;
                                                    rowUA["X_LandingPage"] = null;
                                                     rowUA["N_GlobalUserID"] = SUAUserID;
                                                    dtUA.Rows.Add(rowUA);
                                                    int UAUserID = dLayer.SaveData("sec_userapps", "N_AppMappingID", dtUA, connection, transaction);

                            
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        }

                    transaction.Commit();
                    SortedList Result = new SortedList();
                    Result.Add("N_AdmissionID", nAdmissionID);
                    Result.Add("X_AdmissionNo", X_AdmissionNo);
                    return Ok(api.Success(Result,"Admission Completed"));

                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }
        }

        [HttpGet("detailList") ]
        public ActionResult AdmissionList(int nCompanyID,int nAcYearID,int n_ClassID,int n_DivisionID)
        {    
            SortedList param = new SortedList();           
            DataTable dt=new DataTable();
            
            string sqlCommandText="";

            if(n_ClassID!=0)
            {
                if(n_DivisionID!=0)
                    sqlCommandText="select * from vw_SchAdmission where N_CompanyID=@p1 and n_AcYearID=@p2 and n_ClassID=@p3 and n_DivisionID=@p4 and isNull(N_Inactive,0)=0 ";
                else                    
                    sqlCommandText="select * from vw_SchAdmission where N_CompanyID=@p1 and n_AcYearID=@p2 and n_ClassID=@p3 and isNull(N_Inactive,0)=0 ";
            }
            else
            {
                if(n_DivisionID!=0)
                    sqlCommandText="select * from vw_SchAdmission where N_CompanyID=@p1 and n_AcYearID=@p2 and n_DivisionID=@p4 and isNull(N_Inactive,0)=0 ";
                else                    
                    sqlCommandText="select * from vw_SchAdmission where N_CompanyID=@p1 and n_AcYearID=@p2 and isNull(N_Inactive,0)=0 ";
            }

            param.Add("@p1", nCompanyID);             
            param.Add("@p2", nAcYearID);             
            param.Add("@p3", n_ClassID);             
            param.Add("@p4", n_DivisionID);             
                
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    dt=dLayer.ExecuteDataTable(sqlCommandText,param,connection);
                }
                if(dt.Rows.Count==0)
                {
                   return Ok(api.Success(dt));
                }
                else
                {
                    return Ok(api.Success(dt));
                }              
            }
            catch(Exception e)
            {
                return Ok(api.Error(User,e));
            }   
        }   
      
    [HttpDelete("delete")]
        public ActionResult DeleteData(int nAdmissionID ,int nFnYearId,int nCustomerID)
        {

            int Results = 0;
            int nCompanyID=myFunctions.GetCompanyID(User);
            try
            {                        
                SortedList Params = new SortedList();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    if ( nAdmissionID > 0)
                    {
                        object admCount = dLayer.ExecuteScalar("select count(1) From Sch_BusRegistration where N_AdmissionID =" + nAdmissionID + " and N_CompanyID =" + nCompanyID + " and N_FnYearID=" + nFnYearId , connection, transaction);
                        admCount = admCount == null ? 0 : admCount;
                        if (myFunctions.getIntVAL(admCount.ToString()) > 0)
                            return Ok(api.Error(User, "Already In Use !!"));
                    }
                    object PayCount = dLayer.ExecuteScalar("select COUNT(Inv_PayReceiptDetails.N_InventoryID) from Inv_PayReceiptDetails INNER JOIN Sch_Sales ON Sch_Sales.N_CompanyID=Inv_PayReceiptDetails.n_companyid and Sch_Sales.N_RefSalesID=Inv_PayReceiptDetails.N_InventoryID where Inv_PayReceiptDetails.N_CompanyID="+ nCompanyID +" and Inv_PayReceiptDetails.X_TransType='SALES' and Sch_Sales.N_RefId="+ nAdmissionID, Params, connection, transaction);
                    if (PayCount != null)
                    {
                        if(myFunctions.getIntVAL(PayCount.ToString())==0)
                        {
                            DataTable dtSch_Sales=dLayer.ExecuteDataTable("select N_CompanyId,N_FnYearId,N_RefSalesID,N_SalesID from Sch_Sales where N_CompanyId="+ nCompanyID +" and N_RefId="+nAdmissionID,Params,connection,transaction);

                            for (int j = 0; j < dtSch_Sales.Rows.Count; j++)
                            {
                                SortedList DeleteParams = new SortedList(){
                                {"N_CompanyID",nCompanyID},
                                {"X_TransType","SALES"},
                                {"N_VoucherID",myFunctions.getIntVAL(dtSch_Sales.Rows[j]["N_RefSalesID"].ToString())}};
                                try
                                {
                                    dLayer.ExecuteNonQueryPro("SP_Delete_Trans_With_SaleAccounts", DeleteParams, connection, transaction);
                                }
                                catch (Exception ex)
                                {
                                    transaction.Rollback();
                                    return Ok(api.Error(User, ex));
                                }

                                dLayer.DeleteData("Sch_SalesDetails", "N_SalesID", myFunctions.getIntVAL(dtSch_Sales.Rows[j]["N_SalesID"].ToString()), "N_CompanyID =" + nCompanyID, connection, transaction);                   
                            }
                             object customerCount = dLayer.ExecuteScalar("select count(1) from Inv_Sales  where N_CompanyID="+ nCompanyID +"  and N_CustomerId="+nCustomerID, Params, connection, transaction);
                         
                           if(myFunctions.getIntVAL(customerCount.ToString())>0)
                           {
                             transaction.Rollback();
                             return Ok(api.Error(User,"Unable to delete Customer already exist in sales")); 
                           }

                            dLayer.DeleteData("Sch_Sales", "N_RefID", nAdmissionID, "N_CompanyID =" + nCompanyID , connection, transaction);    
                                                                   
                            dLayer.ExecuteNonQuery("delete from Inv_Customer where N_CompanyID="+nCompanyID+" and N_CustomerID = (select N_CustomerID from Sch_Admission where N_CompanyID="+nCompanyID+" and N_AdmissionID= "+nAdmissionID+")", Params, connection, transaction);
                            Results = dLayer.DeleteData("Sch_Admission", "n_AdmissionID", nAdmissionID, "N_CompanyID =" + nCompanyID, connection, transaction);                   
                            if (Results > 0)
                            {
                                transaction.Commit();
                                return Ok(api.Success("Student deleted"));
                            }
                            else
                            {
                                return Ok(api.Error(User,"Unable to delete Student"));
                            }
                        }
                        else
                        {
                            return Ok(api.Error(User,"Unable to delete Student"));
                        }
                    }    
                    else
                    {
                        return Ok(api.Error(User,"Unable to delete Student"));
                    }                                                
                }


            }
            catch (Exception ex)
            {
                return Ok(api.Error(User,ex));
            }
        }

        [HttpGet("feeDetails")]
        public ActionResult GetFeeDetails(int nAcYearID,int nClassID,int nStudentTypeID)
        {
            DataSet dt=new DataSet();
            DataTable FeeDetailsTable = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId=myFunctions.GetCompanyID(User);
            string sqlCommandText = "";
            
            sqlCommandText = "select N_CompanyID,N_AcYearID,X_FeeDescription,N_Amount,N_DiscountAmt,N_FeeCodeID,N_FrequencyID,N_Frequency,N_Installment from vw_SchStudentFee where N_CompanyID=@p1 and N_AcYearID=@p2 and N_ClassID=@p3 and N_StudentTypeID=@p4";
            
            Params.Add("@p1", nCompanyId);  
            Params.Add("@p2", nAcYearID);
            Params.Add("@p3", nClassID);
            Params.Add("@p4", nStudentTypeID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    FeeDetailsTable = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);

                    if (FeeDetailsTable.Rows.Count == 0)
                    {
                        return Ok(api.Success(dt));  
                    }
                
                    FeeDetailsTable = api.Format(FeeDetailsTable, "FeeDetails");
                    dt.Tables.Add(FeeDetailsTable);
                }
                return Ok(api.Success(dt));               
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }

        [HttpGet("stdCustomerlist") ]
        public ActionResult StdCustomerList(int nCompanyID, int nFnYearId, int nBranchId, bool bAllBranchesData, int nClassID, int nDivisionID)
        {    
            SortedList param = new SortedList();           
            DataTable dt=new DataTable();
            string sqlCommandText="";
            string X_Crieteria="";
            string condition="";

            if (bAllBranchesData == true)
            { X_Crieteria=" where N_CompanyID=@p1 and N_FnYearID=@p2 and isNull(N_Inactive, 0)=0 and ISNULL(N_EnablePopup,0)=0"; }
            else
            {
                X_Crieteria=" where N_CompanyID=@p1 and N_FnYearID=@p2 and isNull(N_Inactive, 0)=0 and ISNULL(N_EnablePopup,0)=0 and (N_BranchID=0 or N_BranchID=@p3)";
                param.Add("@p3", nBranchId);
            }

            if (nClassID > 0 && nDivisionID > 0)
            {
                condition=" and N_ClassID=@p4 and N_ClassDivisionID=@p5";
                param.Add("@p4", nClassID);
                param.Add("@p5", nDivisionID);
            }

            sqlCommandText="select * from vw_InvCustomer_Student "+X_Crieteria+""+condition+" order by N_CustomerID DESC";
            param.Add("@p1", nCompanyID);
            param.Add("@p2", nFnYearId); 
                
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    dt=dLayer.ExecuteDataTable(sqlCommandText,param,connection);
                }
                if(dt.Rows.Count==0)
                {
                    return Ok(api.Notice("No Results Found"));
                }
                else
                {
                    return Ok(api.Success(dt));
                }
            }
            catch(Exception e)
            {
                return Ok(api.Error(User,e));
            }   
        }  
    }
}

