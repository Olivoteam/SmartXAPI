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
using System.Net.Mail;
using System.Collections.Generic;

namespace SmartxAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("emailtemplate")]
    [ApiController]
    public class Gen_EmailTemplate : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        private readonly int N_FormID = 1348;
        private readonly IMyAttachments myAttachments;

        public Gen_EmailTemplate(IDataAccessLayer dl, IApiFunctions _api, IMyFunctions myFun, IConfiguration conf, IMyAttachments myAtt)
        {
            api = _api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
            myAttachments = myAtt;
        }
        [AllowAnonymous]


        [HttpPost("send")]
        public ActionResult SendData([FromBody] DataSet ds)
        {

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    int companyid = myFunctions.GetCompanyID(User);
                    DataTable Master = ds.Tables["master"];
                    DataRow MasterRow = Master.Rows[0];
                    SortedList Params = new SortedList();
                    string Toemail = "";
                    string Email = MasterRow["X_ContactEmail"].ToString();
                    string Body = MasterRow["X_Body"].ToString();
                    string Subjectval = MasterRow["x_TempSubject"].ToString();
                    int nTemplateID = myFunctions.getIntVAL(MasterRow["n_TemplateID"].ToString());
                    int nopportunityID = myFunctions.getIntVAL(MasterRow["N_OpportunityID"].ToString());
                    Toemail = Email.ToString();
                    object companyemail = "";
                    object companypassword = "";
                    object Company, Oppportunity, Contact, CustomerID;
                    int nCompanyId = myFunctions.GetCompanyID(User);

                    companyemail = dLayer.ExecuteScalar("select X_Value from Gen_Settings where X_Group='210' and X_Description='EmailAddress' and N_CompanyID=" + companyid, Params, connection, transaction);
                    companypassword = dLayer.ExecuteScalar("select X_Value from Gen_Settings where X_Group='210' and X_Description='EmailPassword' and N_CompanyID=" + companyid, Params, connection, transaction);

                    string Subject = "";
                    if (Toemail.ToString() != "")
                    {
                        if (companyemail.ToString() != "")
                        {
                            object body = null;
                            string MailBody;
                            body = Body;
                            if (body != null)
                            {
                                body = body.ToString();
                            }
                            else
                                body = "";

                            if (nopportunityID > 0)
                            {
                                Oppportunity = dLayer.ExecuteScalar("select x_Opportunity from vw_CRMOpportunity where N_CompanyID =" + nCompanyId + " and N_OpportunityID=" + nopportunityID, Params, connection, transaction);
                                Contact = dLayer.ExecuteScalar("Select x_Contact from vw_CRMOpportunity where N_CompanyID=" + nCompanyId + " and N_OpportunityID=" + nopportunityID, Params, connection, transaction);
                                Company = dLayer.ExecuteScalar("select x_customer from vw_CRMOpportunity where N_CompanyID =" + nCompanyId + " and N_OpportunityID=" + nopportunityID, Params, connection, transaction);
                                CustomerID = dLayer.ExecuteScalar("select N_CustomerID from vw_CRMOpportunity where N_CompanyID =" + nCompanyId + " and N_OpportunityID=" + nopportunityID, Params, connection, transaction);


                                Body = Body.ToString().Replace("@CompanyName", Company.ToString());
                                Body = Body.ToString().Replace("@ContactName", Contact.ToString());
                                Body = Body.ToString().Replace("@LeadName", Oppportunity.ToString());

                                Subjectval = Subjectval.ToString().Replace("@CompanyName", Company.ToString());
                                Subjectval = Subjectval.ToString().Replace("@ContactName", Contact.ToString());
                                Subjectval = Subjectval.ToString().Replace("@LeadName", Oppportunity.ToString());


                            }

                            string Sender = companyemail.ToString();
                            Subject = Subjectval;
                            MailBody = body.ToString();
                            myFunctions.SendMail(Toemail, Body, Subject, dLayer, 1348, nTemplateID, companyid);

                        }
                    }
                    Master.Columns.Remove("x_TemplateCode");
                    Master.Columns.Remove("x_TemplateName");
                    Master.Columns.Remove("n_TemplateID");
                    Master.Columns.Remove("x_TempSubject");
                    if (Master.Columns.Contains("n_PkeyId"))
                        Master.Columns.Remove("n_PkeyId");
                    if (Master.Columns.Contains("n_PkeyIdSub"))
                        Master.Columns.Remove("n_PkeyIdSub");
                    Master = myFunctions.AddNewColumnToDataTable(Master, "N_MailLogID", typeof(int), 0);
                    Master = myFunctions.AddNewColumnToDataTable(Master, "X_Subject", typeof(string), Subject);
                    Master.Columns.Remove("X_Body");

                    int N_LogID = dLayer.SaveData("Gen_MailLog", "N_MailLogID", Master, connection, transaction);
                    transaction.Commit();

                    return Ok(api.Success("Email Send"));


                }
            }

            catch (Exception ie)
            {
                return Ok(api.Error(User, ie));
            }
        }
        public static string GetCCMail(int ID, int nCompanyID, SqlConnection connection, SqlTransaction transaction, IDataAccessLayer dLayer)
        {
            SortedList Params = new SortedList();
            object CCMail = dLayer.ExecuteScalar("select X_CCMail from Gen_EmailAddresses where N_subjectID =" + ID + " and N_CompanyID=" + nCompanyID, Params, connection, transaction);
            if (CCMail != null)
                return CCMail.ToString();
            else
                return "";
        }
        public static string GetBCCMail(int ID, int nCompanyID, SqlConnection connection, SqlTransaction transaction, IDataAccessLayer dLayer)
        {
            SortedList Params = new SortedList();
            object BCCMail = dLayer.ExecuteScalar("select X_BCCMail from Gen_EmailAddresses where N_subjectID =" + ID + " and N_CompanyID=" + nCompanyID, Params, connection, transaction);
            if (BCCMail != null)
                return BCCMail.ToString();
            else
                return "";
        }

        [HttpPost("save")]
        public ActionResult SaveData([FromBody] DataSet ds)
        {
            try
            {
                DataTable MasterTable;
                MasterTable = ds.Tables["master"];
                DataTable Attachment = ds.Tables["attachments"];
                int nCompanyID = myFunctions.getIntVAL(MasterTable.Rows[0]["n_CompanyId"].ToString());
                int nFnYearId = myFunctions.getIntVAL(MasterTable.Rows[0]["n_FnYearId"].ToString());
                int nTemplateID = 0;


                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    SortedList Params = new SortedList();
                    // Auto Gen
                    string TemplateCode = "";
                    var values = MasterTable.Rows[0]["X_TemplateCode"].ToString();

                    if (values == "@Auto")
                    {
                        Params.Add("N_CompanyID", nCompanyID);
                        Params.Add("N_YearID", nFnYearId);
                        Params.Add("N_FormID", 1302);
                        TemplateCode = dLayer.GetAutoNumber("Gen_MailTemplates", "X_TemplateCode", Params, connection, transaction);
                        if (TemplateCode == "") { transaction.Rollback(); return Ok(api.Error(User, "Unable to generate Code")); }
                        MasterTable.Rows[0]["X_TemplateCode"] = TemplateCode;
                    }
                    var X_Body = MasterTable.Rows[0]["X_Body"].ToString();
                    MasterTable.Columns.Remove("X_Body");
                    if (MasterTable.Columns.Contains("n_PkeyIdSub"))
                        MasterTable.Columns.Remove("n_PkeyIdSub");
                    if (MasterTable.Columns.Contains("n_PkeyId"))
                        MasterTable.Columns.Remove("n_PkeyId");

                    nTemplateID = dLayer.SaveData("Gen_MailTemplates", "N_TemplateID", MasterTable, connection, transaction);

                    string payCode = MasterTable.Rows[0]["X_TemplateCode"].ToString();
                    int payId = nTemplateID;


                    //  string partyName= Attachment.Rows[0]["x_PartyName"].ToString();
                    if (Attachment.Rows.Count > 0)
                    {
                        string partyCode = Attachment.Rows[0]["x_PartyCode"].ToString();
                        int partyID = myFunctions.getIntVAL(Attachment.Rows[0]["n_PartyID"].ToString());
                        Attachment.Columns.Remove("x_FolderName");

                        Attachment.Columns.Remove("x_PartyCode");
                        Attachment.Columns.Remove("x_TransCode");
                        if (Attachment.Columns.Contains("n_PartyID1"))
                            Attachment.Columns.Remove("n_PartyID1");
                        if (Attachment.Columns.Contains("n_ActionID"))
                            Attachment.Columns.Remove("n_ActionID");
                        if (Attachment.Columns.Contains("tempFileName"))
                            Attachment.Columns.Remove("tempFileName");

                        Attachment.AcceptChanges();
                        myAttachments.SaveAttachment(dLayer, Attachment, payCode, payId, "", partyCode, partyID, "Email", User, connection, transaction);
                    }

                    if (nTemplateID <= 0)
                    {
                        transaction.Rollback();
                        return Ok(api.Error(User, "Unable to save"));
                    }
                    else
                    {
                        dLayer.ExecuteNonQuery("update Gen_MailTemplates set X_Body='" + X_Body + "' where N_CompanyID=@N_CompanyID and N_TemplateID=" + nTemplateID, Params, connection, transaction);
                        transaction.Commit();

                        return Ok(api.Success("Mail Template Created"));
                    }
                }
            }
            catch (Exception ex)
            {
                return Ok(api.Error(User, ex));
            }
        }

        [HttpGet("list")]
        public ActionResult TemplateList()
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            string sqlCommandCount = "";

            string sqlCommandText = "";
            string Criteria = "";


            sqlCommandText = "select  * from Gen_MailTemplates where N_CompanyID=@p1";
            Params.Add("@p1", nCompanyId);

            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    sqlCommandCount = "select count(*) as N_Count  from Gen_MailTemplates where N_CompanyID=@p1";
                    object TotalCount = dLayer.ExecuteScalar(sqlCommandCount, Params, connection);
                    OutPut.Add("Details", api.Format(dt));
                    OutPut.Add("TotalCount", TotalCount);
                    if (dt.Rows.Count == 0)
                    {
                        // return Ok(api.Warning("No Results Found"));
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
        public ActionResult TemplateListDetails(string n_TemplateID)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            string sqlCommandText = "";

            sqlCommandText = "select  * from Gen_MailTemplates where N_CompanyID=@p1 and N_TemplateID=@p2";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", n_TemplateID);

            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    if (dt.Rows.Count == 0)
                    {
                        return Ok(api.Warning("No Results Found"));
                    }
                    else
                    {
                        return Ok(api.Success(dt));
                    }

                }

            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }
        [HttpDelete("delete")]
        public ActionResult DeleteData(int nTemplateID)
        {

            int Results = 0;
            try
            {


                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    Results = dLayer.DeleteData("Gen_MailTemplates", "N_TemplateID", nTemplateID, "", connection, transaction);
                    transaction.Commit();
                }
                if (Results > 0)
                {
                    Dictionary<string, string> res = new Dictionary<string, string>();
                    res.Add("N_TemplateID", nTemplateID.ToString());
                    return Ok(api.Success(res, "Email Template deleted"));
                }
                else
                {
                    return Ok(api.Error(User, "Unable to delete Email Template"));
                }

            }
            catch (Exception ex)
            {
                return Ok(api.Error(User, ex));
            }



        }


        [HttpGet("mailList")]
        public ActionResult GenMailList(string type, int pKeyID, int nFnYearID)
        {
            int nCompanyId = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);

            string sqlCommandText = "";

            if (type == "RFQ")
            {

                sqlCommandText = "SELECT        Inv_RFQVendorList.N_QuotationID as N_PKeyID,Inv_RFQVendorList.N_VendorID as N_PartyID,Inv_Vendor.X_VendorCode as X_PartyCode, Sec_User.N_UserID, Sec_User.X_UserID, Inv_Vendor.X_Email, Inv_Vendor.X_VendorName as X_PartyName,'Vendor' as X_PartyType,'RFQ' as X_TxnType " +
    " FROM            Sec_User RIGHT OUTER JOIN " +
     "                        Inv_Vendor ON Sec_User.N_CustomerID = Inv_Vendor.N_VendorID AND Sec_User.N_CompanyID = Inv_Vendor.N_CompanyID RIGHT OUTER JOIN " +
     "                        Inv_RFQVendorList ON Inv_Vendor.N_VendorID = Inv_RFQVendorList.N_VendorID AND Inv_Vendor.N_CompanyID = Inv_RFQVendorList.N_CompanyID" +
     " where Inv_Vendor.N_FnYearID=@nFnYearID and Inv_RFQVendorList.N_CompanyID=@nCompanyID and Inv_RFQVendorList.N_QuotationID=@nPkeyID group by Inv_RFQVendorList.N_QuotationID,Inv_RFQVendorList.N_VendorID ,Inv_Vendor.X_VendorCode, Sec_User.N_UserID, Sec_User.X_UserID, Inv_Vendor.X_Email, Inv_Vendor.X_VendorName ";
            }
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();


            Params.Add("@nFnYearID", nFnYearID);
            Params.Add("@nCompanyID", nCompanyId);
            Params.Add("@nPkeyID", pKeyID);


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    return Ok(api.Success(api.Format(dt, "details")));
                }

            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }


        [HttpPost("processMailList")]
        public ActionResult ProcessMailList([FromBody] DataSet ds)
        {

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    int companyid = myFunctions.GetCompanyID(User);
                    DataTable Master = ds.Tables["master"];
                    
                    
                    foreach (DataRow row in Master.Rows)
                    {
                        string xBodyText="";
                        string xSubject="";
                        string xURL = "";
                        if(row["X_TxnType"].ToString()=="RFQ"){
                            if(myFunctions.CreatePortalUser(companyid,myFunctions.getIntVAL(row["N_BranchID"].ToString()),row["X_PartyName"].ToString(),row["X_Email"].ToString(),row["X_PartyType"].ToString(),row["X_PartyCode"].ToString(),myFunctions.getIntVAL(row["N_PartyID"].ToString()),true,dLayer,connection,transaction)){
                                xSubject = "RFQ Inward";
                                xBodyText = "RFQ Inward";
                                

                            }
                        }
                    }


                    return Ok(api.Success("Email Send"));


                }
            }

            catch (Exception ie)
            {
                return Ok(api.Error(User, ie));
            }
        }
    }
}