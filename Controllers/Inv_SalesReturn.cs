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
    [Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme)]
    [Route("salesreturn")]
    [ApiController]
    public class Inv_SalesReturn : ControllerBase
    {
        private readonly IApiFunctions _api;
        private readonly IDataAccessLayer dLayer;
        private readonly IMyFunctions myFunctions;
        private readonly string connectionString;
        
        public Inv_SalesReturn(IApiFunctions api, IDataAccessLayer dl, IMyFunctions myFun, IConfiguration conf)
        {
            _api = api;
            dLayer = dl;
            myFunctions = myFun;
            connectionString = conf.GetConnectionString("SmartxConnection");
        }
       

        [HttpGet("list")]
        public ActionResult GetSalesReturn(int? nCompanyId,int nFnYearId)
        {
            DataTable dt=new DataTable();
            SortedList Params=new SortedList();
            
            string sqlCommandText= "select * from vw_InvDebitNo_Search where N_CompanyID=@p1 and N_FnYearID=@p2";
            Params.Add("@p1",nCompanyId);
            Params.Add("@p2",nFnYearId);

            try{
                using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        connection.Open();
                        dt=dLayer.ExecuteDataTable(sqlCommandText,Params,connection);
                    }
                dt=_api.Format(dt);
                if (dt.Rows.Count==0)
                    {
                        return StatusCode(200,_api.Response(200 ,"No Results Found" ));
                    }else{
                        return Ok(dt);
                    }   
            }catch(Exception e){
                return StatusCode(403,_api.ErrorResponse(e));
            }
        }
        [HttpGet("listdetails")]
        public ActionResult GetSalesReturnDetails(int? nCompanyId,string xDebitNoteNo,string xReceiptNo,int nFnYearId,bool bAllBranchData,int nBranchId,bool bDeliveryNote=false)
        {
            DataSet dt=new DataSet();
            SortedList Params=new SortedList();
            string sqlCommandText="";
            string X_type="";
            if (bDeliveryNote)
                X_type = "DELIVERY";
            else
                X_type = "SALES";
            if (bAllBranchData == true)
            {
                if(xReceiptNo!=""&&xReceiptNo!=null)
               { sqlCommandText = "SP_InvSalesReturn_Disp @CompanyID,@RcptNo,0,@Xtype,0,@FnYearID";
                Params.Add("@RcptNo",xReceiptNo);}
                else
                {sqlCommandText = "SP_InvSalesReturn_Disp @CompanyID,@RcptNo,1,@Xtype,0,@FnYearID";
                Params.Add("@RcptNo",xDebitNoteNo);}
            }
            else
            {
                if(xReceiptNo!=""&&xReceiptNo!=null)
                {sqlCommandText="SP_InvSalesReturn_Disp @CompanyID,@RcptNo,0,@Xtype,@BranchID,@FnYearID";
                Params.Add("@RcptNo",xReceiptNo);}
                else
                {sqlCommandText="SP_InvSalesReturn_Disp @CompanyID,@RcptNo,1,@Xtype,@BranchID,@FnYearID";
                Params.Add("@RcptNo",xDebitNoteNo);}
            }
            
            Params.Add("@CompanyID",nCompanyId);
            Params.Add("@FnYearID",nFnYearId);
            Params.Add("@BranchID",nBranchId);
            Params.Add("@Xtype",X_type);


            try{
                DataTable SalesReturn = new DataTable();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                connection.Open();
                SalesReturn=dLayer.ExecuteDataTable(sqlCommandText,Params,connection);
                SalesReturn=_api.Format(SalesReturn,"Master");
                dt.Tables.Add(SalesReturn);
                
                 int N_DebitNoteId = myFunctions.getIntVAL(SalesReturn.Rows[0]["N_DebitNoteId"].ToString());
                 Params.Add("@DebitNoteID",N_DebitNoteId);     
                 string  sqlCommandText2="Select * from vw_InvSalesRetunEdit Where N_CompanyID=@CompanyID and N_FnYearID=@FnYearID and N_DebitNoteId=@DebitNoteID and N_RetQty<>0";

                 DataTable SalesReturnDetails = new DataTable();
                 SalesReturnDetails=dLayer.ExecuteDataTable(sqlCommandText2,Params,connection);
                 SalesReturnDetails=_api.Format(SalesReturnDetails,"Details");
                 dt.Tables.Add(SalesReturnDetails);

                }
                return Ok(dt);
                }catch(Exception e){
                return StatusCode(403,_api.ErrorResponse(e));
                }
        }


        

       //Save....
       [HttpPost("Save")]
        public ActionResult SaveData([FromBody]DataSet ds)
        { 
            
            try{
                    DataTable MasterTable;
                    DataTable DetailTable;
                    MasterTable = ds.Tables["master"];
                    DetailTable = ds.Tables["details"];
                    SortedList Params = new SortedList();
                    // Auto Gen
                    string InvoiceNo="";
                    DataRow masterRow=MasterTable.Rows[0];
                    var values = masterRow["X_DebitNoteNo"].ToString();
                    
                    using (SqlConnection connection = new SqlConnection(connectionString))
                     {
                         connection.Open();
                         SqlTransaction transaction;
                          transaction = connection.BeginTransaction();
                    if(values=="@Auto"){
                        Params.Add("N_CompanyID",masterRow["n_CompanyId"].ToString());
                        Params.Add("N_YearID",masterRow["n_FnYearId"].ToString());
                        Params.Add("N_FormID",80);
                        Params.Add("N_BranchID",masterRow["n_BranchId"].ToString());
                        InvoiceNo =  dLayer.GetAutoNumber("Inv_SalesReturnMaster","X_DebitNoteNo", Params, connection, transaction);
                        if(InvoiceNo==""){return StatusCode(409,_api.Response(409 ,"Unable to generate sales return" ));}
                        MasterTable.Rows[0]["X_DebitNoteNo"] = InvoiceNo;
                    }

                    // dLayer.setTransaction();
                    int N_InvoiceId=dLayer.SaveData("Inv_SalesReturnMaster","N_DebitNoteId",0,MasterTable);                    
                    if(N_InvoiceId<=0){
                        transaction.Rollback();
                        }
                    for (int j = 0 ;j < DetailTable.Rows.Count;j++)
                        {
                            DetailTable.Rows[j]["N_DebitNoteId"]=N_InvoiceId;
                        }
                    int N_InvoiceDetailId=dLayer.SaveData("Inv_SalesReturnDetails","N_DebitnoteDetailsID",0,DetailTable,connection, transaction);                    
                    transaction.Commit();
                     }
                    return Ok("Sales Return Saved");
                }
                catch (Exception ex)
                {
                    return StatusCode(403,ex);
                }
        }
        //Delete....
         [HttpDelete()]
        public ActionResult DeleteData(int? nCompanyId,int? nDebitNoteId)
        {
            try
            {
                string sqlCommandText="";
                SortedList Params = new SortedList();
                sqlCommandText="SP_Delete_Trans_With_SaleAccounts  @N_CompanyId,'SALES RETURN',@N_DebitNoteId";
                Params.Add("@N_CompanyId",nCompanyId);
                Params.Add("@N_DebitNoteId",nDebitNoteId);
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    dLayer.ExecuteDataTable(sqlCommandText,Params,connection);

                }
                return StatusCode(200,_api.Response(200 ,"Sales Return deleted" ));
               
                
            }
            catch (Exception ex)
                {
                    return StatusCode(403,_api.ErrorResponse(ex));
                }

        }


         [HttpGet("salesreturnpendinglist")]
        public ActionResult GetReturnPendingList(int? nCompanyId,int nCustomerId,bool bAllBranchData,int nBranchId,int nLocationId)
        {
            SortedList Params=new SortedList();
            
            string crieteria="";


            if (bAllBranchData == true)
            {
                if (nCustomerId > 0)
                    crieteria = " where X_TransType='SALES' and N_SalesType = 0 and N_CustomerID=@nCustomerId and N_CompanyID=@nCompanyId and B_IsSaveDraft=0 and N_balanceQty>0";
                else
                    crieteria = " where X_TransType='SALES' and N_SalesType = 0 and N_CompanyID=@nCompanyId and B_IsSaveDraft=0 and N_balanceQty>0";
            }
            else
            {
                if (nCustomerId > 0)
                    crieteria = " where X_TransType='SALES' and N_SalesType = 0 and N_CustomerID=@nCustomerId and N_CompanyID=@nCompanyId and N_BranchID=@nBranchId and N_LocationID=@nLocationId and B_IsSaveDraft=0 and N_balanceQty>0 ";
                else
                    crieteria = " where X_TransType='SALES' and N_SalesType = 0 and N_CompanyID=@nCompanyId and N_BranchID=@nBranchId and N_LocationID=@nLocationId and B_IsSaveDraft=0 and N_balanceQty>0 ";
            }
            
            Params.Add("@nCompanyId",nCompanyId);
            Params.Add("@nCustomerId",nCustomerId);
            Params.Add("@bAllBranchData",bAllBranchData);
            Params.Add("@nBranchId",nBranchId);
            Params.Add("@nLocationId",nLocationId);
            string sqlCommandText="select [Invoice No],[Invoice Date],[Customer] as X_CustomerName,X_CustPONo,X_BranchName,N_CompanyID,N_CustomerID,N_SalesID,N_SalesType,X_TransType,N_FnYearID,N_BranchID,X_LocationName,N_LocationID,B_IsSaveDraft,N_balanceQty from vw_InvSalesReturnPending_Search "+ crieteria +" order by N_SalesID DESC,[Invoice No]";
            try{
                DataTable SalesRetunPList = new DataTable();
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                connection.Open();
                SalesRetunPList=dLayer.ExecuteDataTable(sqlCommandText,Params,connection);
                SalesRetunPList=_api.Format(SalesRetunPList);
                if(SalesRetunPList.Rows.Count==0){return Ok(_api.Notice("No Sales Return Pending List Found"));}
                }
                return Ok(_api.Success(SalesRetunPList));
                }catch(Exception e){
                return BadRequest(_api.Error(e));
                }
        }
         
        
    }
}