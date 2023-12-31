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
    [Route("LeadsDashboard")]
    [ApiController]
    public class CRM_LeadsDashboard : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;

        public CRM_LeadsDashboard(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }
        [HttpGet("listDetails")]
        public ActionResult GetCustomerDetails(string xOpportunityCode)
        {
            DataSet dt = new DataSet();
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);

            //string sqlCommandActivitiesList = "select * from vw_CRM_Activity where N_CompanyID=@p1 and X_OpportunityCode=@p2 order by N_Order";
            string sqlCommandActivitiesList = "select * from vw_Tsk_TaskMaster where N_CompanyID=@p1 and n_opportunityID=@p3 order by N_Order";
            string sqlCommandLeadsList = "select CONVERT(varchar,d_EntryDate,101) as d_Entry,* from vw_CRMOpportunity where N_CompanyID =@p1 and X_OpportunityCode=@p2";
            string sqlCommandContactList = "Select * from vw_CRMContact where N_CompanyID=@p1 and X_OpportunityCode=@p2";
            string sqlCommandQuotationList = "Select * from inv_salesquotation where N_CompanyID=@p1 and n_opportunityID=@p3";
            string sqlCommandOrderList = "Select * from inv_salesOrder where N_CompanyID=@p1 and n_opportunityID=@p3";
            string sqlCommandinvoiceList = "Select * from inv_sales where N_CompanyID=@p1 and n_opportunityID=@p3";
            string sqlCommandMailLogList = "Select CONVERT(VARCHAR(10), d_Date, 103) + ' '  + convert(VARCHAR(8), d_Date, 14) as d_Entry,* from Gen_MailLog where N_CompanyID=@p1 and N_OpportunityID=@p3 order by N_maillogid desc";
            string sqlCommandProjectList = "Select CONVERT(varchar,d_StartDate,101) as d_Start,CONVERT(varchar,d_EndDate,101) as d_End,* from crm_Project where N_CompanyID=@p1 and N_ProjectID=@p5";

            Params.Add("@p1", nCompanyID);
            Params.Add("@p2", xOpportunityCode);

            DataTable ActivitiesList = new DataTable();
            DataTable LeadsList = new DataTable();
            DataTable ContactList = new DataTable();
            DataTable QuotationList = new DataTable();
            DataTable OrderList = new DataTable();
            DataTable InvoiceList = new DataTable();
            DataTable MailLogList = new DataTable();
            DataTable ProjectList = new DataTable();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    object N_OpportunityID = dLayer.ExecuteScalar("select N_opportunityID from crm_opportunity where X_OpportunityCode=@p2 and N_CompanyID=@p1", Params, connection);
                    object N_ProjectID = dLayer.ExecuteScalar("select N_ProjectID from crm_opportunity where X_OpportunityCode=@p2 and N_CompanyID=@p1", Params, connection);
                    Params.Add("@p3", N_OpportunityID);
                    

                    object N_Quotationid = dLayer.ExecuteScalar("select n_quotationid from inv_salesquotation where N_OpportunityID=@p3 and N_CompanyID=@p1", Params, connection);
                    if (N_OpportunityID != null)
                    {
                        InvoiceList = dLayer.ExecuteDataTable(sqlCommandinvoiceList, Params, connection);
                        InvoiceList = api.Format(InvoiceList, "InvoiceList");
                        dt.Tables.Add(InvoiceList);
                    }
                    if (N_ProjectID != null)
                    {
                        Params.Add("@p5", N_ProjectID);
                        ProjectList = dLayer.ExecuteDataTable(sqlCommandProjectList, Params, connection);
                        ProjectList = api.Format(ProjectList, "ProjectList");
                        dt.Tables.Add(ProjectList);
                    }

                    ActivitiesList = dLayer.ExecuteDataTable(sqlCommandActivitiesList, Params, connection);
                    LeadsList = dLayer.ExecuteDataTable(sqlCommandLeadsList, Params, connection);
                    ContactList = dLayer.ExecuteDataTable(sqlCommandContactList, Params, connection);
                    QuotationList = dLayer.ExecuteDataTable(sqlCommandQuotationList, Params, connection);
                    OrderList = dLayer.ExecuteDataTable(sqlCommandOrderList, Params, connection);
                    MailLogList = dLayer.ExecuteDataTable(sqlCommandMailLogList, Params, connection);


                    ActivitiesList = api.Format(ActivitiesList, "ActivitiesList");
                    LeadsList = api.Format(LeadsList, "LeadsList");
                    ContactList = api.Format(ContactList, "ContactList");
                    QuotationList = api.Format(QuotationList, "QuotationList");
                    MailLogList = api.Format(MailLogList, "MailLogList");
                    OrderList = api.Format(OrderList, "OrderList");



                    dt.Tables.Add(ActivitiesList);
                    dt.Tables.Add(LeadsList);
                    dt.Tables.Add(ContactList);
                    dt.Tables.Add(QuotationList);
                    dt.Tables.Add(MailLogList);
                    dt.Tables.Add(OrderList);

                    return Ok(api.Success(dt));

                }
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }
        [HttpGet("update")]
        public ActionResult ActivityUpdate(string xActivityCode, bool bFlag,int nStageID,int nOpportunityID)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            string sqlCommandText = "";
            if (bFlag)
                sqlCommandText = "update crm_activity set b_closed=1,x_status='Closed'  where N_CompanyID=@p1 and X_ActivityCode=@p2";
            else
                sqlCommandText = "update crm_activity set b_closed=0,x_status='Active'  where N_CompanyID=@p1 and X_ActivityCode=@p2";
            Params.Add("@p1", nCompanyId);
            Params.Add("@p2", xActivityCode);
            Params.Add("@p3", nStageID);
            Params.Add("@p4", nOpportunityID);


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dLayer.ExecuteNonQuery(sqlCommandText, Params, connection);
                    if(nStageID>0)
                        dLayer.ExecuteNonQuery("update crm_opportunity set N_StageID=@p3 where N_CompanyID=@p1 and N_OpportunityID=@p4", Params, connection);
                }
                return Ok(api.Warning("Activity Updated"));

            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }
        // [HttpGet("notesupdate")]
        // public ActionResult NotesUpdate(string xOpportunityCode, string xnotes)
        // {
        //     DataTable dt = new DataTable();
        //     SortedList Params = new SortedList();
        //     int nCompanyId = myFunctions.GetCompanyID(User);
        //     string sqlCommandText = "";
        //     sqlCommandText = "update crm_opportunity set X_Notes=@p3 where N_CompanyID=@p1 and X_OpportunityCode=@p2";
        //     Params.Add("@p1", nCompanyId);
        //     Params.Add("@p2", xOpportunityCode);
        //     Params.Add("@p3", xnotes);


        //     try
        //     {
        //         using (SqlConnection connection = new SqlConnection(connectionString))
        //         {
        //             connection.Open();
        //             dLayer.ExecuteNonQuery(sqlCommandText, Params, connection);
        //         }
        //         return Ok(api.Warning("Notes Updated"));

        //     }
        //     catch (Exception e)
        //     {
        //         return Ok(api.Error(User,e));
        //     }
        // }

        [HttpPost("orderupdate")]
        public ActionResult OrderUpdate([FromBody] DataSet ds)
        {

            DataTable MasterTable;
            MasterTable = ds.Tables["master"];
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            Params.Add("@p1", nCompanyId);

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    int N_Order = 1;
                    foreach (DataRow var in MasterTable.Rows)
                    {
                        //dLayer.ExecuteNonQuery("update crm_activity set N_Order=" + N_Order + " where N_CompanyID=@p1 and X_ActivityCode=" + var["x_ActivityCode"].ToString(), Params, connection);
                        dLayer.ExecuteNonQuery("update tsk_taskmaster set N_Order=" + N_Order + " where N_CompanyID=@p1 and x_TaskCode=" + var["x_TaskCode"].ToString(), Params, connection);
                        N_Order++;
                    }
                }
                return Ok(api.Success("Order Updated"));

            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }

        [HttpGet("comments")]
        public ActionResult GetComments(int nOpportunityID)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    DataSet dt = new DataSet();
                    SortedList Params = new SortedList();
                    // DataTable MasterTable = new DataTable();
                    DataTable CommentsTable = new DataTable();
                    string CommentsSql = "";

                    Params.Add("@nCompanyID", myFunctions.GetCompanyID(User));
                    Params.Add("@nOpportunityID", nOpportunityID);

                    // object TaskID = dLayer.ExecuteScalar("select N_TaskID from Tsk_TaskMaster where N_CompanyID=@nCompanyID and N_OpportunityID=@nOpportunityID order by N_TaskID desc", Params, connection);

                    //TaskComments
                    CommentsSql = "select top(5) X_UserName,D_Date,X_Comments,'Commented by #CREATOR on #TIME - ' + X_Comments as X_TaskComments from VW_Task_Comments where N_ActionID in (select N_TaskID from Tsk_TaskMaster where N_CompanyID=@nCompanyID and N_OpportunityID=@nOpportunityID) order by D_Date desc";
                    CommentsTable = dLayer.ExecuteDataTable(CommentsSql, Params, connection);

                    foreach (DataRow row in CommentsTable.Rows)
                    {
                        object creator = row["X_UserName"];

                        string time = row["D_Date"].ToString();
                        DateTime _date1;
                        string day1 = "";
                        if (time != null && time != "")
                        {
                            _date1 = DateTime.Parse(time.ToString());
                            day1 = _date1.ToString("dd-MMM-yyyy  HH:mm tt");
                        }
                        
                        if (row["X_TaskComments"].ToString().Contains("#CREATOR"))
                        {
                            row["X_TaskComments"] = row["X_TaskComments"].ToString().Replace("#CREATOR", creator.ToString());
                        }
                        if (row["X_TaskComments"].ToString().Contains("#TIME"))
                        {
                            row["X_TaskComments"] = row["X_TaskComments"].ToString().Replace("#TIME", day1);
                        }
                    }
                    
                    CommentsTable.AcceptChanges();
                    CommentsTable = api.Format(CommentsTable, "Comments");

                    dt.Tables.Add(CommentsTable);

                    return Ok(api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }
        [HttpPost("savenotes")]
        public ActionResult SaveNotes([FromBody] DataSet ds)
        {

            DataTable MasterTable;
            MasterTable = ds.Tables["master"];
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();
                    int nCommentID = dLayer.SaveData("CRM_Notes", "N_CommentID", MasterTable, connection, transaction);
                    transaction.Commit();
                    return Ok(api.Success("Saved"));

                }
            }
            catch (Exception e)
            {
                return Ok(api.Error(User, e));
            }
        }
        [HttpGet("notesDetails")]
        public ActionResult GetNotesDetails(int nTransID, int nFormID)
        {
            DataSet dt = new DataSet();
            SortedList Params = new SortedList();
            DataTable NotesTable = new DataTable();
            int nCompanyID = myFunctions.GetCompanyID(User);

            string sqlCommand = "select * from vw_CRM_Notes where N_CompanyID=@p1 and n_TransID=@p2 and n_MenuID =@p3 order by (d_EntryDate)desc"; 
            Params.Add("@p1", nCompanyID);
            Params.Add("@p2", nTransID);
            Params.Add("@p3", nFormID);
            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    NotesTable = dLayer.ExecuteDataTable(sqlCommand, Params, connection);
                    NotesTable.AcceptChanges();
                    NotesTable = api.Format(NotesTable, "Notes");
                    dt.Tables.Add(NotesTable);
                    return Ok(api.Success(dt));
                }
            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }
}
}

