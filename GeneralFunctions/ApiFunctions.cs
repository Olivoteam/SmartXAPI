using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;

namespace SmartxAPI.GeneralFunctions
{
    public class ApiFunctions:IApiFunctions
    {
        private readonly IMapper _mapper;
         public ApiFunctions(IMapper mapper)
        {
            _mapper=mapper;
        }

          public object Response(int Code,string Response)
                {
                        return (new { StatusCode = Code , Message= Response });
                }
        public object ErrorResponse(Exception ex){
            string Msg="";
            string subString =ex.Message.Substring(8,ex.Message.Length -8);
            
                switch(ex.Message.Substring(0,8)){
                    case "Column '": Msg = ex.Message.Substring(7, subString.IndexOf("'")+1) +" is required"; 
                    break;
                    case "Error co": Msg = ex.Message.Substring(0,42); 
                    break;
                    default : Msg = "Invalid Request  " + ex.Message; 
                    break;
                }


                return (new { StatusCode = 403 , Message= Msg });
           
           
        }

        public DataTable Format (DataTable dt,string tableName){
            foreach(DataColumn c in dt.Columns){
                    c.ColumnName = Char.ToLowerInvariant(c.ColumnName[0]) + c.ColumnName.Substring(1);
                    //c.ColumnName = c.ColumnName.Replace("_", string.Empty);
                    c.ColumnName = String.Join("", c.ColumnName.Split());
                    //c.ColumnName = c.ColumnName.ToT
                    }
                    if(tableName!=""){
                        dt.TableName= tableName;
                    }
            return dt;
        }


    }
public interface IApiFunctions
    {
        public object Response(int Code,string Response);
        public object ErrorResponse(Exception ex);
        public DataTable Format (DataTable table,string tableName);
    }    
}