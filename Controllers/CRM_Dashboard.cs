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
    [Route("crmDashboard")]
    [ApiController]
    public class CrmDashboard : ControllerBase
    {
        private readonly IApiFunctions api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;

        public CrmDashboard(IApiFunctions apifun, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            api = apifun;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }

        [HttpGet("details")]
        public ActionResult GetDashboardDetails(int nFnYearId)
        {
            SortedList Params = new SortedList();
            int nCompanyID = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);
            string UserPattern = myFunctions.GetUserPattern(User);
            string Pattern = "";
            string AssigneePattern = "";
            if (UserPattern != "")
            {
                Params.Add("@UserPattern", UserPattern);
                Pattern = " and Left(X_Pattern,Len(@UserPattern))=@UserPattern ";
                AssigneePattern =" and (Left(X_Pattern,Len(@UserPattern))=@UserPattern or N_LoginUserID="+nUserID+") ";
            }
            else
            {
                 Pattern = " and N_CreatedUser=" + nUserID;
                 AssigneePattern = " and (N_CreatedUser=" + nUserID + " or N_LoginUserID="+nUserID+")";
            }
            string sqlCurrentLead = "SELECT COUNT(*) as N_ThisMonth FROM vw_CRMLeads WHERE MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP) and N_CompanyID = "+nCompanyID+""+ Pattern;
            string sqlWin = "select count(*) as N_ThisMonth from vw_CRMOpportunity where N_StatusTypeID=308 and MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP) and N_CompanyID = "+nCompanyID+""+ AssigneePattern; 
            string sqlLose = "select count(*) as N_ThisMonth from vw_CRMOpportunity where N_StatusTypeID=309 and  MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP) and N_CompanyID = "+nCompanyID+""+ AssigneePattern; 
            string sqlCurrentRevenue = "SELECT COUNT(*) as N_ThisMonth,sum(Cast(REPLACE(N_ExpRevenue,',','') as Numeric(10,2)) ) as TotalAmount FROM vw_CRMOpportunity WHERE MONTH(D_EntryDate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_EntryDate) = YEAR(CURRENT_TIMESTAMP) and N_StatusTypeID=308 and N_CompanyID = "+nCompanyID+""+ AssigneePattern;
            string sqlPipelineoppotunity = "select count(*) as N_Count,sum(Cast(REPLACE(N_ExpRevenue,',','') as Numeric(10,2)) ) as TotalAmount from vw_CRMOpportunity where N_ClosingStatusID=0 or N_ClosingStatusID is null and N_CompanyID = "+nCompanyID+""+ AssigneePattern;
            string sqlOpportunitiesStage = "select isNull(X_Stage,'Others') as X_Stage,CAST(COUNT(*) as varchar(50)) as N_Percentage  from vw_CRMOpportunity where N_CompanyID = "+nCompanyID + AssigneePattern +" group by X_Stage,N_Sort order by ISNULL(N_Sort,1000)";
            string sqlLeadsbySource = "select isnull(x_SubSource,'Other') as X_LeadSource ,CAST(COUNT(*) as varchar(50)) as N_Percentage from vw_CRMOpportunity where isnull(N_ClosingStatusID,0) = 0 and  N_CompanyID = "+nCompanyID + Pattern +" group by x_SubSource";
            string sqlPreviousLead = "SELECT COUNT(*) as N_LastMonth FROM vw_CRMLeads WHERE DATEPART(m, D_EntryDate) = DATEPART(m, DATEADD(m, -1, getdate())) and N_CompanyID = "+nCompanyID+""+ Pattern;
            string sqlCurrentCustomer = "SELECT COUNT(*) as N_ThisMonth FROM CRM_Customer WHERE MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP) and N_CompanyID = "+nCompanyID+" and N_UserID = "+nUserID+"";
            string sqlPreviousCustomer = "SELECT COUNT(*) as N_LastMonth FROM CRM_Customer WHERE DATEPART(m, D_EntryDate) = DATEPART(m, DATEADD(m, -1, getdate())) and N_CompanyID = "+nCompanyID+" and N_UserID = "+nUserID+"";
           // string sqlPerformance = " SELECT 'Leads Created' as X_Status,CONVERT(VARCHAR,COUNT(*)) as N_Count FROM crm_leads WHERE N_CompanyID = "+nCompanyID+" and N_UserID = "+nUserID+" and D_Entrydate >= DATEADD(DAY, -90, GETDATE()) union SELECT 'Opportunities Created' as X_Status,CONVERT(VARCHAR,COUNT(*))as N_Count FROM CRM_Opportunity WHERE N_CompanyID = "+nCompanyID+" and N_UserID = "+nUserID+" and D_Entrydate >= DATEADD(DAY, -90, GETDATE()) union SELECT 'Customer Created' as X_Status,CONVERT(VARCHAR,COUNT(*)) as N_Count FROM CRM_Customer WHERE  N_CompanyID = "+nCompanyID+" and N_UserID = "+nUserID+" and D_Entrydate >= DATEADD(DAY, -90, GETDATE()) union SELECT  'Revenue Generated' as X_Status, CAST(CONVERT(VARCHAR, CAST(sum(N_TotAmt) AS MONEY), 1) AS VARCHAR) as N_Count FROM vw_InvSalesOrderNo_Search WHERE N_CompanyID = "+nCompanyID+" and N_UserID = "+nUserID+" and D_OrderDate >= DATEADD(DAY, -90, GETDATE()) union SELECT 'Close Deals' as X_Status,CONVERT(VARCHAR,COUNT(*)) as N_Count from vw_CRMOpportunity where ( N_StatusTypeID=308 OR N_StatusTypeID=309) and N_CompanyID= "+nCompanyID+" and N_UserID = "+nUserID+"  and D_Entrydate >= DATEADD(DAY, -90, GETDATE())  union SELECT 'Lose Deals' as X_Status,CONVERT(VARCHAR,COUNT(*)) as N_Count from vw_CRMOpportunity where  N_StatusTypeID=309 and N_CompanyID= "+nCompanyID+" and N_UserID = "+nUserID+"  and D_Entrydate >= DATEADD(DAY, -90, GETDATE()) --'Contacts Created' as X_Status,COUNT(*) as N_Count FROM CRM_Contact WHERE N_CompanyID = "+nCompanyID+" and N_UserID = "+nUserID+" and D_Entrydate >= DATEADD(DAY, -90, GETDATE())  union SELECT 'Projects Created' as X_Status,COUNT(*) as N_Count FROM CRM_Project WHERE N_CompanyID = "+nCompanyID+" and N_UserID = "+nUserID+" and D_Entrydate >= DATEADD(DAY, -90, GETDATE()) " ;
            string sqlPerformance = "SELECT 'New Opportunities' as X_Status,CONVERT(VARCHAR,COUNT(*))as N_Count,'2' as OrderNo FROM vw_CRMOpportunity WHERE N_CompanyID = "+nCompanyID + Pattern +" and D_Entrydate >= DATEADD(DAY, -90, GETDATE()) union SELECT 'New Customers' as X_Status,CONVERT(VARCHAR,COUNT(*)) as N_Count,'1' as OrderNo FROM vw_CRMCustomer WHERE  N_CompanyID = "+nCompanyID+ Pattern +" and D_Entrydate >= DATEADD(DAY, -90, GETDATE()) union SELECT  'Revenue Generated' as X_Status, CAST(CONVERT(VARCHAR, CAST(sum(N_TotAmt) AS MONEY), 1) AS VARCHAR) as N_Count,'5' as OrderNo FROM vw_InvSalesOrderNo_Search_Cloud WHERE N_CompanyID = "+nCompanyID+ Pattern +" and D_OrderDate >= DATEADD(DAY, -90, GETDATE()) union SELECT 'Closed Deals' as X_Status,CONVERT(VARCHAR,COUNT(*)) as N_Count,'3' as OrderNo from vw_CRMOpportunity where ( N_StatusTypeID=308 OR N_StatusTypeID=309) and N_CompanyID= "+nCompanyID + Pattern +"  and D_Entrydate >= DATEADD(DAY, -90, GETDATE())  union SELECT 'Lose Deals' as X_Status,CONVERT(VARCHAR,COUNT(*)) as N_Count,'4' as OrderNo from vw_CRMOpportunity where  N_StatusTypeID=309 and N_CompanyID= "+nCompanyID + Pattern +"  and D_Entrydate >= DATEADD(DAY, -90, GETDATE()) order by OrderNo" ;
            string sqlPreviousRevenue ="SELECT COUNT(*) as N_LastMonth,sum(Cast(REPLACE(N_ExpRevenue,',','') as Numeric(10,2)) ) as TotalAmount FROM vw_CRMOpportunity WHERE DATEPART(m, D_EntryDate) = DATEPART(m, DATEADD(m, -1, getdate()))and N_CompanyID = "+nCompanyID+""+ Pattern;
            string sqlTopSalesman ="select top(5) X_SalesmanName as X_SalesmanName, N_TotRevenue N_Percentage from vw_InvSalesmanRevenue where N_CompanyID = "+nCompanyID+" and N_UserID = "+nUserID+" order by N_TotRevenue Desc";
            string sqlQuarterlyRevenue = "select sum(N_TotAmt) as N_Amount,quarter from vw_QuarterlyRevenue where N_CompanyID = "+nCompanyID+" and N_FnyearID ="+nFnYearId+" and N_UserID = "+nUserID+" group by quarter ";
            string sqlTotTargetAmount ="select Sum(N_TargetAmount) as TotalAmount from Inv_SalesMan where N_CompanyID="+nCompanyID+"" ;



            SortedList Data=new SortedList();
            DataTable CurrentLead = new DataTable();
            DataTable CurrentCustomer = new DataTable();
            DataTable Performance = new DataTable();
            DataTable OpportunitiesStage = new DataTable();
            DataTable LeadsbySource = new DataTable();
            DataTable PipelineOppotunity = new DataTable();
            DataTable CurrentRevenue= new DataTable();
            DataTable Win = new DataTable();
            DataTable Lose = new DataTable();
            DataTable TopSalesman = new DataTable();
         
            DataTable QuarterlyRevenue = new DataTable();
            DataTable TargetAmountSalesMan = new DataTable();
            object LeadLastMonth="";
            object CustomerLastMonth="";
            object RevenueLastMonth="";
            object LeadPercentage="";
            object CustomerPercentage="";
            object RevenuePercentage="";

            

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    CurrentLead = dLayer.ExecuteDataTable(sqlCurrentLead, Params, connection);
                    CurrentCustomer = dLayer.ExecuteDataTable(sqlCurrentCustomer, Params, connection);
                    Performance = dLayer.ExecuteDataTable(sqlPerformance, Params, connection);
                    OpportunitiesStage = dLayer.ExecuteDataTable(sqlOpportunitiesStage, Params, connection);
                    LeadsbySource = dLayer.ExecuteDataTable(sqlLeadsbySource, Params, connection);
                    PipelineOppotunity = dLayer.ExecuteDataTable(sqlPipelineoppotunity, Params, connection);
                    Win=dLayer.ExecuteDataTable(sqlWin, Params, connection);
                    Lose=dLayer.ExecuteDataTable(sqlLose, Params, connection);
                    LeadLastMonth = dLayer.ExecuteScalar(sqlPreviousLead, Params, connection);
                    CustomerLastMonth = dLayer.ExecuteScalar(sqlPreviousCustomer, Params, connection);
                    CurrentRevenue=dLayer.ExecuteDataTable(sqlCurrentRevenue, Params, connection);
                    RevenueLastMonth=dLayer.ExecuteDataTable(sqlPreviousRevenue, Params, connection);
                    TopSalesman = dLayer.ExecuteDataTable(sqlTopSalesman, Params, connection);
                    QuarterlyRevenue = dLayer.ExecuteDataTable(sqlQuarterlyRevenue, Params, connection);
                    TargetAmountSalesMan = dLayer.ExecuteDataTable(sqlTotTargetAmount, Params, connection);


                    if(myFunctions.getVAL(LeadLastMonth.ToString())!=0)
                        LeadPercentage=(Math.Round((myFunctions.getVAL(CurrentLead.Rows[0]["N_ThisMonth"].ToString())- myFunctions.getVAL(LeadLastMonth.ToString()))/myFunctions.getVAL(LeadLastMonth.ToString())*100)).ToString();
                    if(myFunctions.getVAL(CustomerLastMonth.ToString())!=0)
                        CustomerPercentage=((myFunctions.getVAL(CurrentCustomer.Rows[0]["N_ThisMonth"].ToString())- myFunctions.getVAL(CustomerLastMonth.ToString()))/myFunctions.getVAL(CustomerLastMonth.ToString())*100).ToString();
                  if(myFunctions.getVAL(RevenueLastMonth.ToString())!=0)
                        RevenuePercentage=((myFunctions.getVAL(CurrentRevenue.Rows[0]["N_ThisMonth"].ToString())- myFunctions.getVAL(RevenueLastMonth.ToString()))/myFunctions.getVAL(RevenueLastMonth.ToString())*100).ToString();
                    
                }
                // double N_TotalOppotunity=0;
                

                // double N_TotalLead=0;
                // foreach (DataRow dtRow in LeadsbySource.Rows)
                // {N_TotalLead=N_TotalLead + myFunctions.getVAL(dtRow["N_Percentage"].ToString());}
                // foreach (DataRow dtRow in OpportunitiesStage.Rows)
                // {
                //     dtRow["N_Percentage"]=((myFunctions.getVAL(dtRow["N_Percentage"].ToString())/N_TotalLead)*100).ToString();
                // }
  
                CurrentLead = myFunctions.AddNewColumnToDataTable(CurrentLead, "N_LastMonth", typeof(string),LeadLastMonth);
                CurrentLead = myFunctions.AddNewColumnToDataTable(CurrentLead, "N_Percentage", typeof(string),LeadPercentage);
                CurrentLead.AcceptChanges();
                CurrentCustomer = myFunctions.AddNewColumnToDataTable(CurrentCustomer, "N_LastMonth", typeof(string), CustomerLastMonth);
                CurrentCustomer = myFunctions.AddNewColumnToDataTable(CurrentCustomer, "N_Percentage", typeof(string), CustomerPercentage);
                CurrentCustomer.AcceptChanges();
                CurrentRevenue = myFunctions.AddNewColumnToDataTable(CurrentRevenue, "N_LastMonth", typeof(string), RevenueLastMonth);
                CurrentRevenue = myFunctions.AddNewColumnToDataTable(CurrentRevenue, "N_Percentage", typeof(string),RevenuePercentage);
                CurrentRevenue.AcceptChanges();


                if(CurrentLead.Rows.Count>0)Data.Add("leadData",CurrentLead);
                if(CurrentCustomer.Rows.Count>0)Data.Add("customerData",CurrentCustomer);
                if(Performance.Rows.Count>0)Data.Add("performance",Performance);
                if(OpportunitiesStage.Rows.Count>0)Data.Add("opportunitiesStage",OpportunitiesStage);
                if(LeadsbySource.Rows.Count>0)Data.Add("leadsbySource",LeadsbySource);
                if(PipelineOppotunity.Rows.Count>0)Data.Add("oppotunityData",PipelineOppotunity);
                if(Win.Rows.Count>0)Data.Add("winData",Win);
                if(Lose.Rows.Count>0)Data.Add("loseData",Lose);
                if(CurrentRevenue.Rows.Count>0)Data.Add("revenueData",CurrentRevenue);
                if(TopSalesman.Rows.Count>0)Data.Add("topSalesmanData",TopSalesman);
                if(QuarterlyRevenue.Rows.Count>0)Data.Add("quarterlyRevenueData",QuarterlyRevenue);
                if(TargetAmountSalesMan.Rows.Count>0)Data.Add("salesManTargetAmount",TargetAmountSalesMan);

                return Ok(api.Success(Data));

            }
            catch (Exception e)
            {
                return Ok(api.Error(User,e));
            }
        }
[HttpGet("leadlist")]
        public ActionResult GetLeadList(int nFnYearId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            string sqlCommandCount = "";
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (x_lead like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_LeadID desc";
            else
                xSortBy = " order by " + xSortBy;

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_CRMLeads where MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP)   " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_CRMLeads where MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP)   " + Searchkey + " and N_LeadID not in (select top(" + Count + ") N_LeadID from vw_CRMLeads where N_CompanyID=@p1 " + xSortBy + " ) " + xSortBy;
            Params.Add("@p1", nCompanyId);

            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    sqlCommandCount = "Select * from vw_CRMLeads Where MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP)  ";
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
                return Ok(api.Error(User,e));
            }
        }


[HttpGet("opportunitylist")]
        public ActionResult GetOpportunityList(int nFnYearId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            string sqlCommandCount = "";
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_Opportunity like'%" + xSearchkey + "%'or X_OpportunityCode like'%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_OpportunityID desc";
            else
                xSortBy = " order by " + xSortBy;

            if (Count == 0)
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_CRMOpportunity where N_ClosingStatusID=0 or N_ClosingStatusID is null and MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP) " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(" + nSizeperpage + ") * from vw_CRMOpportunity where N_ClosingStatusID=0 or N_ClosingStatusID is null and MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP)" + Searchkey + "  and N_OpportunityID not in (select top(" + Count + ") N_OpportunityID from vw_CRMOpportunity where N_CompanyID=@p1 " + xSortBy + ") " + xSortBy;
            Params.Add("@p1", nCompanyId);

            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);

                    sqlCommandCount = "Select * from vw_CRMOpportunity where N_ClosingStatusID=0 or N_ClosingStatusID is null and MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP)";
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
                return Ok(api.Error(User,e));
            }
        }
  [HttpGet("customerslist")]
        public ActionResult CustomerList(int nPage,int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            string sqlCommandCount = "";
            int nCompanyId=myFunctions.GetCompanyID(User);
            int Count= (nPage - 1) * nSizeperpage;
            string sqlCommandText ="";

            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_Customer like '%" + xSearchkey + "%'or X_CustomerCode like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_CustomerID desc";
            else
                xSortBy = " order by " + xSortBy;
             
             if(Count==0)
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_CRMCustomer where MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP) " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_CRMCustomer where MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP)" + Searchkey + " and N_CustomerID not in (select top("+ Count +") N_CustomerID from vw_CRMCustomer where N_CompanyID=@p1 " + xSortBy + " ) " + xSortBy;
            Params.Add("@p1", nCompanyId);

            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);

                    sqlCommandCount = "select count(*) as N_Count  from vw_CRMCustomer where MONTH(D_Entrydate) = MONTH(CURRENT_TIMESTAMP) AND YEAR(D_Entrydate) = YEAR(CURRENT_TIMESTAMP)";
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
                return Ok(api.Error(User,e));
            }
        }

 [HttpGet("opportunitylist1")]
        public ActionResult OpportunityList1(int nPage,int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            string sqlCommandCount = "";
            int nCompanyId=myFunctions.GetCompanyID(User);
            int Count= (nPage - 1) * nSizeperpage;
            string sqlCommandText ="";
            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and (X_Opportunity like'%" + xSearchkey + "%'or X_OpportunityCode like'%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_OpportunityID desc";
            else
                xSortBy = " order by " + xSortBy;
             
             if(Count==0)
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_CRMOpportunity where N_CompanyID=@p1 and (N_ClosingStatusID is null or N_ClosingStatusID=0) " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top("+ nSizeperpage +") * from vw_CRMOpportunity where N_CompanyID=@p1 and (N_ClosingStatusID is null or N_ClosingStatusID=0) " + Searchkey + " and N_OpportunityID not in (select top("+ Count +") N_OpportunityID from vw_CRMOpportunity where N_CompanyID=@p1 " + xSortBy + " ) " + xSortBy;
            Params.Add("@p1", nCompanyId);

            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params,connection);

                    sqlCommandCount = "select count(*) as N_Count  from vw_CRMOpportunity where N_CompanyID=@p1 ";
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
                return Ok(api.Error(User,e));
            }
        }


        
        [HttpGet("salesManList")]
        public ActionResult GetSalesmanList(int nFnYearId, int nPage, int nSizeperpage, string xSearchkey, string xSortBy)
        {
            DataTable dt = new DataTable();
            SortedList Params = new SortedList();
            int nCompanyId = myFunctions.GetCompanyID(User);
            int nUserID = myFunctions.GetUserID(User);
            
            string sqlCommandCount = "";
            int Count = (nPage - 1) * nSizeperpage;
            string sqlCommandText = "";
            string Searchkey = "";
            if (xSearchkey != null && xSearchkey.Trim() != "")
                Searchkey = "and ([X_SalesmanCode] like '%" + xSearchkey + "%')";

            if (xSortBy == null || xSortBy.Trim() == "")
                xSortBy = " order by N_TotRevenue desc";
            else
                xSortBy = " order by " + xSortBy;

            if (Count == 0)
                sqlCommandText = "select top(10) * from vw_InvSalesmanRevenue where  N_CompanyID ="+nCompanyId+" and N_UserID = "+nUserID+"   " + Searchkey + " " + xSortBy;
            else
                sqlCommandText = "select top(10) * from vw_InvSalesmanRevenue where N_CompanyID ="+nCompanyId+" and N_UserID = "+nUserID+"   " + Searchkey + " " + xSortBy;
            Params.Add("@p1", nCompanyId);

            SortedList OutPut = new SortedList();


            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dt = dLayer.ExecuteDataTable(sqlCommandText, Params, connection);
                    dt = api.Format(dt);
                    sqlCommandCount = "Select * from vw_InvSalesmanRevenue Where  N_CompanyID ="+nCompanyId+" and N_UserID = "+nUserID+"    ";
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
                return Ok(api.Error(User,e));
            }
        }

      

    }
 
}