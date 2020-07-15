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
    [Route("itemunit")]
    [ApiController]
    public class Inv_ItemUnit : ControllerBase
    {
        private readonly IDataAccessLayer dLayer;
        private readonly IApiFunctions _api;

        
        public Inv_ItemUnit(IDataAccessLayer dl,IApiFunctions api)
        {
            dLayer=dl;
            _api=api;
        }
       

        [HttpGet("list")]
        public ActionResult GetItemUnitList(int? nCompanyId)
        {
            DataTable dt=new DataTable();
            SortedList Params=new SortedList();
            
            string sqlCommandText="select Code,[Unit Code],Description from vw_InvItemUnit_Disp where N_CompanyID=@p1 and N_ItemID is null order by ItemCode,[Unit Code]";
            Params.Add("@p1",nCompanyId);

            try{
                dt=dLayer.ExecuteDataTable(sqlCommandText,Params);
                dt=_api.Format(dt);
                if(dt.Rows.Count==0)
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
        public ActionResult GetItemUnitListDetails(int? nCompanyId,int? nItemUnitID)
        {
            DataTable dt=new DataTable();
            SortedList Params=new SortedList();
            
            string sqlCommandText="select Code,[Unit Code],Description from vw_InvItemUnit_Disp where N_CompanyID=@p1 and code=@p2 order by ItemCode,[Unit Code]";
            Params.Add("@p1",nCompanyId);
            Params.Add("@p2",nItemUnitID);

            try{
                dt=dLayer.ExecuteDataTable(sqlCommandText,Params);
                dt=_api.Format(dt);
                if(dt.Rows.Count==0)
                    {
                        return StatusCode(200,_api.Response(200 ,"No Results Found" ));
                    }else{
                        return Ok(dt);
                    }   
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
                    MasterTable = ds.Tables["master"];
                    
                    SortedList Params = new SortedList();
                    dLayer.setTransaction();
                    int N_ItemUnitID=dLayer.SaveData("Inv_ItemUnit","N_ItemUnitID",0,MasterTable);                    
                    if(N_ItemUnitID<=0){
                        dLayer.rollBack();
                        return StatusCode(409,_api.Response(409 ,"Unable to save ItemUnit" ));
                        }
                   else{
                        dLayer.commit();
                    }
                 return  GetItemUnitListDetails(int.Parse(MasterTable.Rows[0]["n_CompanyId"].ToString()),N_ItemUnitID);
                
                }

                catch (Exception ex)
                {
                    dLayer.rollBack();
                    return StatusCode(403,_api.ErrorResponse(ex));
                }
        }


        [HttpGet("itemwiselist")]
        public ActionResult GetItemWiseUnitList(int? nCompanyId,string baseUnit,int itemId)
        {
            try{
                SortedList mParamsList = new SortedList()
                    {
                        {"N_CompanyID",nCompanyId},    
                        {"X_ItemUnit",baseUnit},
                        {"N_ItemID",itemId}
                    };
                DataTable masterTable = dLayer.ExecuteDataTablePro("SP_FillItemUnit",mParamsList);
                if(masterTable.Rows.Count==0){return Ok(new {});}
                return Ok(masterTable);
            }catch(Exception e){
                return StatusCode(403,_api.ErrorResponse(e));
            }
        }

         [HttpDelete("delete")]
        public ActionResult DeleteData(int nItemUnitID)
        {
             int Results=0;
            try
            {
                Results=dLayer.DeleteData("Inv_ItemUnit","N_ItemUnitID",nItemUnitID,"");
                if(Results>0){
                    return StatusCode(200,_api.Response(200 ,"Product Unit deleted" ));
                }else{
                    return StatusCode(409,_api.Response(409 ,"Unable to delete product Unit" ));
                }
                
            }
            catch (Exception ex)
                {
                    return StatusCode(403,_api.ErrorResponse(ex));
                }
            

        }
    }
}