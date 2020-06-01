using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using SmartxAPI.GeneralFunctions;
using System;
using System.Data;
using System.Collections;

namespace SmartxAPI.Controllers

{
    [Authorize(AuthenticationSchemes=JwtBearerDefaults.AuthenticationScheme)]
    [Route("salesquotation")]
    [ApiController]
    public class Inv_SalesQuotationController : ControllerBase
    {
        private readonly IDataAccessLayer _dataAccess;
        private readonly IApiFunctions _api;

        
        public Inv_SalesQuotationController(IDataAccessLayer dataaccess,IApiFunctions api)
        {
            _dataAccess=dataaccess;
            _api=api;
        }
       

        [HttpGet("list")]
        public ActionResult GetSalesQuotationList(int? nCompanyId,int nFnYearId)
        {
            DataTable dt=new DataTable();
            SortedList Params=new SortedList();
            
            string X_Table="vw_InvSalesQuotationNo_Search";
            string X_Fields = "N_QuotationId as NQuotationId,[Quotation No] as QuotationNo,[Quotation Date] as QuotationDate,N_CompanyId as NCompanyId,N_CustomerId as NCustomerId,[Customer Code] as CustomerCode,N_FnYearID as NFnYearId,D_QuotationDate as DQuotationDate,N_BranchId as NBranchId,B_YearEndProcess as BYearEndProcess,X_CustomerName as XCustomerName,X_BranchName as XBranchName,X_RfqRefNo as XRfqRefNo,D_RfqRefDate as DRfqRefDate,N_Amount as NAmount,N_FreightAmt as NFreightAmt,N_DiscountAmt as NDiscountAmt,N_Processed as NProcessed,N_OthTaxAmt as NOthTaxAmt,N_BillAmt as NBillAmt,N_ProjectID as NProjectId,X_ProjectName as XProjectName";
            string X_Crieteria = "N_CompanyID=@p1 and N_FnYearID=@p2";
            string X_OrderBy="";
            Params.Add("@p1",nCompanyId);
            Params.Add("@p2",nFnYearId);

            try{
                dt=_dataAccess.Select(X_Table,X_Fields,X_Crieteria,Params,X_OrderBy);
                if(dt.Rows.Count==0)
                    {
                        return StatusCode(200,_api.Response(200 ,"No Results Found" ));
                    }else{
                        return Ok(dt);
                    }   
            }catch(Exception e){
                return StatusCode(404,_api.Response(404,e.Message));
            }
        }
        [HttpGet("listDetails")]
        public ActionResult GetSalesQuotationList(int? nCompanyId,int nQuotationId,int nFnYearId)
        {
            DataSet dt=new DataSet();
            SortedList Params=new SortedList();
            
            string X_Table="vw_InvSalesQuotationNo_Search";
            string X_Fields = "*";   
            string X_Crieteria = "N_CompanyID=@p1 and N_FnYearID=@p2 and N_QuotationID=@p3";
            string X_OrderBy="";
            Params.Add("@p1",nCompanyId);
            Params.Add("@p2",nFnYearId);
            Params.Add("@p3",nQuotationId);

            try{
                DataTable Quotation = new DataTable();
                
                Quotation=_dataAccess.Select(X_Table,X_Fields,X_Crieteria,Params,X_OrderBy);
                foreach(DataColumn c in Quotation.Columns)
                    c.ColumnName = String.Join("", c.ColumnName.Split());
                dt.Tables.Add(Quotation);
                Quotation.TableName="Master";
                
                //Quotation Details

            string  X_Table1="vw_InvQuotationDetails";
            string X_Fields1 = "*";
            string X_Crieteria1 = "N_CompanyID=@p1 and N_FnYearID=@p2 and N_QuotationID=@p3";
            string X_OrderBy1="";
            DataTable QuotationDetails = new DataTable();
            QuotationDetails=_dataAccess.Select(X_Table1,X_Fields1,X_Crieteria1,Params,X_OrderBy1);
            foreach(DataColumn c in QuotationDetails.Columns)
                    c.ColumnName = String.Join("", c.ColumnName.Split());
            dt.Tables.Add(QuotationDetails);
            QuotationDetails.TableName="Details";

            



return Ok(dt);

                // if(dt.Tables["Master"].Rows.Count==0)
                //     {
                //         return StatusCode(200,_api.Response(200 ,"No Results Found" ));
                //     }else{
                //         return Ok(dt.Tables[0]);
                //     }   
            }catch(Exception e){
                return StatusCode(404,_api.Response(404,e.Message));
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

                    // Auto Gen
                    var values = MasterTable.Rows[0]["x_QuotationNo"].ToString();


                    _dataAccess.StartTransaction();
                    int N_QuotationId=_dataAccess.SaveData("Inv_SalesQuotation","N_QuotationId",0,MasterTable);                    
                    if(N_QuotationId<=0){
                        _dataAccess.Rollback();
                        }
                    for (int j = 0 ;j < DetailTable.Rows.Count;j++)
                        {
                            DetailTable.Rows[j]["n_QuotationID"]=N_QuotationId;
                        }
                    int N_QuotationDetailId=_dataAccess.SaveData("Inv_SalesQuotationDetails","n_QuotationDetailsID",0,DetailTable);                    
                    _dataAccess.Commit();
                    return Ok("DataSaved");
                }
                catch (Exception ex)
                {
                    _dataAccess.Rollback();
                    return StatusCode(403,ex);
                }
        }
        //Delete....
         [HttpDelete()]
        public ActionResult DeleteData(int N_QuotationID)
        {
            int Results=0;
            try
            {Results=_dataAccess.DeleteData("Inv_SalesQuotation","n_quotationID",N_QuotationID,"");
            return Ok();}
            catch (Exception ex)
            {return Ok();}
            

        }
        
    }
}